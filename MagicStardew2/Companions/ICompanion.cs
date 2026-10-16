using Microsoft.Xna.Framework;
using StardewValley;

namespace MagicStardew.Companions
{
    public interface ICompanion
    {
        long OwnerId { get; }
        void Update(GameTime time, GameLocation location);
        void OnWarp(GameLocation newLocation);
        void Cleanup();
    }
}