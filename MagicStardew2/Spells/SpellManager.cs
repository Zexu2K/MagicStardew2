using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;

namespace MagicStardew.Spells
{
    public class SpellManager
    {
        public const string SpellBookItemId = "MagicStardew.SpellBook";

        private readonly Dictionary<string, Spell> registeredSpells = new(StringComparer.OrdinalIgnoreCase);
        private readonly IMonitor monitor;
        public ManaBar ManaBar { get; }

        public static bool SpellJustCast { get; set; } = false;

        public SpellManager(ManaBar manaBar, IModHelper helper, IMonitor monitor, ModConfig config)
        {
            this.ManaBar = manaBar;
            this.monitor = monitor;
            this.ManaBar.SetSpellManager(this);
        }

        public void RegisterSpell(Spell spell)
        {
            if (!registeredSpells.ContainsKey(spell.Name))
                registeredSpells[spell.Name] = spell;
        }

        public Spell? GetSpellByName(string name) => registeredSpells.TryGetValue(name, out var spell) ? spell : null;

        public IEnumerable<Spell> GetAllSpells() => registeredSpells.Values;

        public bool HasSpellBookEquipped(Farmer who)
        {
            if (who == null) return false;

            bool wearingRing = (who.leftRing.Value?.ItemId == SpellBookItemId)
                            || (who.rightRing.Value?.ItemId == SpellBookItemId);
            bool holdingItem = who.CurrentItem?.ItemId == SpellBookItemId;

            return wearingRing || holdingItem;
        }

        public void Cast(Farmer caster, string spellName)
        {
            var spell = GetSpellByName(spellName);
            if (spell == null) return;

            if (spell.CanCast != null && !spell.CanCast(caster)) return;

            int cost = spell.GetManaCost(caster);
            if (ManaBar.GetCurrentMana(caster) < cost)
            {
                Game1.playSound("cancel");
                Game1.showRedMessage("Not enough mana.");
                return;
            }

            ManaBar.UseMana(caster, cost);
            spell.Effect(caster);
            spell.IncrementUseCount(caster);

            SpellJustCast = true;
        }

        public void UnlockSpell(string spellName, string key)
        {
            Game1.player.modData[key] = "true";
            Game1.addHUDMessage(new HUDMessage($"Unlocked spell: {spellName}!", HUDMessage.newQuest_type));
        }
    }
}