// Consolidated from: AsharaHouse.cs, AsharaOutside.cs
// Original class implementations preserved.
using Microsoft.Xna.Framework;
using StardewModdingAPI.Events;
using StardewModdingAPI;
using StardewValley.BellsAndWhistles;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;
using StardewValley;
using System;
using xTile.Layers;
using xTile.Tiles;

// ===== Original file: AsharaHouse.cs =====
// AsharaHouse.cs
namespace MagicStardew
{
    /// <summary>
    /// Scalona klasa zarządzająca oświetleniem, kominkiem, grzejnikami oraz interakcją z magicznym kafelkiem w lokacji AsharaHouse.
    /// </summary>
    public class AsharaHouse
    {
        private IMonitor Monitor = null!;
        private IModHelper Helper = null!;

        private const string LocationName = "MagicStardew_AsharaHouse";

        // ---- dane dla kominka ----
        private static readonly Point[] FireplaceTiles = new[]
        {
            new Point(12, 22), new Point(13, 22),
            new Point(12, 23), new Point(13, 23)
        };

        private static readonly int[,] NightTileIndexes = {
            {462, 463},
            {495, 496}
        };

        private const string BaseLayerName = "Buildings";
        private const string OverlayLayerName = "Buildings1";
        private bool tilesModified;
        private StaticTile[] overlayTiles = new StaticTile[4];

        // ambient / dźwięk kominka
        private bool fireAmbientActive = false;
        private readonly Vector2 fireSoundTile = new Vector2(13, 23);

        // ---- dane dla światła ----
        private static readonly Vector2 LanternTilePos = new(10f, 4f);
        private const float LightRadius = 5f;
        private static readonly Color LightColor = new(0, 50, 170);
        private const string LightId = "MagicStardew_AsharaHouse_Lantern";

        // ---- dane dla losowania przedmiotu ----
        private readonly Point RewardTile = new(20, 23);
        private bool hasDrawnToday = false;
        private Random rng = new Random();

        // współrzędne grup kafelków grzejników
        private static readonly Point[][] HeaterGroups = new[]
        {
        new[] { new Point(9, 8), new Point(10, 8), new Point(11, 8) },
        new[] { new Point(12, 17), new Point(13, 17), new Point(14, 17) },
        new[] { new Point(31, 29), new Point(32, 29), new Point(33, 29) },
        new[] { new Point(41, 29), new Point(42, 29), new Point(43, 29) },
        new[] { new Point(30, 38), new Point(31, 38), new Point(32, 38) }
    };

        // kafelki nakładek – nieanimowane wersje grzejników
        private static readonly int[] HeaterCoverIds = { 458, 459, 460 };
        private bool heaterTilesCovered;
        private StaticTile[][] heaterOverlayTiles = new StaticTile[5][];

        // ---- publiczne API ----
        public void Initialize(IModHelper helper, IMonitor monitor)
        {
            Helper = helper;
            Monitor = monitor;

            helper.Events.GameLoop.TimeChanged += OnTimeChanged;
            helper.Events.Player.Warped += OnWarped;
            helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
            helper.Events.Input.ButtonPressed += OnButtonPressed;
            helper.Events.GameLoop.DayStarted += OnDayStarted;
        }

        // ---- event handlers ----
        private void OnWarped(object? sender, WarpedEventArgs e)
        {
            if (!e.IsLocalPlayer) return;
            if (e.NewLocation?.NameOrUniqueName != LocationName) return;

            UpdateLightState(Game1.timeOfDay);
            UpdateFireplaceState(Game1.timeOfDay);
            UpdateHeaterState(Game1.timeOfDay);
        }

        private void OnTimeChanged(object? sender, TimeChangedEventArgs e)
        {
            UpdateLightState(e.NewTime);
            UpdateFireplaceState(e.NewTime);
            UpdateHeaterState(e.NewTime);
        }

        private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
        {
            if (!Context.IsPlayerFree) return;
            var loc = Game1.currentLocation;
            if (loc?.NameOrUniqueName != LocationName) return;

            if (Game1.timeOfDay >= 2100)
            {
                if (!fireAmbientActive)
                {
                    AmbientLocationSounds.addSound(fireSoundTile, AmbientLocationSounds.sound_cracklingFire);
                    fireAmbientActive = true;
                }
            }
            else
            {
                if (fireAmbientActive)
                {
                    AmbientLocationSounds.removeSound(fireSoundTile);
                    fireAmbientActive = false;
                }
            }
        }

