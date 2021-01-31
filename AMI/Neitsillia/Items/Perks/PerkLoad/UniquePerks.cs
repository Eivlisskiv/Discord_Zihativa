using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AMI.Neitsillia.Items.Perks.PerkLoad
{
    public partial class PerkLoad
    {
        public static Perk GiftOfOdez(string name) => new Perk(name)
        {
            trigger = Perk.Trigger.Turn,
            desc = "Gain a random buff every 10 turns lasting 5 turns." +
            " [ Merciless, Focused, Vigorous, Warded, Recovering ]",
        };
    }
}
