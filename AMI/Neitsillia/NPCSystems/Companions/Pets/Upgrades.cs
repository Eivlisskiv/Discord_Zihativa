using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AMI.Neitsillia.NPCSystems.Companions.Pets
{
    class PetUpgrades
    {
        internal static Dictionary<string, int[]> Costs =
            new Dictionary<string, int[]>()
            {
                {
                    "Default",
                    new int[]
                    {
                        8, 20,
                        15, 50,
                    }
                }
            };

        internal enum Upgrade
        {
            Health, Stamina,
            Damage, Resistance
        }
    }
}
