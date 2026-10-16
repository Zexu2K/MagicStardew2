// Consolidated from: GameStateQueries.cs, CutsceneImageOverlay.cs, MagicSpellBookBoard.cs
// Original class implementations preserved.
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using StardewModdingAPI.Events;
using StardewModdingAPI;
using StardewValley.Delegates;
using StardewValley.Menus;
using StardewValley.SpecialOrders;
using StardewValley;
using System;

// ===== Original file: GameStateQueries.cs =====
namespace MagicStardew
{
    public static class GameStateQueries
    {
        private static IMonitor Monitor;

        public static void Register(IMonitor monitor)
        {
            Monitor = monitor;

            // Czysta rejestracja w SDV 1.6 bez wywoływania dekompilatorowych delegatów i __methodptr
            GameStateQuery.Register("MagicStardew_HealthPercent", CheckHealthPercent);
        }

        private static bool CheckHealthPercent(string[] query, GameStateQueryContext context)
        {
            // Poziom logowania 5 u dekompilatora to zwykle wysoki poziom, ale takie zapytania najlepiej dać w Trace, 
            // żeby nie spamowały konsoli przy każdej klatce gry
            Monitor.Log("Sprawdzam HP: " + string.Join(" ", query), LogLevel.Trace);

            // Poprawne użycie ArgUtility.TryGet z użyciem argumentów typu "out"
            if (!ArgUtility.TryGet(query, 1, out string targetName, out string error1, true, "string targetName") ||
                !ArgUtility.TryGet(query, 2, out string op, out string error2, true, "string op") ||
                !ArgUtility.TryGetFloat(query, 3, out float targetValue, out string error3, "float targetValue"))
            {
                return false;
            }

            // Rozwikłany, czytelny warunek wyboru farmera
            Farmer farmer = context.Player; // Domyślnie obecny gracz

            if (string.Equals(targetName, "Host", StringComparison.OrdinalIgnoreCase))
            {
                farmer = Game1.MasterPlayer;
            }
            else if (string.Equals(targetName, "Current", StringComparison.OrdinalIgnoreCase))
            {
                farmer = context.Player;
            }

            // Zabezpieczenie przed błędem w multi i dzieleniem przez 0
            if (farmer == null || farmer.maxHealth <= 0)
                return false;

            // Obliczanie wartości procentowej od 0.0 do 1.0 (np. 0.5 to 50% zdrowia)
            float currentHealthPercent = (float)farmer.health / farmer.maxHealth;

            // Czysty switch na operatorze bez dziwnego rzutowania na (double)
            switch (op)
            {
                case "<":
                    return currentHealthPercent < targetValue;
                case ">":
                    return currentHealthPercent > targetValue;
                case "<=":
                    return currentHealthPercent <= targetValue;
                case ">=":
                    return currentHealthPercent >= targetValue;
                default:
                    return false;
            }
        }
    }
}

// ===== Original file: CutsceneImageOverlay.cs =====
namespace MagicStardew
{
    public static class CutsceneImageOverlay
    {
        private static IModHelper Helper;
        private static IMonitor Monitor;
        private static Texture2D OverlayTexture;
        public static bool IsActive = false;

        public static void Register(IModHelper helper, IMonitor monitor)
        {
            Helper = helper;
            Monitor = monitor;

            // Ładujemy Twój obrazek H7.png
            try
            {
                OverlayTexture = helper.ModContent.Load<Texture2D>("assets/H7.png");
            }
            catch
            {
                monitor.Log("Nie udało się załadować assets/H7.png!", LogLevel.Error);
            }

            // Rejestrujemy komendy eventowe (włączenie i wyłączenie)
            Event.RegisterCommand("MagicStardew_ShowH7", ShowOverlay);
            Event.RegisterCommand("MagicStardew_HideH7", HideOverlay);

            // Podpinamy się pod rysowanie interfejsu (żeby obrazek zawsze był na samej górze)
            helper.Events.Display.RenderedHud += OnRenderedHud;
        }

        private static void ShowOverlay(Event @event, string[] args, EventContext context)
        {
            IsActive = true;
            @event.CurrentCommand++;
        }

