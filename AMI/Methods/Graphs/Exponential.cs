using System;

namespace AMI.Methods.Graphs
{
    class Exponential
    {
        private static double F_Rational(double a, double x, double h, double k)
            => (a / (x - (h))) + k;

        private static double F_Rational(long a, double x, long h, double k)
            => (a / (x - (h))) + k;

        public static double F2_Exponential(double a, double b, double x, double h, double k)
            => (a * Math.Pow(b, (x + h)) + k);

        public static double Armor(long x)
            => F_Rational(-7306, x, -91.32, 80.1);

        public static double Durability(int x)
            => F2_Exponential(24.3, 0.995, x, -100, 15);

        public static double ItemValue(int y, int m)
            => F_Rational(-5350, ((y * 100) / m), -53, 125) / 100;

        public static int MobEvolveRequirement(int level, int test)
                => level > 3800 ? 2500 : Convert.ToInt32(Math.Floor(F_Rational(-64000000, level, 12000, -5325)));

        public static double KoinsGeneratedWithMob(int level)
            => F_Rational(-28400000000, level, -284010, 100000);

        public static double ItemsGeneratedWithMob(int level)
            => F_Rational(-18600, level, -190, 100);

        internal static double Scaling(int x, double buffPercent, int m = 0)
        {
            //https://www.desmos.com/calculator/dh7uipcz4k
            double multiplier = 1;
            if (x < m + 25)
                multiplier = F2_Exponential(-0.0043, 1.178, x, 34.4 - m, 20.25) + 5;
            else
                multiplier = F2_Exponential(6, 2.7 + (m * -0.01), x, 44.6 + (m), 1.001);

            multiplier = Math.Round(multiplier, 2);

            return buffPercent * multiplier;
        }

        internal static double CreatureScale(int baselevel, int level)
        {
            double a = 0.767;
            double s = 0.0024;

            double h = 0.01;
            double v = -25;
            return (level - baselevel) * F2_Exponential(s, a, baselevel, v, h);
        }

        internal static int IndexFromRate(int length, double rng) 
            => NumbersM.FloorParse<int>(Math.Pow(length, 0.01 * rng) - 1);

        static double ReversedRates(int length, int i)
            => (Math.Log(i + 1) * 100) / Math.Log(length);

        public static double RateFromIndex(int length, int i)
            => ReversedRates(length, i + 1) - ReversedRates(length, i);
    }
}
