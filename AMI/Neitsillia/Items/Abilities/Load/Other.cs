using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AMI.Neitsillia.Items.Abilities.Load
{
    public static partial class LoadAbility
    {
        public static Ability Kill(string aname, int alevel = -1)
        {
            Ability a = new Ability(false)
            {
                name = aname,
                type = Ability.AType.Tactical,

                tier = 0,
                description = "Insta kill.",
            };
            return a;
        }
    }
}
