using AMI.Methods;
using AMI.Neitsillia.Combat;

namespace AMI.Neitsillia.Items
{
    partial class Perk
    {
        public object[] HunterTrap(CombatResult owner, CombatResult target)
        {
            if (rank >= 5 && owner.SentHit && 
                owner.abilityUsed.type == Ability.AType.Tactical)
            {
                int set = GearSets.GetSetPower("Trapper", owner.character.equipment);
                target.character.Status("Ensnared", 3 + set, set * 5);
                target.perkProcs.Add($"Ensnared in {owner.Name}'s trap");
                rank = 0;
            }

            return new object[] { owner, target };
        }

        public object[] HunterTrap_End(CombatResult owner)
        {
            if (rank < 5) rank++;

            return new object[] { owner };
        }

        public object[] GladiatorPatientia(CombatResult owner)
        {
            int set = GearSets.GetSetPower("Gladiator", owner.character.equipment);
            double buff = (set * 0.2) * rank;
            int max = 10 + (3 * set);
            if (buff >= max)
            {
                buff += (18 * set);
                rank = 0;
            }
            else
            {
                rank++;
            }

            owner.perkProcs.Add($"Gladiator Patientia: +{buff}% PHY DMG");

            owner.damageMultiplier = ArrayM.AddEach(owner.damageMultiplier, buff / 100.00);

            return R(owner);
        }
    }
}
