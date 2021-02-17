using AMI.Methods;
using System;

namespace AMI.AMIData
{
    public class LootTables<T>
    {
        private readonly T[][] table;
        private readonly Random random;

        public LootTables(T[][] tab, Random rng)
        {
            table = tab;
            random = rng;
        }

        public T GetItem(out (int t1, int t2) ts)
        {
            int t1 = ArrayM.IndexWithRates(table.Length - 1, random);
            ts = (t1, ArrayM.IndexWithRates(table[t1].Length - 1, random));
            return table[t1][ts.t2];
        }

        public void GetItems(double floorPercentage, int perceptionBonus, Action<T, int, int> action)
        {
            int amount = NumbersM.FloorParse<int>(random.Next(2, Math.Max(3, perceptionBonus)) * (floorPercentage + 1));
            for (int i = 0; i < amount; i++)
            {
                T item = GetItem(out (int t1, int t2) ts);
                action(item, ts.t1, ts.t2);
            }
        }
    }
}
