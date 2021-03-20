using AMI.Methods;
using AMI.Neitsillia.Items.ItemPartials;
using System;
using System.Collections.Generic;

namespace AMI.Neitsillia.Items
{
    class StatsWeights
    {
        internal Dictionary<string, int> weights;

        internal int damage => Get("damage");
        internal int resistance => Get("resistance");
        internal int health => Get("healthBuff");
        internal int stamina => Get("stamina");
        internal int perk => Get("perk");
        internal int other => Get("other");

        internal double GetScaler(int desiredTier, int currentTier)
            => (desiredTier + 0.00) / currentTier;

        public StatsWeights()
        {
            weights = new Dictionary<string, int>()
            {
                {"damage", 15},
                {"resistance", 15},
                {"healthBuff", 10},
                {"stamina", 5},
                {"perk", 200},
            };
        }

        internal int Get(string key)
        {
            if (weights.TryGetValue(key, out int v))
                return v;
            return 1;
        }

        internal Item ScaleWeapon(Item item, int target)
        {
            double m = (double)target / item.tier;
            double n = m - 1;
            long l = 0;

            #region Excluded stats
            foreach (int r in item.resistance)
                l += r != 0 ? NumbersM.CeilParse<int>(n * r * resistance) : 0;

            try
            {
                l += NumbersM.CeilParse<long>(item.healthBuff * health * n)
                    +
                     NumbersM.CeilParse<long>(item.staminaBuff * stamina * n)
                    +
                     NumbersM.CeilParse<long>( 
                         (
                                item.durability + item.agility
                                + item.critChance
                                + item.critMult
                                + (item.perk != null ? (item.perk.tier + 1) * perk : 0)
                         ) * n
                    );
            }
            catch (Exception e) { Console.WriteLine("Error in other math"); throw e; }
            #endregion

            long i = 0;
            foreach(var d in item.damage)
            {
                i += d > 0 ? damage : 0;
            }

            for (int k = 0; k < item.damage.Length; k++)
            {
                if (item.damage[k] > 0)
                    item.damage[k] = NumbersM.CeilParse<long>(
                        (item.damage[k] * m) +
                        ((damage * l / i) / damage));
            }

            return item;
        }

        internal Item ScaleArmor(Item item, int target)
        {
            double m = ((double)target) / item.tier;
            double n = m - 1;

            #region Excluded stats
            long l = 0;
            foreach (int r in item.damage)
                l += r > 0 ? NumbersM.CeilParse<long>(n * r * damage) : 0;
            l += NumbersM.CeilParse<long>(
                + (item.durability + item.agility
                + Convert.ToInt64(item.critChance)
                + Convert.ToInt64(item.critMult)
                + (item.perk != null ? (item.perk.tier + 1) * perk : 0)
                ) * n);

            #endregion

            #region Included Stats
            long i = 
                (item.healthBuff != 0 ? health : 0)
                + ( item.staminaBuff != 0 ? stamina : 0);
            foreach (var d in item.resistance)
            {
                i += d != 0 ? resistance : 0;
            }
            #endregion

            //Update stats
            item.healthBuff = item.healthBuff != 0 ?
                item.healthBuff = NumbersM.CeilParse<long>(
                        (item.healthBuff * m) +
                        ((health * (l + 0.00) / i) / health)) : 0;

            item.staminaBuff = item.staminaBuff != 0 ?
                item.staminaBuff = NumbersM.CeilParse<int>(
                        (item.staminaBuff * m) +
                        ((stamina * (l+0.00) / i) / stamina)) : 0;

            for (int k = 0; k < item.resistance.Length; k++)
            {
                if(item.resistance[k] != 0)
                    item.resistance[k] = NumbersM.FloorParse<int>(
                        (item.resistance[k] * m) +
                        ((resistance * (l+0.00) / i) / resistance));
            }

            return item;
        }
    }
}
