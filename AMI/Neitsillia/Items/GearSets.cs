using AMI.Methods;
using AMI.Neitsillia.Collections;
using AMYPrototype;
using Neitsillia.Items.Item;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AMI.Neitsillia.Items
{
    static class GearSets
    {
        static KeyValuePair<string, string[]>  WriteSet(string name, string chest, string pants, string helm = null, string mask = null, string boots = null)
        => new KeyValuePair<string, string[]>(name, new string[] { helm, mask, chest, pants, boots});


        static Dictionary<string, string[]> sets = new Dictionary<string, string[]>()
        {

        };

        public static string Drop(string name)
        {
            if (sets.ContainsKey(name))
            {
                List<int> others = new List<int>();

                if (sets[name][0] != null) others.Add(0);
                if (sets[name][1] != null) others.Add(1);
                if (sets[name][4] != null) others.Add(4);

                if (sets[name][2] != null && Program.Chance(1)) return sets[name][2];
                if (others.Count == 0 && sets[name][3] != null && Program.Chance(5)) return sets[name][3];

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
