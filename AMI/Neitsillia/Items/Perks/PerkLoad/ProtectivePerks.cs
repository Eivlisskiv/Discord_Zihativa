namespace AMI.Neitsillia.Items.Perks.PerkLoad
{
    public partial class PerkLoad
    {
        public static Perk ReflectiveWard(string name, int tier, int rank) 
            => new Perk(name, tier, rank)
            {
                trigger = Perk.Trigger.Defense,
                desc = $"Has a {tier}% chance to reflect {5 + tier}% damage ignoring resistances.",
            };
    }
}