        private static void HideOverlay(Event @event, string[] args, EventContext context)
        {
            IsActive = false;
            @event.CurrentCommand++;
        }

        private static void OnRenderedHud(object sender, RenderedHudEventArgs e)
        {
            // Rysujemy tylko wtedy, gdy event aktywował nakładkę i tekstura istnieje
            if (!IsActive || !Context.IsWorldReady || OverlayTexture == null)
                return;

            SpriteBatch b = e.SpriteBatch;

            // 1. Rysujemy przyciemnienie (kinowy efekt skupienia tła) - 60% czerni
            // Używamy Game1.staminaRect, bo to po prostu 1x1 czysty biały piksel
            Rectangle screenRect = new Rectangle(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height);
            b.Draw(Game1.staminaRect, screenRect, Color.Black * 0.6f);

            // 2. Obliczamy środek ekranu
            Vector2 centerScreen = new Vector2(Game1.uiViewport.Width / 2f, Game1.uiViewport.Height / 2f);

            // 3. Obliczamy środek obrazka
            Vector2 origin = new Vector2(OverlayTexture.Width / 2f, OverlayTexture.Height / 2f);

            // 4. Skala obrazka. W Stardew Valley pixel-arty zazwyczaj skaluje się x4. 
            // Jeśli obrazek H7 to HD (np. pełny rysunek z anime), zmień '4f' na '1f'.
            float scale = 4f;

            // Rysujemy obrazek H7 na samym środku ekranu!
            b.Draw(OverlayTexture, centerScreen, null, Color.White, 0f, origin, scale, SpriteEffects.None, 1f);
        }
    }
}

// ===== Original file: MagicSpellBookBoard.cs =====
namespace MagicStardew.Spells
{
    internal class MagicSpellBookBoard : SpecialOrdersBoard
    {
        private static IModHelper Helper;
        private static IMonitor Monitor;

        internal static void AddEventHooks(IModHelper helper, IMonitor monitor)
        {
            Helper = helper;
            Monitor = monitor;

            // Czysta subskrypcja zdarzeń bez zbędnych delegatów dekompilatora
            helper.Events.GameLoop.GameLaunched += RegisterTileAction;
            helper.Events.GameLoop.DayStarted += UpdateSpellListOnWeekStart;

            Monitor.Log("[SpellBookBoard] Event hooks registered.", LogLevel.Info);
        }

        private static void UpdateSpellListOnWeekStart(object sender, DayStartedEventArgs e)
        {
            // Aktualizujemy zadania tylko jako Host gry i tylko w poniedziałki (1, 8, 15, 22 dzień pory roku)
            if (!Game1.IsMasterGame || (Game1.dayOfMonth != 1 && Game1.dayOfMonth != 8 && Game1.dayOfMonth != 15 && Game1.dayOfMonth != 22))
                return;

            SpecialOrder.UpdateAvailableSpecialOrders("SpellBook", true);
            Monitor.Log("[SpellBookBoard] Special Orders updated for the week.", LogLevel.Info);
        }

        private static void RegisterTileAction(object sender, GameLaunchedEventArgs e)
        {
            // Rejestracja akcji na kafelku, bez sztucznego Func<>
            GameLocation.RegisterTileAction("MagicStardew.SpellBook", OpenSpellBookMenu);
            Monitor.Log("[SpellBookBoard] Tile action registered: MagicStardew.SpellBook", LogLevel.Info);
        }

        private static bool OpenSpellBookMenu(GameLocation loc, string[] args, Farmer who, Point tile)
        {
            Monitor.Log("[SpellBookBoard] SpellBook clicked!", LogLevel.Info);

            // Otwieramy menu tablicy zleceń z typem "SpellBook"
            Game1.activeClickableMenu = new MagicSpellBookBoard();

            return true; // true oznacza, że akcja została poprawnie wykonana
        }

        // Konstruktor wywołujący bazową klasę SpecialOrdersBoard z typem tablicy
        internal MagicSpellBookBoard() : base("SpellBook")
        {
        }
    }
}
