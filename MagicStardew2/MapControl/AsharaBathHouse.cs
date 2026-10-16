// Consolidated from: AsharaBathHouse.cs
// Original class implementations preserved.
using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley.Locations;
using StardewValley;
using System.Reflection;

// ===== Original file: AsharaBathHouse.cs =====
namespace AsharaSwimmingEnhance
{
    internal class AsharaBathHouse
    {
        private static Texture2D swimShadow;
        private static int swimShadowFrame;
        private static int swimShadowTimer;

        private static bool IsAsharaInSwimArea(NPC npc)
        {
            if (npc.Name != "Ashara")
                return false;

            if (Game1.currentLocation?.Name != "BathHouse_Pool")
                return false;

            int x = (int)(npc.Position.X / 64f);
            int y = (int)(npc.Position.Y / 64f);

            // Obszar 1
            if (x >= 4 && x <= 7 && y >= 9 && y <= 10)
                return true;

            // Obszar 2
            if (x >= 19 && x <= 23 && y >= 8 && y <= 11)
                return true;

            // Obszar 3
            if (x >= 4 && x <= 23 && y >= 11 && y <= 19)
                return true;

            return false;
        }

        public static void Init(Mod mod)
        {
            var harmony = new Harmony(mod.ModManifest.UniqueID);

            harmony.Patch(
                AccessTools.Method(typeof(GameLocation), nameof(GameLocation.UpdateWhenCurrentLocation)),
                postfix: new HarmonyMethod(typeof(AsharaBathHouse), nameof(UpdateSwimming))
            );

            MethodInfo drawMethod = AccessTools.Method(typeof(Character), "draw", new[] { typeof(SpriteBatch) });
            if (drawMethod == null)
            {
                mod.Monitor.Log("❌ Nie znaleziono Character.draw(SpriteBatch)", LogLevel.Error);
                return;
            }

            mod.Monitor.Log($"✅ Patchujemy {drawMethod}", LogLevel.Debug);

            harmony.Patch(
                drawMethod,
                prefix: new HarmonyMethod(typeof(AsharaBathHouse), nameof(DrawSwimmingAshara))
            );
        }

        public static void UpdateSwimming(GameLocation __instance)
        {
            if (__instance is not BathHousePool)
                return;

            if (swimShadow == null)
                swimShadow = Game1.temporaryContent.Load<Texture2D>("LooseSprites\\swimShadow");

            swimShadowTimer -= Game1.currentGameTime.ElapsedGameTime.Milliseconds;
            if (swimShadowTimer <= 0)
            {
                swimShadowTimer = 70;
                swimShadowFrame++;
                swimShadowFrame %= 10;
            }
        }

        // 👇 UWAGA: parametr musi się nazywać "b", nie "spriteBatch"
        public static bool DrawSwimmingAshara(Character __instance, SpriteBatch b)
        {
            if (__instance is not NPC npc || npc.Name != "Ashara")
                return true;

            if (!IsAsharaInSwimArea(npc))
            {
                npc.HideShadow = false; // poza wodą - przywróć cień
                return true;
            }

            npc.HideShadow = true; // wyłącz cień, jeśli pływa


            if (!IsAsharaInSwimArea(npc))
                return true;

            Rectangle source = npc.Sprite.SourceRect;
            source.Height -= 12;

            Vector2 position = Game1.GlobalToLocal(Game1.viewport, npc.Position);
            float alpha = 1f;
            float scale = 4f;

            // Górna część ciała
            b.Draw(
                npc.Sprite.Texture,
                Game1.GlobalToLocal(Game1.viewport, npc.Position + new Vector2(32f, 0f)),
                source,
                Color.White * alpha,
                0f,
                new Vector2(source.Width / 2, source.Height),
                scale,
                npc.flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
                Math.Max(0f, npc.StandingPixel.Y / 10000f)
            );


            // Cień na wodzie – przesunięty 32px w prawo i 32px w dół
            Vector2 shadowPos = npc.Position + new Vector2(32f, 32f);

            b.Draw(
                swimShadow,
                Game1.GlobalToLocal(Game1.viewport, shadowPos),
                new Rectangle(swimShadowFrame * 16, 0, 16, 16),
                Color.White * 0.9f,
                0f,
                new Vector2(8, 8), // środek sprite'a cienia
                scale,
                SpriteEffects.None,
                (npc.StandingPixel.Y - 1) / 10000f // trochę pod postacią
            );



            return false; // zablokuj domyślne rysowanie
        }
    }
}
