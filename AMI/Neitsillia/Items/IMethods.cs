using AMI.Methods.Graphs;
using AMI.Neitsillia.Collections;
using AMI.Neitsillia.NPCSystems;
using AMYPrototype;
using AMI.Neitsillia.Items.ItemPartials;
using System;
using System.Collections.Generic;
using AMI.Neitsillia.User.PlayerPartials;

namespace AMI.Neitsillia.Items
{
    public static class IMethods
    {
        public static Item Condition(this Item item, int endurance)
        {
            if(Program.Chance(Math.Max(Exponential.Durability(item.durability) - (endurance * Stats.DurabilityPerEnd), 8)))
                item.condition--;
            return item;
        }

        public static List<string> AllArmorCND(CharacterMotherClass user)
        {
            List<string> results = new List<string>();
            for (int i = 2; i <= Equipment.gearCount; i++)
            {
                Item gear = user.equipment.GetGear(i);
                if (gear != null && (i > 7 || i < 5))
                {
                    Condition(gear, user.stats.endurance);
                    if (VerifyOnBreak(gear, user))
                        results.Add($"{gear.name} has broken");
                }
            }
            return results;
        }

        public static bool VerifyOnBreak(this Item item, CharacterMotherClass character)
        {
            if(item.condition < 1)
            {
                Item equipped = character.equipment.GetGear(item.type);
                if (equipped != item) return false;

                character.equipment.Unequip(item.type);

                character.inventory.Add(item, 1, -1);

                return true;
            }

            return false;
        }

        public static int[] GetUpgrades(float[] stats)
        {
            int negative = -1;
            int positive = 0;
            for (int i = 0; i < stats.Length; i++)
            {
                if ((negative == -1 && stats[i] < 0) || 
                    (negative > -1 && stats[i] < stats[negative]))
                    negative = i;
                else if (stats[i] > stats[positive])
                    positive = i;
            }
            return new int[] {negative, positive };
        }

        internal static int GetNewTier(Item item, List<float> stats, int[] mods, int amount)
        {
            if(item.baseTier == 0 && item.originalName != "Wooden Spear")
                item.baseTier = Item.LoadItem(item.originalName).tier;
            //
            long total = 0;
            //damage
            for (int i = 0; i < item.damage.Length; i++)
                total += item.damage[i] * 15;
            //resistance
            for (int i = 0; i < item.resistance.Length; i++)
                total += item.resistance[i] * 15;
            //stats
            total += (item.healthBuff * 10) + item.durability
            + item.agility + (item.staminaBuff * 5) + 
            Convert.ToInt64(item.critChance) + Convert.ToInt64(item.critMult);
            if (item.perk != null)
                total += (200 * item.perk.tier) + 200;
            foreach (int i in mods)
            {
                if (i == 0 || i > 4)
                    total += (long)(stats[i] * 10) * amount;
                else if (i > -1)
                    total += (long)stats[i] * amount;
            }
            //
            return Convert.ToInt32(Linear.ALinear(total, 0.07));
        }
    }
}
