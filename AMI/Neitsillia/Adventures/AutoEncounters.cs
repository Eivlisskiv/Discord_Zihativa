namespace AMI.Neitsillia.Adventures
{
    static class AutoEncounters
    {
        //Starting
        public static readonly string[] startCautious =
        {
            "Rule number one: don't get caught.",
            "Hopefully things will go well."
        };

        public static readonly string[] startDaring =
        {
            "Critters won't be stopping me.",
            "Let's find some goodies!",
        };

        public static readonly string[] startReckless =
        {
            "To my health, and death to the enemy!",
            "Let's show this place a bad time.",
        };

        public static readonly string[] startDeathwish =
        {
            "I'll fight any creature I see!",
            "Everything goes!"
        };

        //Resting
        public static readonly string[] basicResting =
        {
            "I'm already exhausted. I'll camp for a while.",
        };

        //Looting
        public static readonly string[] basicLoot =
        {
            "I discovered loot! This place is not so bad after all.",
        };

        public static readonly string[] coinsLoot =
        {
            "A found a coins purse with {0} coins! Lucky me!",
            "An extra {0} coins for my pockets!",
        };

        //Other Encounters


        //enemy Encounters

        public static readonly string[] perfectWin =
        {
            "I defeated a nasty {0}.",
            "I really made that {0} bite the dust.",
        };

        public static readonly string[] hurtWin =
        {
            "I managed to defeat a {0}, but also took a few hits.",
            "I got a damn {0}. It didn't go down without a fight.",
        };

        public static readonly string[] escapeMob =
        {
            "A {0} overwhelmed me, but I made it out in one piece.",
            "I had to make a run for it, this {0} was giving me no chances.",
        };

        public static readonly string[] defeatedByMob =
        {
            "A {0} battered me, I barely made it out.",
        };

    }
}
