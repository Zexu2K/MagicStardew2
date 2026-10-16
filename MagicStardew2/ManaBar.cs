using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using MagicStardew.Spells;

namespace MagicStardew
{
    public class ManaData
    {
        public int Current { get; set; } = 200;
        public int Max { get; set; } = 200;
        public int NoSleepCounter { get; set; } = 0;
    }

    public class ManaBar
    {
        private readonly IModHelper helper;
        private readonly IMonitor monitor;
        private readonly ITranslationHelper i18n;
        private readonly ModConfig config;

        // ZNAK ZAPYTANIA: Pozwalamy, by tekstura i menedżer na starcie były nullami
        private Texture2D? manaBarTexture;
        private SpellManager? spellManager;

        private IReflectedField<bool> takingScreenshotField;
        private readonly Dictionary<long, ManaData> playerMana = new();

        private const string CurrentKey = "magicstardew.mana.current";
        private const string MaxKey = "magicstardew.mana.max";
        private const string NoSleepKey = "magicstardew.mana.noSleepCounter";
        private const string LastRegenDayKey = "magicstardew.mana.lastRegenDay";

        public ManaBar(IModHelper helper, IMonitor monitor, ModConfig config)
        {
            this.helper = helper;
            this.monitor = monitor;
            this.i18n = helper.Translation;
            this.config = config;
            takingScreenshotField = helper.Reflection.GetField<bool>(Game1.game1, "takingMapScreenshot");

            helper.Events.GameLoop.DayEnding += OnDayEnding;
            helper.Events.GameLoop.Saving += OnSaving;
            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
            helper.Events.Content.AssetRequested += OnAssetRequested;
            helper.Events.Content.AssetsInvalidated += OnAssetsInvalidated;
        }

        // ZNAK ZAPYTANIA PRZY object? sender (SMAPI tego wymaga)
        private void OnAssetsInvalidated(object? sender, AssetsInvalidatedEventArgs e)
        {
            foreach (var name in e.NamesWithoutLocale)
            {
                if (name.IsEquivalentTo("assets/mana_bar"))
                {
                    manaBarTexture = null;
                }
            }
        }

        private ManaData GetData(Farmer who)
        {
            if (!playerMana.TryGetValue(who.UniqueMultiplayerID, out var data))
            {
                data = new ManaData();
                playerMana[who.UniqueMultiplayerID] = data;
            }
            return data;
        }

        public void SetSpellManager(SpellManager manager)
        {
            this.spellManager = manager;
        }

        public void AddMana(Farmer who, int amount)
        {
            var data = GetData(who);
            data.Current = Math.Min(data.Max, data.Current + amount);
            SaveToModData(who);
        }

        public void UseMana(Farmer who, int amount)
        {
            var data = GetData(who);
            data.Current = Math.Max(0, data.Current - amount);
            SaveToModData(who);
        }

        public int GetCurrentMana(Farmer who) => GetData(who).Current;
        public int GetMaxMana(Farmer who) => GetData(who).Max;