        private void OnDayStarted(object? sender, DayStartedEventArgs e)
        {
            hasDrawnToday = false;
        }

        // ---- interakcja z kafelkiem 20,23 ----
        private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            if (!Context.IsWorldReady || Game1.currentLocation?.NameOrUniqueName != LocationName)
                return;

            if (!e.Button.IsActionButton() && !e.Button.IsUseToolButton())
                return;

            Vector2 tile = e.Cursor.GrabTile;
            if ((int)tile.X == RewardTile.X && (int)tile.Y == RewardTile.Y)
            {
                TryDrawReward();
            }
        }

        private void TryDrawReward()
        {
            if (hasDrawnToday)
                return;

            hasDrawnToday = true;
            int roll = rng.Next(100);

            if (roll < 50)
                return;
            else if (roll < 85)
                GiveItem(220);
            else
                GiveItem(221);
        }

        private void GiveItem(int itemId)
        {
            try
            {
                var obj = new StardewValley.Object(itemId.ToString(), 1);
                Game1.player.addItemByMenuIfNecessary(obj);
            }
            catch (Exception ex)
            {
                Monitor.Log($"[ERROR] {itemId}: {ex.Message}", LogLevel.Error);
            }
        }

        // ---- LIGHT ----
        private void UpdateLightState(int time)
        {
            bool isNight = time >= 1900 || time < 600;
            if (isNight) AddLightSourceToGame();
            else RemoveLightSource();
        }

        private void AddLightSourceToGame()
        {
            var loc = Game1.getLocationFromName(LocationName);
            if (loc == null) return;
            if (Game1.currentLocation?.NameOrUniqueName != LocationName) return; // 🔥 DODAJ TO

            var pixelPos = new Vector2((LanternTilePos.X + 0.5f) * 64f, (LanternTilePos.Y + 0.5f) * 64f);
            var light = new LightSource(
                id: LightId,
                textureIndex: LightSource.lantern,
                position: pixelPos,
                radius: LightRadius,
                color: LightColor,
                lightContext: LightSource.LightContext.None,
                playerID: Game1.player.UniqueMultiplayerID
            );

            try { Game1.currentLightSources[LightId] = light; }
            catch
            {
                RemoveLightSource();
                try { Game1.currentLightSources.Add(LightId, light); }
                catch { }
            }
        }


        private void RemoveLightSource()
        {
            if (Game1.currentLightSources.ContainsKey(LightId))
                Game1.currentLightSources.Remove(LightId);
        }

        // ---- FIREPLACE ----
        private void UpdateFireplaceState(int time)
        {
            var loc = Game1.getLocationFromName(LocationName);
            if (loc == null) return;

            if (time < 2100)
            {
                if (!tilesModified)
                {
                    ApplyNightTiles(loc);
                    tilesModified = true;
                }
            }
            else
            {
                if (tilesModified)
                {
                    RemoveNightTiles(loc);
                    tilesModified = false;
                }
            }
        }

        private void ApplyNightTiles(GameLocation loc)
        {
            var baseLayer = loc.Map.GetLayer(BaseLayerName);
            var overlayLayer = loc.Map.GetLayer(OverlayLayerName);
            if (baseLayer == null || overlayLayer == null) return;

            var baseTile = baseLayer.Tiles[12, 23];
            if (baseTile == null) return;
            var tilesheet = baseTile.TileSheet;

            int idx = 0;
            for (int y = 0; y < 2; y++)
                for (int x = 0; x < 2; x++)
                {
                    var p = FireplaceTiles[idx];
                    overlayTiles[idx] = new StaticTile(overlayLayer, tilesheet, BlendMode.Alpha, NightTileIndexes[y, x]);
                    overlayLayer.Tiles[p.X, p.Y] = overlayTiles[idx];
                    idx++;
                }
        }

        private void RemoveNightTiles(GameLocation loc)
        {
            var overlayLayer = loc.Map.GetLayer(OverlayLayerName);
            if (overlayLayer == null) return;
            foreach (var tile in FireplaceTiles)
                overlayLayer.Tiles[tile.X, tile.Y] = null;
        }

        // ---- HEATERS ----

