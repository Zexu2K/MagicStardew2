// Consolidated from: AddEarthquakeCommand.cs, AddHealFieldCommand.cs, AddLightCommand.cs, SpawnLivingChestCommand.cs
// Original class implementations preserved.
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley.Buffs;
using StardewValley.Delegates;
using StardewValley.Tools;
using StardewValley;
using System.Linq;
using System;

// ===== Original file: AddEarthquakeCommand.cs =====
namespace MagicStardew
{
    public static class AddEarthquakeCommand
    {
        private static IMonitor Monitor;

        public static void Register(IMonitor monitor)
        {
            Monitor = monitor;
            // Czysta rejestracja komendy eventowej w Stardew Valley 1.6+
            Event.RegisterCommand("MagicStardew_Spell_Earthquake", EarthquakeEffect);
        }

        public static void EarthquakeEffect(Event @event, string[] args, EventContext context)
        {
            // Ostrzeżenie, jeśli skrypt eventu nie podał argumentów
            if (args.Length < 2)
            {
                Monitor.Log("MagicStardew_Spell_Earthquake: Wymagana nazwa postaci (np. farmer lub Ashara).", LogLevel.Warn);
            }

            GameLocation currentLocation = Game1.currentLocation;

            // Efekty wizualne i dźwiękowe trzęsienia ziemi
            Game1.screenGlow = true;
            Game1.screenGlowAlpha = 0.75f;
            Game1.screenGlowColor = Color.White;
            Game1.flashAlpha = 1f;
            Game1.playSound("explosion");

            // Tworzymy kilof na poziomie Iridium (poziom 4), aby mógł niszczyć najtwardsze kamienie
            Pickaxe pickaxe = new Pickaxe();
            pickaxe.UpgradeLevel = 4;

            int num = 10;
            float stamina = Game1.player.stamina; // Zapisujemy staminę, żeby gracz jej nie tracił przy automatycznym uderzaniu

            // Symulujemy wielokrotne uderzenia (żeby zniszczyć nawet duże skały)
            for (int index = 0; index < num; ++index)
            {
                // Używamy ToList(), ponieważ niszczenie obiektów zmienia kolekcję w trakcie iteracji
                foreach (Vector2 tile in currentLocation.objects.Keys.ToList())
                {
                    if (currentLocation.objects.TryGetValue(tile, out StardewValley.Object obj) && obj != null)
                    {
                        string name = obj.Name?.ToLower() ?? "";

                        if (name.Contains("stone") || name.Contains("node"))
                        {
                            // Uderzenie kilofem
                            pickaxe.DoFunction(currentLocation, (int)(tile.X * 64f), (int)(tile.Y * 64f), 0, Game1.player);

                            // Zwracamy staminę pobraną przez użycie narzędzia
                            Game1.player.stamina = stamina;

                            // Efekt pyłu po zniszczeniu i dźwięk uderzenia
                            currentLocation.temporarySprites.Add(new TemporaryAnimatedSprite(362, tile * 64f, Color.Gray, 8, false, 100f, 0, -1, -1f, -1, 0));
                            currentLocation.playSound("hammer");
                        }
                    }
                }
            }

            // Popchnięcie skryptu eventu do przodu, żeby cutscenka się nie zawiesiła
            @event.CurrentCommand++;
        }
    }
}

// ===== Original file: AddHealFieldCommand.cs =====
namespace MagicStardew
{
    public static class AddHealFieldCommand
    {
        private static IMonitor Monitor;
        private static Texture2D AuraTexture;
        private const int Radius = 384;

        public static void Register(IMonitor monitor, IModHelper helper)
        {
            Monitor = monitor;
            try
            {
                AuraTexture = helper.ModContent.Load<Texture2D>("assets/green_circle.png");
            }
            catch (Exception ex)
            {
                // (LogLevel) 4 z dekompilatora to po prostu LogLevel.Error
                Monitor.Log($"Nie udało się załadować assets/green_circle.png! Błąd: {ex.Message}", LogLevel.Error);
            }

            // Czysta rejestracja komendy, bez dekompilatorowego __methodptr
            Event.RegisterCommand("MagicStardew_Spell_HealField", SpawnHealAura);
        }

