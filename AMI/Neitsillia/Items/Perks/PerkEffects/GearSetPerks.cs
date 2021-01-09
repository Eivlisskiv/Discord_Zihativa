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
                int set = GearSets.GetSetPower("Hunter Trap", owner.character.equipment);
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
    }
}
