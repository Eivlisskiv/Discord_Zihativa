using AMI.Methods.Graphs;
using AMI.Neitsillia.Collections;
using AMI.Neitsillia.NPCSystems;
using AMYPrototype;
using Neitsillia.Items.Item;
using System;
using System.Collections.Generic;

namespace AMI.Neitsillia.Items
{
    class IMethods
    {
        public static Item Condition(Item item, int endurance)
        {
            if(Program.Chance(Math.Max(Exponential.Durability(item.durability) - (endurance * Stats.DurabilityPerEnd), 8)))
                item.condition--;
            return item;
        }
        public static CharacterMotherClass AllArmorCND(CharacterMotherClass user)
        {
            for(int i = 2; i <= Equipment.gearCount; i++)
            {
                Item gear = user.equipment.GetGear(i);
                if (gear != null && (i > 7 || i < 5))
                    Condition(gear, user.stats.endurance);
            }
            return user;
        }

        public static int[] GetUpgrades(float[] stats, bool isWeapon)
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
