using AMI.Methods;
using AMI.Neitsillia.Combat;
using AMI.Neitsillia.Items.Perks.PerkLoad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AMI.Neitsillia.Items
{
    public partial class Perk
    {
        public object[] GiftOfOdez(CombatResult owner)
        {
            if (rank <= 0)
            {
                (string effect, int duration, int intentity) = Utils.RandomElement(
                    ("Merciless", 5, 20), ("Focused", 5, 66), ("Vigorous", 5, 4),
                    ("Warded", 1, 50), ("Recovering", 5, 1));

                owner.character.Status(effect, duration, intentity);
                owner.perkProcs.Add($"Gained {effect}");

                rank = 10;
                return R(owner);
            }
            return TurnPassed(owner);
        }
    }
}