        public static void SpawnHealAura(Event @event, string[] args, EventContext context)
        {
            // Jeśli tekstura się nie załadowała, po prostu pomiń efekt i idź dalej, żeby nie zawiesić eventu
            if (AuraTexture == null)
            {
                @event.CurrentCommand++;
                return;
            }

            // Domyślnie celujemy w gracza
            Character character = Game1.player;

            // Sprawdzamy, czy w komendzie podano konkretny cel (np. NPC)
            if (args.Length > 1)
            {
                string targetName = args[1];
                if (!string.Equals(targetName, "farmer", StringComparison.OrdinalIgnoreCase))
                {
                    NPC npc = Game1.currentLocation.getCharacterFromName(targetName);
                    if (npc != null)
                    {
                        character = npc;
                    }
                }
            }

            // --- RYSOWANIE AURY (ZIELONEGO KOŁA) ---
            Vector2 standingPosition = character.getStandingPosition();
            float scale = 768f / AuraTexture.Width;

            // Normalne operatory matematyczne zamiast Vector2.op_Subtraction
            Vector2 auraPos = standingPosition - new Vector2(AuraTexture.Width * scale / 2f);

            Game1.currentLocation.temporarySprites.Add(new TemporaryAnimatedSprite()
            {
                texture = AuraTexture,
                sourceRect = new Rectangle(0, 0, AuraTexture.Width, AuraTexture.Height),
                position = auraPos,
                animationLength = 1,
                interval = 99999f,
                totalNumberOfLoops = 9999,
                scale = scale,
                layerDepth = 1f / 1000f, // 0.001f
                alpha = 0.5f,
                rotation = 0.0f,
                rotationChange = 0.01f,
                timeBasedMotion = true
            });

            // --- LECZENIE GRACZA ---
            int healAmount = Game1.player.maxHealth - Game1.player.health;
            Game1.player.health = Game1.player.maxHealth;

            // W SDV 1.6 maxStamina to klasa sieciowa, pobieramy po prostu jej .Value
            Game1.player.stamina = Game1.player.maxStamina.Value;

            // --- TEKST LECZENIA (+X) ---
            if (healAmount > 0)
            {
                TemporaryAnimatedSprite healTextSprite = new TemporaryAnimatedSprite(
                    textureName: "LooseSprites\\Cursors",
                    sourceRect: new Rectangle(293, 360, 10, 10),
                    animationInterval: 999f,
                    animationLength: 1,
                    numberOfLoops: 0,
                    position: Game1.player.getStandingPosition() + new Vector2(-16f, -64f), // Normalne dodawanie
                    flicker: false,
                    flipped: false
                );

                healTextSprite.text = $"+{healAmount}";
                healTextSprite.color = Color.LightGreen;
                healTextSprite.motion = new Vector2(0.0f, -1f);
                healTextSprite.layerDepth = 1f;
                healTextSprite.alpha = 1f;
                healTextSprite.alphaFade = 0.01f;
                healTextSprite.scale = 1f;

                Game1.currentLocation.temporarySprites.Add(healTextSprite);
            }

            // Normalne odtworzenie dźwięku (bez pustego new int?() z dekompilatora)
            Game1.playSound("healSound");

            // Popchnięcie skryptu do przodu
            @event.CurrentCommand++;
        }
    }
}

// ===== Original file: AddLightCommand.cs =====
namespace MagicStardew
{
    public static class AddLightCommand
    {
        private static IMonitor Monitor;

        public static void Register(IMonitor monitor)
        {
            Monitor = monitor;

            // Czysta rejestracja komendy w SMAPI / SDV 1.6+
            Event.RegisterCommand("MagicStardew_AddLight", AddLight);
        }

        public static void AddLight(Event @event, string[] args, EventContext context)
        {
            // Ostrzeżenie i zabezpieczenie: jeśli w skrypcie nie podano X i Y, przerywamy, 
            // ale przesuwamy event, by nie zawiesić cutscenki.
            if (args.Length < 3)
            {
                Monitor.Log("MagicStardew_AddLight: Za mało argumentów. Wymagane co najmniej: X Y", LogLevel.Error);
                @event.CurrentCommand++;
                return;
            }

            // Parsowanie koordynatów
            if (!float.TryParse(args[1], out float tileX) || !float.TryParse(args[2], out float tileY))
            {
                Monitor.Log("MagicStardew_AddLight: Błąd w koordynatach (muszą być liczbami).", LogLevel.Error);
                @event.CurrentCommand++;
                return;
            }

            // Standardowe tworzenie wektora (środek kafelka to X*64 + 32, Y*64 + 32)
            Vector2 position = new Vector2((tileX * 64f) + 32f, (tileY * 64f) + 32f);

            // Domyślne wartości
            float radius = 2f;
            Color color = new Color(255, 220, 100);

            // Opcjonalny argument nr 3 - promień (Radius)
            if (args.Length >= 4 && !float.TryParse(args[3], out radius))
            {
                Monitor.Log("MagicStardew_AddLight: Błędny format promienia.", LogLevel.Warn);
            }

            // Opcjonalne argumenty 4, 5, 6 - kolor RGB
            if (args.Length >= 7)
            {
                int.TryParse(args[4], out int r);
                int.TryParse(args[5], out int g);
                int.TryParse(args[6], out int b);
                color = new Color(r, g, b);
            }

            // Tworzymy unikalne ID dla światła
            string lightId = "MagicLight_" + Guid.NewGuid().ToString();

            // Tworzenie źródła światła (ID, textureIndex, position, radius, color, lightContext, playerID, locationName)
            LightSource lightSource = new LightSource(
                id: lightId,
                textureIndex: 1,
                position: position,
                radius: radius,
                color: color,
                lightContext: LightSource.LightContext.None,
                playerID: 0L
            );

            // Czyste dodanie światła do lokacji
            Game1.currentLightSources[lightSource.Id] = lightSource;

            // Przesunięcie eventu do przodu
            @event.CurrentCommand++;
        }
    }
}

