using AMI.Neitsillia.Combat;

namespace AMI.Neitsillia.Items.Abilities
{
    static partial class AbilityEffect
    {
        public static void WhiteBite(CombatResult caster, CombatResult target)
        {
            if (caster.SentHit)
            {
                Ability ability = caster.abilityUsed;
                target.character.Status(ability.statusEffect ?? "Energy Leak", 3 + (ability.level / 3),
                    ability.level);
                caster.perkProcs.Add($"{caster.Name} applied Energy Leak to {target.Name}");
            }
        }

        public static void Taunt(CombatResult caster, CombatResult target)
        {
            if (caster.SentHit)
            {
                Ability ability = caster.abilityUsed;
                //Add agrro mechanics TODO

                caster.character.StaminaE(100);
                target.perkProcs.Add($"Was taunted by {caster.Name}!");
            }
        }
    }
}
