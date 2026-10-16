// Consolidated from: AsharaFireflyManager.cs
// Original class implementations preserved.
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using StardewModdingAPI.Events;
using StardewModdingAPI;
using StardewValley.Characters;
using StardewValley.Locations;
using StardewValley;
using System.Linq;
using System;

// ===== Original file: AsharaFireflyManager.cs =====
namespace MagicStardew
{
    public class AsharaFireflyManager
    {
        private readonly IModHelper helper;
        private readonly IMonitor monitor;
        private Texture2D fireflyTexture;

        private FireflyInstance? asharaFirefly;
        private NPC? asharaNPC;

        public AsharaFireflyManager(IModHelper helper, IMonitor monitor)
        {
            this.helper = helper;
            this.monitor = monitor;
            this.fireflyTexture = helper.ModContent.Load<Texture2D>("assets/firefly.png");

            helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
            helper.Events.GameLoop.DayEnding += OnDayEnding;
        }

        private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
        {
            if (!Context.IsWorldReady || !e.IsMultipleOf(30)) // co pół sekundy
                return;

            asharaNPC ??= Game1.getCharacterFromName("Ashara", mustBeVillager: false);

            if (asharaNPC == null)
            {
                monitor.Log("[AsharaFirefly] NPC 'Ashara' nie istnieje w świecie.", LogLevel.Trace);
                CleanupFirefly();
                return;
            }

            GameLocation loc = asharaNPC.currentLocation;
            if (loc == null)
            {
                monitor.Log("[AsharaFirefly] Ashara nie ma przypisanej lokacji.", LogLevel.Trace);
                CleanupFirefly();
                return;
            }

            // 🔹 LOG diagnostyczny
            monitor.Log($"[AsharaFirefly] Lokacja Ashary: {loc.NameOrUniqueName}, Gracz: {Game1.currentLocation.NameOrUniqueName}, Czas: {Game1.timeOfDay}", LogLevel.Trace);

            if (!IsOutdoorLike(loc))

            {
                monitor.Log("[AsharaFirefly] Lokacja nie jest outdoorowa — pomijam.", LogLevel.Trace);
                CleanupFirefly();
                return;
            }

            if (IsAsharaHome(loc.NameOrUniqueName))
            {
                monitor.Log("[AsharaFirefly] Ashara jest w domu — pomijam.", LogLevel.Trace);
                CleanupFirefly();
                return;
            }

            if (Game1.currentLocation != loc)
            {
                monitor.Log("[AsharaFirefly] Gracz nie jest w tej samej lokacji co Ashara.", LogLevel.Trace);
                CleanupFirefly();
                return;
            }

            int currentTime = Game1.timeOfDay;
            int requiredTime = GetRequiredTimeForSeason(Game1.currentSeason);

            if (currentTime < requiredTime)
            {
                monitor.Log($"[AsharaFirefly] Za wcześnie ({currentTime} < {requiredTime}) — jeszcze nie pora.", LogLevel.Trace);
                CleanupFirefly();
                return;
            }

            if (asharaFirefly == null)
            {
                asharaFirefly = new FireflyInstance(fireflyTexture, asharaNPC);
                monitor.Log("[AsharaFirefly] ✨ Firefly pojawił się przy Asharze!", LogLevel.Info);
            }

            asharaFirefly.Update();
        }
        private bool IsOutdoorLike(GameLocation loc)
        {
            // jeśli gra uznaje lokację za outdoor → OK
            if (loc.IsOutdoors)
                return true;

            // jeśli to nasza niestandardowa lokacja, którą traktujemy jako outdoor
            if (loc.NameOrUniqueName.Contains("MagicStardew_AsharaHouse_Outside", StringComparison.OrdinalIgnoreCase))
                return true;

            return false;
        }

        private void OnDayEnding(object? sender, DayEndingEventArgs e)
        {
            CleanupFirefly();
        }