        private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
        {
            if (e.NameWithoutLocale.IsEquivalentTo("assets/mana_bar"))
            {
                string texturePath;

                if (config.ManaBarRecolor == "Auto")
                {
                    bool hasStarblue = helper.ModRegistry.IsLoaded("Lita.StarblueValleyUnofficial") ||
                                       helper.ModRegistry.IsLoaded("Lita.StarblueValley");
                    bool hasStarryBlueUI = helper.ModRegistry.IsLoaded("Sqbr.StarryBlueUI");
                    bool hasDoraVPRUI = helper.ModRegistry.IsLoaded("Dora.VPRUI");
                    bool hasOvergrownFloweryInterface = helper.ModRegistry.IsLoaded("Maraluna.OvergrownFloweryInterface");
                    bool hasVictorian = helper.ModRegistry.IsLoaded("DBB.VictorianGothicUI");
                    bool hasSoleilsCozyInterface = helper.ModRegistry.IsLoaded("4pplecinnamon.SoleilsCozyInterface");
                    bool hasWitchyPurpleUIInterface = helper.ModRegistry.IsLoaded("Lynnea.WitchyPurpleUIInterface");
                    bool hasEarthyInterface = helper.ModRegistry.IsLoaded("DaisyNiko.EarthyInterface");
                    bool hasVintageInterfacev2 = helper.ModRegistry.IsLoaded("ManaKirel.VintageInterface2");

                    if (hasVictorian)
                    {
                        switch (config.VictorianManaBar)
                        {
                            case "JetBlack": texturePath = "assets/mb_JetBlack.png"; break;
                            case "OffWhite": texturePath = "assets/mb_OffWhite.png"; break;
                            case "SoftBrown": texturePath = "assets/mb_SoftBrown.png"; break;
                            case "Tea": texturePath = "assets/mb_Tea.png"; break;
                            default: texturePath = "assets/mana_bar.png"; break;
                        }
                    }
                    else if (hasStarryBlueUI) texturePath = "assets/mana_bar_Starry_Blue.png";
                    else if (hasStarblue) texturePath = "assets/mana_bar_StarblueValley.png";
                    else if (hasDoraVPRUI) texturePath = "assets/mana_bar_Dora.VPRUI.png";
                    else if (hasOvergrownFloweryInterface) texturePath = "assets/mana_bar_Overgrown_Flowery_Interface.png";
                    else if (hasSoleilsCozyInterface) texturePath = "assets/mana_bar_Soleils_Cozy_Interface.png";
                    else if (hasWitchyPurpleUIInterface) texturePath = "assets/mana_bar_WitchyPurpleUIInterface.png";
                    else if (hasEarthyInterface) texturePath = "assets/mb_earthy.png";
                    else if (hasVintageInterfacev2) texturePath = "assets/mb_Vintage_InterfaceV2.png";
                    else texturePath = "assets/mana_bar.png";
                }
                else
                {
                    switch (config.ManaBarRecolor)
                    {
                        case "StarblueValley": texturePath = "assets/mana_bar_StarblueValley.png"; break;
                        case "StarryBlueUI": texturePath = "assets/mana_bar_Starry_Blue.png"; break;
                        case "VPR-Inspired UI": texturePath = "assets/mana_bar_Dora.VPRUI.png"; break;
                        case "OvergrownFloweryInterface": texturePath = "assets/mana_bar_Overgrown_Flowery_Interface.png"; break;
                        case "SoleilsCozyInterface": texturePath = "assets/mana_bar_Soleils_Cozy_Interface.png"; break;
                        case "Victorian:JetBlack": texturePath = "assets/mb_JetBlack.png"; break;
                        case "Victorian:OffWhite": texturePath = "assets/mb_OffWhite.png"; break;
                        case "Victorian:SoftBrown": texturePath = "assets/mb_SoftBrown.png"; break;
                        case "Victorian:Tea": texturePath = "assets/mb_Tea.png"; break;
                        case "WitchyPurpleUIInterface": texturePath = "assets/mana_bar_WitchyPurpleUIInterface"; break;
                        case "VintageInterfacev2": texturePath = "assets/mb_Vintage_InterfaceV2.png"; break;
                        case "Earthy": texturePath = "assets/mb_earthy"; break;
                        default: texturePath = "assets/mana_bar.png"; break;
                    }
                }

                try
                {
                    e.LoadFromModFile<Texture2D>(texturePath, AssetLoadPriority.Medium);
                }
                catch
                {
                    e.LoadFromModFile<Texture2D>("assets/mana_bar.png", AssetLoadPriority.Medium);
                }
            }
        }

        private void OnDayEnding(object? sender, DayEndingEventArgs e)
        {
            foreach (var farmer in Game1.getAllFarmers())
                ApplyManaRegenOncePerDay(farmer);
        }

