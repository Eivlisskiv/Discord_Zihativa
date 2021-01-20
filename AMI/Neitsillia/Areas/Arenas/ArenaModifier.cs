using System.Collections.Generic;

namespace AMI.Neitsillia.Areas.Arenas
{
    //TEST - IGNORE
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
        public int wavesPerRounds = 5;
        public double koinMult = 1;
        public double xPMult = 1;
        public double lootMult;

        public ArenaModifier(string[] bools)
        {
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

        public bool WaveProgress(int score)
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
