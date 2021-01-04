using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AMI.Methods
{
    static class NumbersM
    {

        internal static dynamic NParse<T>(double v) where T : IComparable<T>
        {
            v = Math.Round(v);
            if (typeof(T) == typeof(int))
                return Convert.ToInt32(v);
            else if (typeof(T) == typeof(long))
                return Convert.ToInt64(v);
            return v;
        }
        internal static dynamic NParse<T>(decimal v) where T : IComparable<T>
        {
            v = Math.Round(v);
            if (typeof(T) == typeof(int))
                return Convert.ToInt32(v);
            else if (typeof(T) == typeof(long))
                return Convert.ToInt64(v);
            return v;
        }
        internal static dynamic NParse<T>(float f) where T : IComparable<T>
        {
            double v = Math.Round(f);
            if (typeof(T) == typeof(int))
                return Convert.ToInt32(v);
            else if (typeof(T) == typeof(long))
                return Convert.ToInt64(v);
            return v;
        }
        internal static dynamic CeilParse<T>(double v) where T : IComparable<T>
        {
            v = Math.Ceiling(v);
            if (typeof(T) == typeof(int))
                return Convert.ToInt32(v);
            else if (typeof(T) == typeof(long))
                return Convert.ToInt64(v);
            return v;
        }
        internal static dynamic CeilParse<T>(decimal v) where T : IComparable<T>
        {
            v = Math.Ceiling(v);
            if (typeof(T) == typeof(int))
                return Convert.ToInt32(v);
            else if (typeof(T) == typeof(long))
                return Convert.ToInt64(v);
            return v;
        }
        internal static dynamic CeilParse<T>(float f) where T : IComparable<T>
        {
            double v = Math.Ceiling(f);
            if (typeof(T) == typeof(int))
                return Convert.ToInt32(v);
            else if (typeof(T) == typeof(long))
                return Convert.ToInt64(v);
            return v;
        }
        internal static dynamic FloorParse<T>(float f) where T : IComparable<T>
        {
            double v = Math.Floor(f);
            if (typeof(T) == typeof(int))
                return Convert.ToInt32(v);
            else if (typeof(T) == typeof(long))
                return Convert.ToInt64(v);
            return v;
        }
        internal static dynamic FloorParse<T>(decimal v) where T : IComparable<T>
        {
            v = Math.Floor(v);
            if (typeof(T) == typeof(int))
                return Convert.ToInt32(v);
            else if (typeof(T) == typeof(long))
                return Convert.ToInt64(v);
            return v;
        }
        internal static dynamic FloorParse<T>(double v) where T : IComparable<T>
        {
            v = Math.Floor(v);
            if (typeof(T) == typeof(int))
                return Convert.ToInt32(v);
            else if (typeof(T) == typeof(long))
                return Convert.ToInt64(v);
            return v;
        }

        public static int[] OrderIndex(double[] array)
        {
            int[] res = ArrayM.FillWith(new int[array.Length],-1);
            List<int> ignore = new List<int>();
            for(int r = 0; r < res.Length; r++)
            {
                res[r] = HighestIndex(array, ignore.ToArray());
                ignore.Add(res[r]);
            }
            return res;
        }
        public static int HighestIndex(double[] array, params int[] ignore)
        {
            int x = -1;
            for (int i = 0; i < array.Length; i++)
                if (!ignore.Contains(i) &&
                   (x == -1 || array[i] >= array[x]))
                    x = i;
            return x;
        }
        public static int HighestIndex(int[] array, params int[] ignore)
        {
            int x = -1;
            for (int i = 0; i < array.Length; i++)
                if (!ignore.Contains(i) &&
                   (x == -1 || array[i] >= array[x]))
                    x = i;
            return x;
        }

        internal static string GetLevelMark(int v)
        {
            if (v > 10)
                return "Ω";
            else if (v > 0)
            {
                if(v >= 10) return "X" + GetLevelMark(v - 10);
                else if(v >= 9) return "IX" + GetLevelMark(v - 9);
                else if(v >= 5) return "V" + GetLevelMark(v - 5);
                else if(v >= 4) return "IV" + GetLevelMark(v - 4);
                else if(v >= 1) return "I" + GetLevelMark(v - 1);
            }
            return null;
        }
        internal static bool BeforeVersion(string tabCurrent, List<int> test)
        {
            List<int> current = tabCurrent.Split('.').Select(int.Parse).ToList();
            while (current.Count < test.Count)
                current.Add(0);
            while (test.Count < current.Count)
                test.Add(0);
            for (int i = 0; i < current.Count || i < test.Count; i++)
            {
                if (current[i] < test[i])
                    return true;
                else if (current[i] > test[i])
                    return false;
            }
            return false;
        }

        
    }
}