        private void ApplyManaRegenOncePerDay(Farmer farmer)
        {
            var data = GetData(farmer);
            int today = Game1.Date.TotalDays;

            if (farmer.modData.TryGetValue(LastRegenDayKey, out string last)
                && int.TryParse(last, out int lastDay)
                && lastDay == today)
            {
                return;
            }

            if (farmer.passedOut || farmer.health <= 0)
                data.NoSleepCounter++;
            else
                data.NoSleepCounter = 0;

            RecalculateMaxMana(farmer);

            double regenPercent;
            if (config.CustomManaRegen)
            {
                regenPercent = config.ManaRegenPercent / 100.0;
            }
            else
            {
                regenPercent = 0.25;
                if (data.NoSleepCounter == 1) regenPercent = 0.20;
                else if (data.NoSleepCounter == 2) regenPercent = 0.15;
                else if (data.NoSleepCounter >= 3) regenPercent = 0.10;
            }

            int regenAmount = (int)(data.Max * regenPercent);
            data.Current = Math.Min(data.Max, data.Current + regenAmount);

            SaveToModData(farmer);
            farmer.modData[LastRegenDayKey] = today.ToString();
        }

        private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
        {
            foreach (var farmer in Game1.getAllFarmers())
                LoadFromModData(farmer);

            if (spellManager != null)
            {
                foreach (var farmer in Game1.getAllFarmers())
                {
                    RecalculateMaxMana(farmer);
                    SaveToModData(farmer);
                }
            }
        }

        private void OnSaving(object? sender, SavingEventArgs e)
        {
            foreach (var farmer in Game1.getAllFarmers())
                SaveToModData(farmer);
        }

        private void SaveToModData(Farmer who)
        {
            var data = GetData(who);
            who.modData[CurrentKey] = data.Current.ToString();
            who.modData[MaxKey] = data.Max.ToString();
            who.modData[NoSleepKey] = data.NoSleepCounter.ToString();
        }

        private void LoadFromModData(Farmer who)
        {
            var data = GetData(who);

            if (who.modData.TryGetValue(CurrentKey, out string cur) && int.TryParse(cur, out int c))
                data.Current = c;
            else
                data.Current = 200;

            if (who.modData.TryGetValue(MaxKey, out string max) && int.TryParse(max, out int m))
                data.Max = m;
            else
                data.Max = 200;

            if (who.modData.TryGetValue(NoSleepKey, out string ns) && int.TryParse(ns, out int n))
                data.NoSleepCounter = n;
            else
                data.NoSleepCounter = 0;

            playerMana[who.UniqueMultiplayerID] = data;
        }

        public void RecalculateMaxMana(Farmer who)
        {
            var data = GetData(who);
            int baseMana = 200;
            int bonus = 0;

            if (spellManager != null)
            {
                foreach (var spell in spellManager.GetAllSpells())
                {
                    if (!spell.CanBeLeveledUp)
                        continue;

                    int useCount = spell.GetUseCount(who);
                    int spellBonus = Math.Min((useCount / 10) * 10, 40);
                    bonus += spellBonus;
                }
            }

            int calculated = baseMana + bonus;
            int cap = Math.Min(config.MaxManaCap, 5000);

            data.Max = Math.Min(calculated, cap);

            if (data.Current > data.Max)
                data.Current = data.Max;

            who.modData[MaxKey] = data.Max.ToString();
        }

        public void SetMana(Farmer who, int amount)
        {
            var data = GetData(who);
            data.Current = Math.Clamp(amount, 0, data.Max);
            SaveToModData(who);
        }

