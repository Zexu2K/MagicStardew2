using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace MagicStardew.Companions
{
    public class CompanionManager
    {
        private readonly Dictionary<long, List<ICompanion>> activeCompanions = new();

        public CompanionManager(IModHelper helper)
        {
            helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
            helper.Events.Player.Warped += OnWarped;
            helper.Events.GameLoop.DayEnding += OnDayEnding;
        }

        public void AddCompanion(long ownerId, ICompanion companion)
        {
            if (!activeCompanions.ContainsKey(ownerId))
                activeCompanions[ownerId] = new List<ICompanion>();

            activeCompanions[ownerId].Add(companion);
        }

        public void RemoveCompanionsOfType<T>(long ownerId) where T : ICompanion
        {
            if (!activeCompanions.TryGetValue(ownerId, out var list)) return;

            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (list[i] is T companion)
                {
                    companion.Cleanup();
                    list.RemoveAt(i);
                }
            }
        }

        public bool HasCompanionOfType<T>(long ownerId) where T : ICompanion
        {
            return activeCompanions.TryGetValue(ownerId, out var list) && list.Exists(c => c is T);
        }

        private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
        {
            if (!Context.IsWorldReady) return;

            if (Game1.currentGameTime is not GameTime gameTime || Game1.currentLocation is not GameLocation currentLocation)
                return;

            foreach (var (ownerId, companions) in activeCompanions)
            {
                if (Game1.GetPlayer(ownerId) is not Farmer farmer || farmer.currentLocation != currentLocation) continue;

                foreach (var companion in companions)
                {
                    companion.Update(gameTime, currentLocation);
                }
            }
        }

        private void OnWarped(object? sender, WarpedEventArgs e)
        {
            if (activeCompanions.TryGetValue(e.Player.UniqueMultiplayerID, out var list))
            {
                foreach (var companion in list)
                {
                    companion.OnWarp(e.NewLocation);
                }
            }
        }

        private void OnDayEnding(object? sender, DayEndingEventArgs e)
        {
            foreach (var list in activeCompanions.Values)
            {
                foreach (var companion in list)
                {
                    companion.Cleanup();
                }
            }
            activeCompanions.Clear();
        }
    }
}