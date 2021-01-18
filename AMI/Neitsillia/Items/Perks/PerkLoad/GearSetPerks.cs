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

        public static Perk GladiatorPatientia(string name) => new Perk(name)
        {
            trigger = Perk.Trigger.Turn,
            rank = 0,
            maxRank = 5,
            desc = "+0.2% [per set piece] physical damage per turn, " +
            "stacks up to 10% [+3% per set piece]." +
            Environment.NewLine + 
            "Additional +18% [per set piece] on the next turn after reaching full stack.",
        };
    }
}
