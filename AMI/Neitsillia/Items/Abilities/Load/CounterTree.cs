namespace AMI.Neitsillia.Items.Abilities.Load
{
    static partial class LoadAbility
    {
        public static Ability CounterPrep(string aname, int alevel = 0)
        {
            Ability a = new Ability(aname)
            {
                type = Ability.AType.Defensive,
                //
                agility = 70,
                //
                staminaDrain = 30,
                //
                level = 1,
                maxLevel = 10,
                statusEffect = "Patient Requite",
                evolves = new string[] { "Keen Eye", "Reflect" }
            };
            //
            Set(a, alevel);
            a.description = $"Charges {a.level * 2} physical damage for each turn target is not attacked, damage is released" +
            " on attacker once a hit is taken.";
            return a;
        }

        public static Ability KeenEye(string aname, int alevel = 0)
        {
            Ability a = new Ability(aname)
            {
                type = Ability.AType.Defensive,
                //
                agility = 0,
                //
                staminaDrain = 80,
                //
                level = 0,
                maxLevel = 50,
                tier = 1,
                statusEffect = "Keen Evaluation",
                evolves = new string[] { "Counter Prep", "Reflect" }
            };
            //
            Set(a, alevel);
            a.description = $"+{5 + (a.level / 10)} Critical Chance and +{(5 + (a.level / 10)) * 2} Critical Damage per turn for {5 + (a.level / 10)} turns." +
            $"Effect wears off after casting an offense ability";
            return a;
        }

        public static Ability Reflect(string name, int level = 0)
        {
            Ability a = Set(new Ability(name)
            {
                type = Ability.AType.Defensive,

                staminaDrain = 100,
                level = 1,
                maxLevel = 60,
                tier = 1,

                statusEffect = "Keen Evaluation",
                evolves = new string[] { "Counter Prep", "Keen Eye"}
            }, level);

            a.description = $"Has a {5 + (level / 2)}% chance to reflect {5 + level}% incoming damage ignoring resistance. " +
                $"Effect last {3 + (level / 5)} turns and wears off after one use.";
            return a;
        }
    }
}
