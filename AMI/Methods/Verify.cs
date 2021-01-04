using System;
using System.Collections.Generic;

namespace AMI.Methods
{
    static class Verify
    {
        public static (int index, int amount) IndexXAmount(string arg)
        {
            string[] splitArg = arg.Split('x', 'X', '*');
            int amount = 1;
            int index = -1;
            try
            {
                index = int.Parse(splitArg[0]);
                if (splitArg.Length > 1)
                    amount = int.Parse(splitArg[1]);

            }catch (Exception) { throw Module.NeitsilliaError.ReplyError($"Incorrect format entered, Item x Count Format: ``ItemSlot*Amount``"); }
            return (index, amount);
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
        {
            if (x < min)
                return min;
            return x;
        }
        public static double Min(double x, double min = 0)
        {
            if (x < min)
                return min;
            return x;
        }
        public static long Min(long x, long min = 0)
        {
            if (x < min)
                return min;
            return x;
        }
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
        {
            if (x > max)
                return max;
            return x;
        }
        internal static int Max(int x, int max)
        {
            if (x > max)
                return max;
            return x;
        }
        internal static double Max(double x, double max)
        {
            if (x > max)
                return max;
            return x;
        }
        public static int VerifyInt(string value, int baseValue = 0)
        {
            if (value == null)
                return baseValue;
            else if (!int.TryParse(value, out int result))
            {
                Log.LogS(value + " Could not be int.Parsed()");
                return baseValue;
            }

            return int.Parse(value);
        }
        public static long VerifyLong(string value, int baseValue = 0)
        {
            if (value == null)
                return baseValue;
            else if (!long.TryParse(value, out long aresult))
            {
                Log.LogS(value + " Could not be long.Parsed()");
                return baseValue;
            }
            return long.Parse(value);
        }
        public static double VerifyDouble(string value, double baseValue = 0)
        {
            if (value == null)
                return baseValue;
            else if (!Double.TryParse(value, out double result))
            {
                Log.LogS(value + " Could not be int.Parsed()");
                return baseValue;
            }

            return Double.Parse(value);
        }
        public static bool VerifyBool(string value, bool bdefault = false)
        {
            if (value == null)
                return bdefault;
            else if (!bool.TryParse(value, out bool aresult))
            {
                Log.LogS(value + " Could not be bool.Parsed()");
                return bdefault;
            }
            return bool.Parse(value);
        }    }
}
