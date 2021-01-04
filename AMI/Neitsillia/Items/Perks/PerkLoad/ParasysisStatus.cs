namespace AMI.Neitsillia.Items.Perks.PerkLoad
{
    static partial class PerkLoad
    {
        public static Perk Paralysis(string name, int tier, int rank) 
        => new Perk(name, tier, rank)
        {
            trigger = Perk.Trigger.Turn,
            desc = $"{tier}% chance be paralyzed.",
        };

        public static Perk Ensnared(string name, int tier, int rank)
        => new Perk(name, tier, rank)
        {
            trigger = Perk.Trigger.Turn,
            desc = $"{tier}% chance be paralyzed with a small chance to escape ensnarement.",
        };
    }
}
