using System;

namespace AMI.Neitsillia.NPCSystems.Companions
{
    public class Egg
    {
        public enum EggChallenge { Combat, Exploration, Crafting };

        public int Tier;
        public string Name;
        public string Description;
        public EggChallenge challenge;

        public int hatchProgress;
        internal int RequiredProgress => (Tier + 1) * RequiredProgressByType();

        public static Egg Generate(int tier)
        => EggTypes.GetEgg(tier);

        public Egg(bool donotUse)
        {
            //Don't use this
        }

        public NPC Hatch()
        {
            NPC baby = NPC.GenerateNPC(0, Name);
            switch(challenge)
            {
                case EggChallenge.Combat:
                    baby.stats.strength++;
                    baby.stats.endurance++;
                    return baby;
                case EggChallenge.Exploration:
                    baby.stats.dexterity++;
                    baby.stats.perception++;
                    return baby;
                case EggChallenge.Crafting:
                    baby.stats.intelligence++;
                    baby.stats.charisma++;
                    return baby;
                default:
                    return baby;
            }
        }

        public string GetInfo()
        {
            return ((hatchProgress * 100) / RequiredProgress) + "% Hatch" +
                Environment.NewLine + Description;
        }

        int RequiredProgressByType()
        {
            switch(challenge)
            {
                case EggChallenge.Combat:
                    return 35;
                case EggChallenge.Crafting:
                    return 55;
                case EggChallenge.Exploration:
                    return 25;
            }
            return 35;
        }
    }
}
