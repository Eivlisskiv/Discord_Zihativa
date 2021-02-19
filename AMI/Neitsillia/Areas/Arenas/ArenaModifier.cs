using System.Collections.Generic;
using static AMI.Neitsillia.Areas.Arenas.Arena;

namespace AMI.Neitsillia.Areas.Arenas
{
    [MongoDB.Bson.Serialization.Attributes.BsonIgnoreExtraElements]
    public class ArenaModifier
    {
        public enum ArenaModifiers { };
        internal static readonly string[] ModifiersDesc = 
        {
            "test 1",
            "test 2",
            "test 3",
            "test 4",
            "test 5",
        };

        //Active Arena
        public List<ArenaModifiers> ActiveMods = new List<ArenaModifiers>();

        //->//Party Stats
        public long CurrentScore = 0;
        public int wave = 0;
        //
        public int wavesPerRounds = 3;
        public double koinMult = 1;
        public double xpMult = 1;
        public double lootMult;

        public ArenaModifier(bool json) { }

        public ArenaModifier(ArenaMode mode, string[] bools)
        {
            if (mode == ArenaMode.Survival) xpMult  = 5;
            if(bools != null)
            for (int i = 0; i < bools.Length; i++)
                if (bools[i] == "1")
                {
                    ArenaModifiers m = (ArenaModifiers)i;
                    LoadMod(m);
                    ActiveMods.Add(m);
                }
        }

        void LoadMod(ArenaModifiers mod)
        {
            switch(mod)
            {
                default: //todo
                    break;
            }
        }

        public bool WaveProgress(long score)
        {
            CurrentScore += score;
            wave++;
            if (wave == wavesPerRounds)
            {
                wave = 0;
                return true;
            }
            return false;
        }
    }
}
