using AMI.Neitsillia.Combat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AMI.Neitsillia.Items.Abilities.Effects
{
    static partial class AbilityEffects
    {
        public static void CounterPrep(CombatResult caster, CombatResult target)
        {
            if (caster.SentHit)
            {
                Ability counterPrep = caster.abilityUsed;
                target.character.Status(counterPrep.statusEffect ?? "Patient Requite", counterPrep.level,
                    counterPrep.level);
                caster.perkProcs.Add($"Applied Patient Requite to {target.Name}");
            }
        }

        public static void KeenEye(CombatResult caster, CombatResult target)
        {
            if (caster.SentHit)
            {
                Ability keeneye = caster.abilityUsed;
                target.character.Status(keeneye.statusEffect, 5 + (keeneye.level / 10),
                    5 + (keeneye.level / 10));
                caster.perkProcs.Add($"Applied {keeneye.statusEffect} to {target.Name}");
            }
        }

        public static void Reflect(CombatResult caster, CombatResult target)
        {
            if (caster.SentHit)
            {
                Ability reflect = caster.abilityUsed;
                target.character.Status(reflect.statusEffect, 
                   3 + (reflect.level / 5), reflect.level);
                caster.perkProcs.Add($"Applied {reflect.statusEffect} to {target.Name}");
            }
        }
    }
}