        public void OnRenderedHud(object? sender, RenderedHudEventArgs e)
        {
            var who = Game1.player;
            var data = GetData(who);

            // Zabezpieczenie przed nullem dla spellManager
            bool hasSpellBookEquipped = spellManager != null && spellManager.HasSpellBookEquipped(who);
            bool hasSpellBookInInventory = who.Items.ContainsId($"(O){SpellManager.SpellBookItemId}");

            if (!hasSpellBookEquipped && !hasSpellBookInInventory)
                return;

            if (!Game1.displayHUD || Game1.currentLocation == null || Game1.eventUp)
                return;
            if (takingScreenshotField.GetValue())
                return;

            if (manaBarTexture == null)
                manaBarTexture = helper.GameContent.Load<Texture2D>("assets/mana_bar");

            SpriteBatch spriteBatch = e.SpriteBatch;

            int baseWidth = 48;
            int totalHeight = 224;

            if (config.DynamicManaBarSize)
                totalHeight += Math.Min((data.Max - 200) / 10 * 5, 100);

            int x;
            int y;

            if (config.AutoManaBarPosition)
            {
                x = Game1.uiViewport.Width - 112;

                if (Game1.showingHealthBar)
                    x -= 56;

                if (ModEntry.IsDaLionGaugeVisible != null && ModEntry.IsDaLionGaugeVisible())
                    x -= 56;

                if (ModEntry.HasSurvivalisti)
                    x -= 124;

                try
                {
                    if (helper.ModRegistry.IsLoaded("FlyingTNT.Swim"))
                    {
                        var swimType = Type.GetType("Swim.ModEntry, Swim");
                        var field = swimType?.GetProperty("isUnderwater", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                        if (field != null)
                        {
                            var perScreenObj = field.GetValue(null);
                            var valueProp = perScreenObj?.GetType().GetProperty("Value");
                            bool isUnderwater = (bool?)valueProp?.GetValue(perScreenObj) ?? false;

                            if (isUnderwater)
                                x -= 56;
                        }
                    }
                }
                catch (Exception ex)
                {
                    monitor.Log($"[ManaBar] Failed to check Swim.isUnderwater: {ex.Message}", LogLevel.Trace);
                }

                y = Game1.uiViewport.Height - totalHeight - 16;
            }
            else
            {
                x = config.CustomManaBarX;
                y = config.CustomManaBarY;
            }

            int topCapHeight = 60;
            int bottomCapHeight = 74;
            int middleSliceHeight = 30;

            spriteBatch.Draw(manaBarTexture, new Rectangle(x, y, baseWidth, topCapHeight), new Rectangle(0, 0, baseWidth, topCapHeight), Color.White);

            int middleHeight = totalHeight - topCapHeight - bottomCapHeight;
            int middleStartY = y + topCapHeight;
            while (middleHeight > 0)
            {
                int drawHeight = Math.Min(middleSliceHeight, middleHeight);
                spriteBatch.Draw(manaBarTexture, new Rectangle(x, middleStartY, baseWidth, drawHeight), new Rectangle(0, 120, baseWidth, drawHeight), Color.White);
                middleStartY += drawHeight;
                middleHeight -= drawHeight;
            }

            spriteBatch.Draw(manaBarTexture, new Rectangle(x, y + totalHeight - bottomCapHeight, baseWidth, bottomCapHeight), new Rectangle(0, 150, baseWidth, bottomCapHeight), Color.White);

            int fillTopLimit = y + 48;
            int fillBottomLimit = y + totalHeight - 9;
            int availableHeight = fillBottomLimit - fillTopLimit;

            float manaRatio = (float)data.Current / data.Max;
            int filledPixels = (int)(availableHeight * manaRatio);

            int fillTop = fillBottomLimit - filledPixels;

            Rectangle fillRect = new Rectangle(x + 12, fillTop, 24, filledPixels);
            spriteBatch.Draw(Game1.fadeToBlackRect, fillRect, new Color(0x64, 0x95, 0xEC));

            if (filledPixels > 4)
            {
                Rectangle topCap = new Rectangle(fillRect.X, fillRect.Y, fillRect.Width, 4);
                spriteBatch.Draw(Game1.fadeToBlackRect, topCap, new Color(0x5F, 0x8A, 0xD7));
            }

            Rectangle manaBarHitbox = new Rectangle(x, y, baseWidth, totalHeight);
            Point mouse = Game1.getMousePosition();
            if (manaBarHitbox.Contains(mouse))
            {
                string hoverText = $"{data.Current}/{data.Max}";
                Vector2 textSize = Game1.dialogueFont.MeasureString(hoverText);

                Vector2 textPosition = new Vector2(x - textSize.X - 10, y + totalHeight / 2 - textSize.Y / 2 - 22);

                spriteBatch.DrawString(Game1.dialogueFont, hoverText, textPosition, Color.White);
            }
        }
    }
}