using AMI.Methods;
using System;

namespace AMI.Neitsillia.NPCSystems.Companions
{
    static class EggTypes
    {
        static Random R => AMYPrototype.Program.rng;
        const int MaxTier = 0;

        public static Egg GetEgg(int tier)
        {
            tier = Verify.MinMax(tier, MaxTier, 0);
            (string name, string desc) = ((string, string))Utils.GetFunction(typeof(EggTypes), $"Tier{tier}Egg").Invoke(null, null);
            return new Egg(false)
            {
                Tier = tier,
                Name = name,
                Description = desc,
                challenge = Utils.RandomElement<Egg.EggChallenge>(),
            };
        }

        public static (string, string) Tier0Egg()
        {
            return Utils.RandomElement( 
                ("Young Vhoizuku", "Young Vhoizuku egg"),

                ("Young Vhoizuku", "Young Vhoizuku egg")
            );
        }

    }
}
