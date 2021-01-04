using AMI.Neitsillia;
using System;

namespace AMI.Methods.Graphs
{
    class Quadratic
    {
        public static long XPCalc(int level)
        {
            return F_longQuad(level, ReferenceData.xpToLvlMult, 0, 0);
        }
        public static long AbilityXPRequirement(int level)
        {
            return F_longQuad(level, ReferenceData.xpToLvlAbility, 0, 0);
        }
        public static long F_longQuad(int x, int a, int b, int c)
        {
            long num = (long)(a * Math.Pow(x, 2)) + (b * x) + c;
            return num;
        }
        private static double F_dblQuad(double x, double a, double b, double c)
        {
            return (a * Math.Pow(x, 2)) + (b * x) + c;
        }
    }
}
