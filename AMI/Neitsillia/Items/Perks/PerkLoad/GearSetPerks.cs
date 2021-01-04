using System;

namespace AMI.Neitsillia.Items.Perks.PerkLoad
{
    static partial class PerkLoad
    {
        public static Perk HunterTrap(string name) => new Perk(name)
        {
            trigger = Perk.Trigger.Offense,
            end = Perk.Trigger.Turn,
            rank = 0,
            maxRank = 5,
            desc = "Casting a tactical ability ensnares the target [5 Turn cooldown] " +
            Environment.NewLine + "Ensnare strength increases with each Bounty Hunter gear equipped.",
        };
    }
}
