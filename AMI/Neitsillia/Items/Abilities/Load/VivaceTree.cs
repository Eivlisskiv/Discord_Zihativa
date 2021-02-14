using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AMI.Neitsillia.Items.Abilities.Load
{
    static partial class LoadAbility
    {
        public static Ability Vivace(string aname, int alevel = -1)
        {
            Ability a = new Ability(aname)
            {
                name = aname,
                type = Ability.AType.Martial,
                //
                critChance = 0,
                critMult = 1,
                agility = 1,
                //
                staminaDrain = 20,
                //
                level = 1,
                maxLevel = 18,
                evolves = new[]
                {
                    "Spirit Rip",
                },
            };
            a.damage[0] = 1;
            Set(a, alevel);
            a.description = $"Drains {a.level * 3} stamina off the target.";
            return a;
        }
        // >>
        public static Ability SpiritRip(string aname, int alevel = -1)
        {
            Ability a = new Ability(aname)
            {
                name = aname,
                type = Ability.AType.Martial,
                //
                critChance = 0,
                critMult = 0,
                agility = 3,
                //
                staminaDrain = 30,
                //
                maxLevel = 12,
                tier = 1,
                statusEffect = "Energy Leak",
                evolves = new[]
                {
                    "Vivace",
                },
            };
            a.damage[0] = 2;
            Set(a, alevel);
            a.description = $"Has {20 + a.level}% chance to apply Energy Leak on target. " +
                $"Energy Leak: When hit, attacker has {35 + (a.level * 2)}% chance to drain {a.level * 5} stamina";
            
            return a;
        }
    }
}
