using AMI.Methods.Graphs;
using AMYPrototype;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AMI.Methods
{
    static class ArrayM
    {
        public static int IndexWithRates(int length, Random rng) 
            => length > 1 ? Exponential.IndexFromRate(length, rng.Next(100) + rng.NextDouble()) : 0;


        public static T[] FillWith<T>(T[] array, T fill)
        {
            for (int i = 0; i < array.Length; i++)
                array[i] = fill;

            return array;
        }
        public static T[] AddItem<T>(T[] array, T item)
        {
            int count = 0;
            if (array != null)
                count = array.Length;
            T[] temp = new T[count + 1];
            for (int i = 0; i < count; i++)
                temp[i] = array[i];
            temp[count] = item;
            return temp;
        }
        public static T[] MoveItem<T>(T[] array, int iTo, int iFrom)
        {
            T[] result = new T[array.Length];
            T s1 = array[iTo];
            T s2 = array[iFrom];
            if (iFrom - iTo < 2 && iFrom - iTo > -2)
            {
                result = array;
                result[iFrom] = s1;
                result[iTo] = s2;
                return result;
            }
            else if (iFrom < iTo)
            {
                int i = 0;
                for (; i < iFrom; i++)
                    result[i] = array[i];
                for (; i < iTo; i++)
                    result[i] = array[i + 1];
                result[i] = s2;
                for (; i < result.Length; i++)
                    result[i] = array[i];

                return result;
            }
            return array;
        }
        public static T[] RemoveItem<T>(T[] array, T item)
        {
            return RemoveItem(array, GetArrayIndex(array, item));
        }
        public static T[] RemoveItem<T>(T[] array, int index)
        {
            if (index > -1)
            {
                T[] result = new T[array.Length - 1];
                for (int i = 0; i < result.Length; i++)
                {
                    if (i < index)
                        result[i] = array[i];
                    else if (i > index)
                        result[i - 1] = array[i];
                }
                return result;
            }
            Log.LogS("Array Item To Remove was not found");
            return array;
        }
        public static int GetArrayIndex<T>(T[] options, T value)
        {
            if (options == null)
                return -1;
            bool isOption = false;
            for (int i = 0; i < options.Length && !isOption; i++)
                if (options[i].Equals(value))
                    return i;
            return -1;
        }
        public static int[] GetArrayIndex<T>(T[][] options, T value)
        {
            if (options == null)
                return new[] { -1, -1};
            bool isOption = false;
            for (int i = 0; i < options.Length && !isOption; i++)
            {
                int j = GetArrayIndex(options[i], value);
                if (j > -1)
                    return new[] { i, j };
            }
            return new[] { -1, -1 };
        }

        public static T[][] VerifyContent<T>(T[][] array)
        {
            //Verifies if the array and its sub arrays to remove them 
            //if they only have a length of 0
            for(int i = 0; i < array.Length; i++)
            {
                    if (array[i].Length < 1)
                        array = RemoveItem(array, i);
            }
            if (array.Length < 1)
                return null;
            return array;
        }
        public static int Total(int[] array)
        {
            int total = 0;
            for (int i = 0; i < array.Length; i++)
                total += array[i];

            return total;
        }
        public static long Total(long[] array)
        {
            long total = 0;
            for (int i = 0; i < array.Length; i++)
                total += array[i];

            return total;
        }
        public static double Total(double[] array)
        {
            double total = 0;
            for (int i = 0; i < array.Length; i++)
                total += array[i];

            return total;
        }
        public static int TotalIndexes<T>(T[][] array)
        {
            int t = 0;
            if (array == null)
                return 0;
            for (int i = 0; i < array.Length; i++)
                for (int j = 0; j < array[i].Length; j++)
                    t++;
            return t;
        }
        public static int FindIndex(string value, string[] array)
        {
            if (array == null)
                return -1;
            for (int i = 0; i < array.Length; i++)
                if (array[i].ToLower() == value.ToLower())
                    return i;
            return -1;
        }
        public static bool IsInArray(string value, string[] options)
        {
            if (options == null)
                return false;
            bool isOption = false;
            for (int i = 0; i < options.Length && !isOption; i++)
                if (options[i].ToLower() == value.ToLower())
                    isOption = true;
            return isOption;
        }
        public static bool IsInArray<T>(T value, T[] options)
        {
            bool isOption = false;
            for (int i = 0; i < options.Length && !isOption; i++)
                if (options[i].Equals(value))
                    isOption = true;
            return isOption;
        }
        public static int FindClosestValue(int target, int[] options)
        {
            int difference = 0;
            int minus = 0;
            int plus = 0;
            bool end = false;
            while (!end)
            {
                minus = target - difference;
                plus = target + difference;

                if (IsInArray(minus, options))
                    return minus;
                else if (IsInArray(plus, options))
                    return plus;
                else
                    difference++;
            }
            return options[Program.rng.Next(options.Length)];

        }
        public static T[][] RemoveCount<T>(T[][] array, int remAmount, int crement = -1)
        {
            int i = 0;
            int[] at = new int[2];
            if (crement > 0)
                crement = 1;
            else
            { crement = -1; i = array.Length - 1; }
            if (remAmount == 0)
                return array;
            for(; i < array.Length && i > -1 && remAmount > 0; i += crement)
            {
                if (array[i].Length <= remAmount)
                { at[0] = i + crement; remAmount -= array[i].Length; }
                else if(array[i].Length > remAmount)
                { at[0] = i; at[1] = remAmount - 1 ; remAmount = 0; }
            }
            int l = at[0] + 1;
            T[][] result = new T[l][];
            if (crement > 0)
            {
                i = 0;
                for (; i != at[0]; i++)
                    result[i] = array[i];
                for (int k = 0; k < at[1]; k++)
                    result[at[1]] = AddItem(result[at[1]], array[at[0]][k]);
                return result;
            }
            else if (crement < 0)
            {
                l = (array.Length - (at[0]));
                result = new T[l][];
                i = at[0] + 1;
                for (int k = at[1]; k < array[at[0]].Length; k++)
                    result[at[1]] = AddItem(result[at[1]], array[at[0]][k]);
                for (; i < array.Length; i++)
                    result[i] = array[i];
                return result;
            }
            return null;
        }
        public static List<T>[] RemoveCount<T>(List<T>[] alist, int toRemove, int crement = -1)
        {
            List<List<T>> list = new List<List<T>>();
            foreach (var l in alist)
                list.Add(l);

            while (toRemove > 0)
            {
                int x = 0;
                int y = 0;
                if (crement > 0)
                {
                    x = list.Count - 1;
                    y = list[x].Count - 1;
                }
                if (list[x].Count <= toRemove)
                {
                    toRemove -= list[x].Count;
                    list.RemoveAt(x);
                }
                else
                {
                    list[x].RemoveAt(y);
                    toRemove--;
                }
            }
            return list.ToArray();
        }
        public static string ToKString<T>(T[] array, string seperator = " ", int skipIndex = -1)
        {
            if (array == null)
                return null;
            if (array.Length == 0)
                return null;
            string result = "";
            for (int i = 0; i < array.Length; i++)
            {
                if (i != skipIndex)
                {
                    result += array[i];
                    if (i < array.Length - 1)
                        result += seperator;
                }
            }
            return result;
        }
        public static string ToString<T>(this IEnumerable<T> array, string seperator = " ")
        {
            if (array == null || System.Linq.Enumerable.Count(array) == 0)
                return null;
            return string.Join(seperator, array);
        }
        public static string ToString<T>(List<T>[] array, string sep1 = "\r\n", string sep2 = ", ")
        {
            if (array == null)
                return null;
            string r = null;
            foreach (var a in array)
                r += string.Join(sep2, a) + sep1;
            return r;
        }

        internal static string Join<T>(this IEnumerable<T> items, string seperator, Func<T, string> func)
            => items.Select(func).ToString(seperator);
        internal static string Join<T>(this IEnumerable<T> items, string seperator, Func<T, int, string> func)
        {
            string s = null;
            int l = items.Count();
            for (int i = 0; i < l; i++)
                s += func(items.ElementAt(i), i) + (i + 1 < l ? seperator : "");
            return s;
        }

        public static List<T> ToList<T>(T[] array)
        {
            List<T> list = new List<T>();
            foreach (T i in array)
                list.Add(i);
            return list;
        }
        public static List<T>[] ToList<T>(T[][] array)
        {
            List<T>[] list = new List<T>[array.Length];
            for(int i = 0; i < array.Length; i++)
            {
                list[i] = ToList(array[i]);
            }
            return list;
        }

        internal static string ToUpString(string[] args)
        {
            return StringM.UpperAt(ToString(args, " "));
        }

        internal static int[] AddEach(int[] array, int amount)
         => array.Select(i => i += amount).ToArray();

        internal static long[] AddEach(long[] array, long amount)
         => array.Select(i => i += amount).ToArray();
        internal static double[] AddEach(double[] array, double amount) 
            => array.Select(i => i += amount).ToArray();
        internal static float[] AddEach(float[] array, float amount)
            => array.Select(i => i += amount).ToArray();

    }
}
