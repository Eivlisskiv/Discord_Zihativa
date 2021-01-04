using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AMI.Methods
{
    static class Rolls
    {

        public static double DiceCheck(int roll, int num)
        {
            double result = 0;
            if (roll == num)
                result = 2;
            else if ((roll == num + 1 || roll == num - 1) || (roll == 1 && num == 6) || (roll == 6 && num == 1))
                result = 1.5;
            else if ((roll == num + 2 || roll == num - 2) || (roll == 2 && num == 6) || (roll == 5 && num == 1))
                result = 0.5;
            else
                result = 0;
            Console.WriteLine(num + " --- " + roll);
            return result;
        }
    }
}
