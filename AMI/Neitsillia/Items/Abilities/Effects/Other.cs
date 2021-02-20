using AMI.Neitsillia.Combat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AMI.Neitsillia.Items.Abilities.Effects
{
    static partial class AbilityEffects
    {
        public static void Kill(CombatResult caster, CombatResult target)
        {
            target.character.health = 0;
        }
    }
}