// ===== Original file: SpawnLivingChestCommand.cs =====
namespace MagicStardew
{
    public static class LivingChestEventCommands
    {
        private static IMonitor Monitor;
        private static Texture2D ChestTexture;

        public static void Register(IMonitor monitor, IModHelper helper)
        {
            Monitor = monitor;

            // Ładujemy teksturę z góry, żeby móc zrobić z niej sztucznego sprite'a w evencie
            ChestTexture = helper.ModContent.Load<Texture2D>("assets/living_chest.png");

            Event.RegisterCommand("MagicStardew_FakeLivingChest", SpawnFakeChest);
            Event.RegisterCommand("MagicStardew_GiveLivingChestBuff", GiveChestBuff);
        }

        public static void SpawnFakeChest(Event @event, string[] args, EventContext context)
        {
            if (args.Length >= 3 && float.TryParse(args[1], out float tileX) && float.TryParse(args[2], out float tileY))
            {
                Vector2 position = new Vector2(tileX * 64f, tileY * 64f);

                // 1. Magiczny efekt pojawienia się (gwiazdki)
                Game1.currentLocation.temporarySprites.Add(new TemporaryAnimatedSprite(
                    rowInAnimationTexture: 10,
                    position: position + new Vector2(0, -42),
                    color: Color.LightBlue,
                    animationLength: 8,
                    flipped: false,
                    animationInterval: 100f
                ));

                // 2. Tworzymy sztuczną skrzynię-aktora, która po prostu skacze w miejscu
                var fakeChest = new TemporaryAnimatedSprite(
                    textureName: "",
                    sourceRect: new Rectangle(0, 0, 16, 16),
                    animationInterval: 120f,
                    animationLength: 18,
                    numberOfLoops: 99999, // Pętli się w nieskończoność przez cały event
                    position: position,
                    flicker: false,
                    flipped: false
                )
                {
                    texture = ChestTexture,
                    scale = 4f,
                    layerDepth = (position.Y + 64f) / 10000f,
                    id = 999888777 // Nadajemy unikalne ID, żeby łatwo było ją potem skasować
                };

                // 3. Dodajemy cień pod skrzynią dla realizmu
                var shadow = new TemporaryAnimatedSprite(
                    textureName: "",
                    sourceRect: new Rectangle(0, 0, 12, 7),
                    animationInterval: 9999f,
                    animationLength: 1,
                    numberOfLoops: 99999,
                    position: position + new Vector2(-12f * 3.5f / 2f + 30f, -7f * 3.5f / 2f + 60f),
                    flicker: false,
                    flipped: false
                )
                {
                    texture = Game1.shadowTexture,
                    color = Color.White * 0.7f,
                    scale = 3.5f,
                    layerDepth = fakeChest.layerDepth - 0.0001f,
                    id = 999888778
                };

                Game1.currentLocation.temporarySprites.Add(fakeChest);
                Game1.currentLocation.temporarySprites.Add(shadow);
                Game1.playSound("wand");
            }

            @event.CurrentCommand++;
        }

        public static void GiveChestBuff(Event @event, string[] args, EventContext context)
        {
            Farmer player = Game1.player;
            string buffId = "livingchest_buff";
            int duration = 1_800_000; // 30 minut w milisekundach

            // Skasowanie sztucznej skrzyni, jeśli nadal istnieje
            Game1.currentLocation.removeTemporarySpritesWithID(999888777);
            Game1.currentLocation.removeTemporarySpritesWithID(999888778);

            // Dodanie buffa
            if (!player.hasBuff(buffId))
            {
                player.applyBuff(new Buff(
                    id: buffId,
                    displayName: "Living Chest",
                    iconTexture: ModEntry.SpellIcons,
                    iconSheetIndex: 35,
                    duration: duration,
                    source: "Event"
                ));

                Game1.playSound("yoba");
            }

            @event.CurrentCommand++;
        }
    }
}
