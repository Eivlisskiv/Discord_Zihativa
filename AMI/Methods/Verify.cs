using System;
using System.Collections.Generic;

namespace AMI.Methods
{
    static class Verify
    {
        public static (int index, int amount) IndexXAmount(string arg)
        {
            try
            {
                string[] splitArg = arg.Split('x', 'X', '*');
                int amount = 1;
                int index = 1;
                index = int.Parse(splitArg[0]);
                if (splitArg.Length > 1)
                    amount = int.Parse(splitArg[1]);
                return (index, amount);
            }
            catch (Exception) 
            { 
                throw Module.NeitsilliaError.ReplyError(
                    "Incorrect format entered, Item x Count Format: `{ItemSlot}x{Amount}`");
            }
            
        }
        public static List<(int index, int amount)> IndexXAmount(string[] arg)
        {
            List<(int index, int amount)> selections = new List<(int index, int amount)>();
            for (int i = 0; i < arg.Length; i++)
            {
                var data = IndexXAmount(arg[i]);
                int k = 0;

                data.index--;
                if (selections.Count > 0)
                    while (k < selections.Count &&
                          selections[k].index > data.index)
                            k++;
                else k = i;

                selections.Insert(k, data);
            }
            return selections;
        }

        public static bool IsInArray(string value, string[] options)
        {
            for (int i = 0; i < options.Length; i++)
                if (options[i].ToLower() == value.ToLower())
                    return true;
            return false;
        }
        public static bool IsInArray(char value, char[] options)
        {
            bool isOption = false;
            for (int i = 0; i < options.Length && !isOption; i++)
                if (options[i] == value)
                    isOption = true;
            return isOption;
        }
        public static int Min(int x, int min = 0)
            => Math.Max(x, min);
        public static double Min(double x, double min = 0)
            => Math.Max(x, min);
        public static long Min(long x, long min = 0)
            => Math.Max(x, min);
        public static int MinMax(int x, int max, int min = 0)
        {
            if (x < min)
                return min;
            else if (x > max)
                return max;
            return x;
        }
        public static double MinMax(double x, double max, double min = 0)
        {
            if (x < min)
                return min;
            else if (x > max)
                return max;
            return x;
        }
        public static long MinMax(long x, long max, long min = 0)
        {
            if (x < min)
                return min;
            else if (x > max)
                return max;
            return x;
        }
        internal static long Max(long x, long max)
            => Math.Min(x, max);
        internal static int Max(int x, int max)
            => Math.Min(x, max);
        internal static double Max(double x, double max)
            => Math.Min(x, max);  
    }
}
