using AMI.Methods;
using AMI.Neitsillia.Collections;
using AMYPrototype;
using System.Collections.Generic;

namespace AMI.Neitsillia.Items
{
    static class GearSets
    {
        static string[] SetContent(string chest, string pants, string helm = null, string mask = null, string boots = null)
            => new string[] { helm, mask, chest, pants, boots };

        static Dictionary<string, string[]> sets = new Dictionary<string, string[]>()
        {
            { "Trapper", SetContent("Trapper Tunic", "Trapper Leg Patches", "Trapper Cap", null, "Trapper Boots") },
            { "Gladiator", SetContent("Lorica", null, "Galea", null, "Gladiator Sandals") },
        };

        public static string Drop(string name)
        {
            if (sets.ContainsKey(name))
            {
                if (sets[name][2] != null && Program.Chance(2)) return sets[name][2];
                if (sets[name][3] != null && Program.Chance(8)) return sets[name][3];

                List<int> others = new List<int>();

                if (sets[name][0] != null) others.Add(0);
                if (sets[name][1] != null) others.Add(1);
                if (sets[name][4] != null) others.Add(4);

                return others.Count > 0 ? sets[name][Utils.RandomElement(others)] : null;
            }
            return null;
        }

        public static int GetSetPower(string name, Equipment gear)
        {
            int p = 0;
            if (sets.ContainsKey(name))
            {
                for(int i = 0; i < sets[name].Length; i++)
                {
                    //  not null so null gear won't count               +2 to skip weapons slots
                    if (sets[name][i] != null && sets[name][i] == gear.GetGear(i + 2)?.originalName) p++;
                }
            }

            return p;
        }
    }
}