        private void CleanupFirefly()
        {
            if (asharaFirefly != null)
            {
                asharaFirefly.Cleanup();
                asharaFirefly = null;
                monitor.Log("[AsharaFirefly] 🔹 Firefly usunięty.", LogLevel.Trace);
            }
        }

        private bool IsAsharaHome(string locationName)
        {
            // TODO: zmień na dokładną nazwę jej domu jeśli masz
            return locationName.Contains("AsharaHouse", StringComparison.OrdinalIgnoreCase);
        }

        private int GetRequiredTimeForSeason(string season) => season switch
        {
            "winter" => 1730,
            "spring" => 1830,
            "summer" => 1930,
            "fall" => 1800,
            _ => 1830
        };

        // 🔸 Minimalna wersja klasy firefly
        private class FireflyInstance
        {
            private readonly NPC target;
            private readonly Texture2D texture;
            private readonly TemporaryAnimatedSprite sprite;
            private readonly LightSource light;

            private Vector2 lastNpcPos;
            private Vector2 smoothNpcPos;

            private readonly Vector2 OrbitOffset = new(40, -60);
            private const float BobbingY = 5f;
            private const float BobbingX = 8f;
            private const double BobbingIntervalY = 250.0;
            private const double BobbingIntervalX = 500.0;

            public FireflyInstance(Texture2D texture, NPC target)
            {
                this.texture = texture;
                this.target = target;

                lastNpcPos = target.Position;
                smoothNpcPos = target.Position;

                sprite = new TemporaryAnimatedSprite(
                    "",
                    new Rectangle(0, 0, 10, 10),
                    150f,
                    4,
                    9999,
                    target.Position,
                    false,
                    false
                )
                {
                    texture = texture,
                    scale = 2f,
                    layerDepth = (target.getStandingPosition().Y + 16f) / 10000f
                };

                target.currentLocation?.temporarySprites.Add(sprite);

                light = new LightSource(
                    id: $"firefly_ashara",
                    textureIndex: 1,
                    position: target.Position + new Vector2(10f, 64f),
                    radius: 5f,
                    color: new Color(0, 60, 180),
                    lightContext: LightSource.LightContext.None
                );

                Game1.currentLightSources[light.Id] = light;
            }

            public void Update()
            {
                if (target.currentLocation == null)
                    return;

                // 🔹 Interpolacja pozycji NPC
                Vector2 npcPos = target.Position;
                smoothNpcPos = Vector2.Lerp(smoothNpcPos, npcPos, 0.25f);
                lastNpcPos = npcPos;

                // 🔹 Obliczanie docelowej pozycji firefly
                Vector2 followTarget = smoothNpcPos + OrbitOffset;

                // 🔹 Falowanie (jak w spellu)
                float bobY = (float)Math.Sin(Game1.currentGameTime.TotalGameTime.TotalMilliseconds / BobbingIntervalY) * BobbingY;
                float bobX = (float)Math.Sin(Game1.currentGameTime.TotalGameTime.TotalMilliseconds / BobbingIntervalX) * BobbingX;
                Vector2 dynamicTarget = followTarget + new Vector2(bobX, bobY);

                // 🔹 Płynne podążanie
                sprite.position = Vector2.Lerp(sprite.position, dynamicTarget, 0.15f);

                // 🔹 Aktualizacja światła
                light.position.Value = sprite.position + new Vector2(5, 5);

                // 🔹 Dopasowanie warstwy
                sprite.layerDepth = (target.getStandingPosition().Y + 16f) / 10000f;
            }

            public void Cleanup()
            {
                try
                {
                    if (target.currentLocation?.temporarySprites.Contains(sprite) == true)
                        target.currentLocation.temporarySprites.Remove(sprite);
                    if (Game1.currentLightSources.ContainsKey(light.Id))
                        Game1.currentLightSources.Remove(light.Id);
                }
                catch { }
            }
        }

    }
}
