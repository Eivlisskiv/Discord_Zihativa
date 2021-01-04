using AMYPrototype;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neitsillia.Methods
{
    static class RandomName
    {
        private static string[] c =
            {"b", "c", "d", "f", "g", "h", "j", "k", "l", "m", "n", "p", "q", "r", "s", "t", "v", "w", "x", "y", "z"}; //20
        private static string[] v =
            {"a", "e", "i", "o", "u"}; //5
        private static string[] c2 =
            {"ts", "is", "vl", "vh", "ts", "rh", "ch", "sk", "yh"}; //9

        public static string ARandomName(int nameLength = 5, bool random = false)
        {
            string name = null;
            int cv = v.Length + c.Length;
            string lchar = "c";

            Random x = Program.rng;
            if(random)
                nameLength = x.Next(nameLength - 2, nameLength + 4);
            int n = x.Next(0, c.Length  + c2.Length + v.Length);

            if (n < v.Length)
            {
                name = v[n].ToUpper();
                lchar = "v";
            }
            else if (n < cv)
                name = c[n - (v.Length - 1) - 1].ToUpper();
            else
            {
                char[] c3 = c2[n - cv].ToCharArray();
                name = c3[0].ToString().ToUpper() + c3[1];
            }
            nameLength--;

            for(int i = 0; i < nameLength; i++)
            {
                if(lchar == "c")
                {
                    int c = 0;
                    c = x.Next(0, v.Length);
                    name += v[c];
                    if (x.Next(0, 4) <= 0)
                    {
                        c = x.Next(0, v.Length);
                        name += v[c];
                        i++;
                    }
                    lchar = "v";
                }
                else if (lchar == "v")
                {
                    int v = 0;
                    if (x.Next(0, 3) <= 1 || i == nameLength - 1)
                    {
                        v = x.Next(0, c.Length);
                        name += c[v];
                    }
                    else
                    {

                        v = x.Next(0, c2.Length);
                        name += c2[v];
                    }
                    lchar = "c";
                }
            }
            return name;
        }
    }
}
