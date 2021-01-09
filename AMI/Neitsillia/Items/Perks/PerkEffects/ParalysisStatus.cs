using AMI.Neitsillia.Combat;
using AMYPrototype;

namespace AMI.Neitsillia.Items
{
    partial class Perk
    {
        public object[] Paralysis(CombatResult owner)
        {
            if (owner.action == CombatResult.Action.Cast && Program.Chance(tier))
                owner.action = CombatResult.Action.Paralyzed;

            return TurnPassed(owner);
        }

        public object[] Ensnared(CombatResult owner)
        {
            if (Program.Chance((50 + owner.character.stats.GetDEX()) - (rank + tier)))
                rank = 0;
            else if (owner.action == CombatResult.Action.Cast)
                owner.Paralyse("Ensnared");

            return TurnPassed(owner);
        }
    }
}