        private void UpdateHeaterState(int time)
        {
            var loc = Game1.getLocationFromName(LocationName);
            if (loc == null) return;

            string season = Game1.currentSeason;
            bool isColdSeason = season == "winter" || (season == "fall" && time >= 1800);

            if (isColdSeason)
            {
                // zimno → grzejniki włączone → usuwamy nakładki
                if (heaterTilesCovered)
                {
                    RemoveHeaterCovers(loc);
                    heaterTilesCovered = false;
                }
            }
            else
            {
                // ciepło → grzejniki wyłączone → nakładamy statyczne kafelki
                if (!heaterTilesCovered)
                {
                    ApplyHeaterCovers(loc);
                    heaterTilesCovered = true;
                }
            }
        }

        private void ApplyHeaterCovers(GameLocation loc)
        {
            var front1 = loc.Map.GetLayer("Front1");
            var front = loc.Map.GetLayer("Front");
            if (front1 == null || front == null) return;

            for (int g = 0; g < HeaterGroups.Length; g++)
            {
                var group = HeaterGroups[g];
                heaterOverlayTiles[g] = new StaticTile[group.Length];

                for (int i = 0; i < group.Length && i < HeaterCoverIds.Length; i++)
                {
                    var p = group[i];
                    var baseTile = front.Tiles[p.X, p.Y];
                    if (baseTile == null) continue;

                    var sheet = baseTile.TileSheet;
                    var cover = new StaticTile(front1, sheet, BlendMode.Alpha, HeaterCoverIds[i]);
                    heaterOverlayTiles[g][i] = cover;
                    front1.Tiles[p.X, p.Y] = cover;
                }
            }
        }

        private void RemoveHeaterCovers(GameLocation loc)
        {
            var front1 = loc.Map.GetLayer("Front1");
            if (front1 == null) return;

            foreach (var group in HeaterGroups)
                foreach (var p in group)
                    front1.Tiles[p.X, p.Y] = null;
        }

    }
}

// ===== Original file: AsharaOutside.cs =====
// AsharaOutside.cs
namespace MagicStardew
{
    public class AsharaOutside
    {
        private IMonitor Monitor = null!;
        private IModHelper Helper = null!;

        private const string LocationName = "MagicStardew_AsharaHouse_Outside";

        // dane lampy
        private static readonly Vector2 LampTilePos = new(12f, 13f);
        private const float LampRadius = 4f;
        private static readonly Color LampColor = new(255, 240, 180);
        private const string LampLightId = "MagicStardew_AsharaOutside_Lamp";

        // dane świateł okien
        private static readonly Vector2 Window1TilePos = new(9f, 13f);
        private static readonly Vector2 Window2TilePos = new(17f, 14f);
        private const float WindowRadius = 2.8f;
        private static readonly Color WindowColor = new(255, 220, 160);
        private const string Window1Id = "MagicStardew_AsharaOutside_Window1";
        private const string Window2Id = "MagicStardew_AsharaOutside_Window2";

        private bool LampOn = false; // <--- NEW

        public void Initialize(IModHelper helper, IMonitor monitor)
        {
            Helper = helper;
            Monitor = monitor;

            helper.Events.Player.Warped += OnWarped;
            helper.Events.GameLoop.TimeChanged += OnTimeChanged;
        }

        private void OnWarped(object? sender, WarpedEventArgs e)
        {
            if (!e.IsLocalPlayer) return;
            if (e.NewLocation?.NameOrUniqueName != LocationName) return;

            UpdateLampState(Game1.timeOfDay);
            UpdateWindowLights(Game1.timeOfDay);
        }

        private void OnTimeChanged(object? sender, TimeChangedEventArgs e)
        {
            UpdateLampState(e.NewTime);
            UpdateWindowLights(e.NewTime);
        }

        // ----------------- LAMPA -----------------
        private void UpdateLampState(int time)
        {
            int lightOnTime = GetLightOnTimeForSeason(Game1.currentSeason);
            bool shouldBeOn = time >= lightOnTime;

            if (shouldBeOn && !LampOn)
            {
                LampOn = true;
                AddLight(LampLightId, LampTilePos, LampRadius, LampColor, -8f, 0f);
                SetLampTiles(true); // <--- NEW
                Monitor.Log($"[AsharaOutside] Lampa WŁĄCZONA o {time}", LogLevel.Debug);
            }
            else if (!shouldBeOn && LampOn)
            {
                LampOn = false;
                RemoveLight(LampLightId);
                SetLampTiles(false); // <--- NEW
                Monitor.Log($"[AsharaOutside] Lampa WYŁĄCZONA o {time}", LogLevel.Debug);
            }
        }

