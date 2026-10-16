using System;
using System.Linq;
using HarmonyLib;
using MagicStardew.Companions;
using MagicStardew.Spells;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace MagicStardew
{
    public class ModEntry : Mod
    {
        public static ModEntry Instance { get; private set; } = null!;
        public ModConfig Config { get; private set; } = null!;
        public ManaBar ManaBar { get; private set; } = null!;
        public SpellManager SpellManager { get; private set; } = null!;
        public CompanionManager CompanionManager { get; private set; } = null!;

        public static Texture2D CooldownIcon = null!;
        public static Texture2D SpellBookTexture = null!;
        public static Texture2D SpellIcons = null!;
        public static Texture2D SpellBorder = null!;

        // ZNAK ZAPYTANIA: Funcja może być nullem, dopóki jej nie załadujemy z innego moda
        public static Func<bool>? IsDaLionGaugeVisible;
        public static bool HasSurvivalisti;

        public override void Entry(IModHelper helper)
        {
            Instance = this;
            Config = helper.ReadConfig<ModConfig>();

            SpellBorder = helper.ModContent.Load<Texture2D>("assets/border.png");
            SpellIcons = helper.ModContent.Load<Texture2D>("assets/spell.png");
            CooldownIcon = helper.ModContent.Load<Texture2D>("assets/cooldown.png");
            SpellBookTexture = helper.ModContent.Load<Texture2D>("assets/spellbook.png");

            ManaBar = new ManaBar(helper, Monitor, Config);
            SpellManager = new SpellManager(ManaBar, helper, Monitor, Config);
            CompanionManager = new CompanionManager(helper);

            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            helper.Events.GameLoop.DayStarted += OnDayStarted;
            helper.Events.GameLoop.TimeChanged += OnTimeChanged;
            helper.Events.Player.InventoryChanged += OnInventoryChanged;
            helper.Events.Display.RenderedHud += OnRenderedHud;

            new Harmony(ModManifest.UniqueID).PatchAll();

            RegisterConsoleCommands(helper);
        }

        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            HasSurvivalisti = Helper.ModRegistry.IsLoaded("Ophaneom.Survivalistic");

            IModInfo? daLionProfessions = Helper.ModRegistry.Get("DaLion.Professions");
            if (daLionProfessions != null)
            {
                try
                {
                    System.Reflection.Assembly? daLionAssembly = null;
                    foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        var fullName = assembly.FullName;
                        if (!string.IsNullOrEmpty(fullName) && fullName.StartsWith("DaLion.Professions,", StringComparison.Ordinal))
                        {
                            daLionAssembly = assembly;
                            break;
                        }
                    }

                    if (daLionAssembly != null)
                    {
                        Type? limitGaugeType = daLionAssembly.GetType("DaLion.Professions.Framework.Limits.LimitGauge");
                        if (limitGaugeType != null)
                        {
                            System.Reflection.PropertyInfo? isVisibleProperty = limitGaugeType.GetProperty("IsVisible", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

                            // BEZPIECZNE SPRAWDZENIE: czy property i method nie są nullami
                            if (isVisibleProperty != null && isVisibleProperty.GetMethod != null)
                            {
                                IsDaLionGaugeVisible = (Func<bool>)Delegate.CreateDelegate(typeof(Func<bool>), isVisibleProperty.GetMethod);
                                Monitor.Log("Successfully hooked into DaLion.Professions's Limit Gauge visibility check.", LogLevel.Info);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Monitor.Log($"Failed to get DaLion.Professions's IsVisible property via reflection: {ex}", LogLevel.Error);
                }
            }
        }

        private void OnDayStarted(object? sender, DayStartedEventArgs e)
        {
            var who = Game1.player;
            if (who == null) return;

            if (who.MiningLevel >= 10 && !who.modData.ContainsKey("magicstardew.spell.earthquake.unlocked"))
                SpellManager.UnlockSpell("Earthquake", "magicstardew.spell.earthquake.unlocked");

            if (who.MiningLevel >= 6 && !who.modData.ContainsKey("magicstardew.spell.stonespirit.unlocked"))
                SpellManager.UnlockSpell("Stone Spirit", "magicstardew.spell.stonespirit.unlocked");

            if (who.ForagingLevel >= 6 && !who.modData.ContainsKey("magicstardew.spell.lumberspirit.unlocked"))
                SpellManager.UnlockSpell("Lumber Spirit", "magicstardew.spell.lumberspirit.unlocked");
        }

        private void OnTimeChanged(object? sender, TimeChangedEventArgs e)
        {
            var who = Game1.player;
            if (who == null) return;

            if (e.NewTime >= 2500 && e.NewTime < 2600 && !who.modData.ContainsKey("magicstardew.spell.timerewind.unlocked"))
            {
                SpellManager.UnlockSpell("Time Rewind", "magicstardew.spell.timerewind.unlocked");
            }
        }

        private void OnInventoryChanged(object? sender, InventoryChangedEventArgs e)
        {
            if (!Context.IsWorldReady || !e.IsLocalPlayer) return;
            var who = e.Player;
            if (who == null) return;

            if (!who.modData.ContainsKey("magicstardew.spell.MachineSpeedUpSpell.unlocked"))
            {
                if (e.Added.Any(item => item is StardewValley.Object obj && obj.Name.Contains("Wine") && obj.Quality == 4))
                {
                    SpellManager.UnlockSpell("Machine Speed Up", "magicstardew.spell.MachineSpeedUpSpell.unlocked");
                }
            }
        }

        private void OnRenderedHud(object? sender, RenderedHudEventArgs e)
        {
            ManaBar?.OnRenderedHud(sender, e);
        }

        private void RegisterConsoleCommands(IModHelper helper)
        {
            helper.ConsoleCommands.Add("mana_max", "Refills mana.", (cmd, args) =>
            {
                if (!Context.IsWorldReady) return;
                var player = Game1.player;
                if (player == null) return;
                ManaBar.SetMana(player, ManaBar.GetMaxMana(player));
                Monitor.Log("Mana maxed out!", LogLevel.Info);
            });
        }
    }
}