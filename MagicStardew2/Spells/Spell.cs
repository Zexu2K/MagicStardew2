using Microsoft.Xna.Framework;
using StardewValley;
using System;

namespace MagicStardew.Spells
{
    public class Spell
    {
        public string Name { get; }
        public Rectangle IconSource { get; }
        public Func<Farmer, int> ManaCostFunc { get; }
        public string Description { get; }
        public string UnlockHint { get; }
        public Predicate<Farmer> UnlockedCondition { get; }
        public Action<Farmer> Effect { get; }
        public Predicate<Farmer>? CanCast { get; }
        public Predicate<Farmer>? InUse { get; }
        public bool CanBeLeveledUp { get; set; } = true;

        private readonly SpellManager spellManager;

        public Spell(
            string name,
            Rectangle iconSource,
            Func<Farmer, int> manaCostFunc,
            string description,
            string unlockHint,
            Predicate<Farmer> unlocked,
            Action<Farmer> effect,
            SpellManager spellManager,
            Predicate<Farmer>? canCast = null,
            Predicate<Farmer>? inUse = null,
            bool canBeLeveledUp = true)
        {
            Name = name;
            IconSource = iconSource;
            ManaCostFunc = manaCostFunc;
            Description = description;
            UnlockHint = unlockHint;
            UnlockedCondition = unlocked;
            Effect = effect;
            this.spellManager = spellManager;
            CanCast = canCast;
            InUse = inUse;
            CanBeLeveledUp = canBeLeveledUp;
        }

        public bool IsUnlocked(Farmer who) => UnlockedCondition(who);
        public int GetManaCost(Farmer who) => ManaCostFunc(who);

        public int GetLevel(Farmer who)
        {
            int uses = GetUseCount(who);
            return Math.Min(4, 1 + (uses / 10));
        }

        public int GetUseCount(Farmer who)
        {
            string key = $"MagicStardew.UseCount.{Name}";
            return who.modData.TryGetValue(key, out string val) && int.TryParse(val, out int count) ? count : 0;
        }

        public void IncrementUseCount(Farmer who)
        {
            int current = GetUseCount(who);
            string key = $"MagicStardew.UseCount.{Name}";
            who.modData[key] = (current + 1).ToString();
        }
    }
}