        private int GetLightOnTimeForSeason(string season)
        {
            return season switch
            {
                "winter" => 1730,
                "spring" => 1830,
                "summer" => 1930,
                "fall" => 1800,
                _ => 1830
            };
        }

        // ----------------- OKNA -----------------
        private void UpdateWindowLights(int time)
        {
            bool windowsOn = time >= 800 && time <= 2430;

            if (windowsOn)
            {
                AddLight(Window1Id, Window1TilePos, WindowRadius, WindowColor, -3f, -16f);
                AddLight(Window2Id, Window2TilePos, WindowRadius, WindowColor, -4f, +8f);
            }
            else
            {
                RemoveLight(Window1Id);
                RemoveLight(Window2Id);
            }
        }

        // ----------------- FUNKCJE MAPY -----------------
        private void SetLampTiles(bool on)
        {
            var loc = Game1.getLocationFromName(LocationName);
            if (loc == null) return;

            var layer = loc.Map.GetLayer("Front");
            var baseLayer = loc.Map.GetLayer("Buildings");
            if (layer == null || baseLayer == null) return;

            // Spróbuj pobrać tilesheet użyty na warstwie Buildings przy lampie
            Tile? refTile = baseLayer.Tiles[12, 13] ?? baseLayer.Tiles[11, 13] ?? baseLayer.Tiles[12, 12];
            if (refTile == null)
            {
                Monitor.Log("[AsharaOutside] Nie znaleziono tile’a referencyjnego na warstwie Buildings!", LogLevel.Warn);
                return;
            }

            var tileSheet = refTile.TileSheet;
            if (tileSheet == null)
            {
                Monitor.Log("[AsharaOutside] Nie udało się pobrać tilesheetu z referencyjnego tile’a!", LogLevel.Warn);
                return;
            }

            if (on)
            {
                // cztery tile (2x2)
                layer.Tiles[11, 12] = new StaticTile(layer, tileSheet, BlendMode.Alpha, 31);
                layer.Tiles[12, 12] = new StaticTile(layer, tileSheet, BlendMode.Alpha, 32);
                layer.Tiles[11, 13] = new StaticTile(layer, tileSheet, BlendMode.Alpha, 64);
                layer.Tiles[12, 13] = new StaticTile(layer, tileSheet, BlendMode.Alpha, 65);

                Monitor.Log("[AsharaOutside] Ustawiono tile’e lampy (lamp ON)", LogLevel.Debug);
            }
            else
            {
                layer.Tiles[11, 12] = null;
                layer.Tiles[12, 12] = null;
                layer.Tiles[11, 13] = null;
                layer.Tiles[12, 13] = null;

                Monitor.Log("[AsharaOutside] Usunięto tile’e lampy (lamp OFF)", LogLevel.Debug);
            }
        }


        // ----------------- FUNKCJE ŚWIATEŁ -----------------
        private void AddLight(string id, Vector2 tilePos, float radius, Color color, float offsetX = 0, float offsetY = 0)
        {
            var loc = Game1.getLocationFromName(LocationName);
            if (loc == null || Game1.currentLocation?.NameOrUniqueName != LocationName)
                return;

            var pixelPos = new Vector2(
                (tilePos.X * 64f) + 32f + offsetX,
                (tilePos.Y * 64f) + 32f + offsetY
            );

            var light = new LightSource(
                id: id,
                textureIndex: LightSource.lantern,
                position: pixelPos,
                radius: radius,
                color: color,
                lightContext: LightSource.LightContext.None,
                playerID: Game1.player.UniqueMultiplayerID
            );

            try { Game1.currentLightSources[id] = light; }
            catch
            {
                RemoveLight(id);
                try { Game1.currentLightSources.Add(id, light); }
                catch { }
            }
        }

        private void RemoveLight(string id)
        {
            if (Game1.currentLightSources.ContainsKey(id))
                Game1.currentLightSources.Remove(id);
        }
    }
}
