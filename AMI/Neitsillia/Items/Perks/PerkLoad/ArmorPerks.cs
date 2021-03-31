namespace AMI.Neitsillia.Items.Perks.PerkLoad
{
    public partial class PerkLoad
    {
        public static Perk SpikedArmor(string name) => new Perk(name)
        {
            trigger = Perk.Trigger.BeforeDefense,
            desc = "Return 1% of physical damage received for every endurance point. [Max 25%]",
        };
        public static Perk TakeOnMe(string name) => new Perk(name)
        {
            trigger = Perk.Trigger.Defense,
            desc = "Increases the enemy hit chance by 15% and has a 10% chance to restore 10% of your maximum health when hit.",
        };
        public static Perk ToxinFilter(string name) => new Perk(name)
        {
            trigger = Perk.Trigger.Defense,
            maxRank = 5,
            desc = "Has 10% chance to restore stamina when receiving toxic damage.",
        };
        public static Perk LoneWolf(string name) => new Perk(name)
        {
            trigger = Perk.Trigger.BeforeDefense,
            desc = "+20 all resistance per enemy outnumbering you.",
        };
        public static Perk Opportunist(string name) => new Perk(name)
        {
            //Opportunist
            trigger = Perk.Trigger.Offense,
            desc = "+1% damage for each status effect on target.",
        };
        public static Perk CleanSlate(string name) => new Perk(name)
        {
            trigger = Perk.Trigger.Turn,
            desc = "Recover +5% stamina per turn while no status effects are applied."
        };
        public static Perk Underestimated(string name) => new Perk(name)
        {
            //Underestimated 
            trigger = Perk.Trigger.BeforeOffense,
            desc = "Deal 10% damage to higher level or ranked enemies."
        };
        public static Perk AwaitedStorm(string name) => new Perk(name)
        {
            trigger = Perk.Trigger.Offense,
            desc = "Increase damage on the next attack by 1% per Strength points while not using Martial, Elemental or Enchantment abilities. Resets after using such ability.",
        };
        public static Perk FastLearner(string name) => new Perk(name)
        {
            trigger = Perk.Trigger.GainXP,
            desc = "+15% XP gain",
        };

        //tier 1
        public static Perk Lightweight(string name) => new Perk(name)
        {
            trigger = Perk.Trigger.Defense,
            desc = "+15% Martial and Enchanted attacks dodge chance",
        };
        public static Perk MagicDescry(string name) => new Perk(name)
        {
            trigger = Perk.Trigger.Defense,
            desc = "+10% Elemental and Tactical attacks dodge chance",
        };

        public static Perk LovingHands(string name) => new Perk(name)
        {
            trigger = Perk.Trigger.Offense,
            desc = "Defensive abilities have a chance to resurrect and drain 100% more stamina."
        };
        //
        public static Perk InfectionCarrier(string name) => new Perk(name)
        {
            trigger = Perk.Trigger.Offense,
            end = Perk.Trigger.Defense,
            desc = "Contact with enemies have a chance" +
            " to infect them with the Flesh Curse.",
        };
    }
}
