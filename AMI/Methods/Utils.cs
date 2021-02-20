using AMI.Methods.Graphs;
using AMYPrototype;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace AMI.Methods
{
    static class Utils
    {
        internal static Random Rng => Program.rng;

        internal static MethodInfo GetFunction(Type classType, string methodName, bool isTry = false)
        {
            MethodInfo method = classType.GetMethod(methodName);
            if (method == null && !isTry)
                throw new Exception($"Method {methodName} was not found in {classType.Namespace}");
            return method;
        }
        internal static T RunMethod<T>(string methodName, object obj, params object[] parameters)
        {
            return (T)GetFunction(obj.GetType(), methodName).Invoke(obj, parameters);
        }
        internal static T RunMethod<T>(string methodName, Type classType, params object[] parameters)
        {
            return (T)GetFunction(classType, methodName).Invoke(null, parameters);
        }
        //
        internal static FieldInfo GetVar(Type classType, string fieldName, bool isTry = false)
        {
            FieldInfo field = classType.GetField(fieldName);
            if (field == null && !isTry)
                throw new Exception($"Field {fieldName} was not found in {classType.Namespace}");
            return field;
        }
        internal static R GetVar<R>(Type classType, string fieldName, bool isTry = false)
        {
            FieldInfo field = classType.GetField(fieldName);
            if (field == null && !isTry)
                throw new Exception($"Field {fieldName} was not found in {classType.Namespace}");
            return field == null ? default : (R)field.GetValue(null);
        }
        internal static R GetVar<R, T>(T classObj, string fieldName, bool isTry = false)
        {
            FieldInfo field = classObj.GetType().GetField(fieldName);
            if (field == null)
            {
                if (!isTry)
                    throw new Exception($"Field {fieldName} was not found in {classObj.GetType().Namespace}");
                else return default;
            }
            return (R)field.GetValue(classObj);
        }
        internal static V SetVar<T, V>(T classobj, string fieldName, V value)
        {
            FieldInfo field = classobj.GetType().GetField(fieldName);
            if (field == null)
                throw new Exception($"Field {fieldName} was not found in {classobj.GetType().Namespace}");
            field.SetValue(classobj, value);
            return (V)field.GetValue(classobj);
        }
        //
        internal static T Clone<T>(T arg)
        {
            string objstring = JsonConvert.SerializeObject(arg);
            return JsonConvert.DeserializeObject<T>(objstring);
        }

        internal static IEnumerable<Type> GetTypesWithAttribute(Type attribute)
        {
            foreach (Type type in Program.assembly.GetTypes())
            {
                if (type.GetCustomAttributes(attribute, true).Length > 0)
                {
                    yield return type;
                }
            }
        }
        internal static List<Type> GetTypesWithBaseClass(Type baseClass)
        {
            return Program.assembly.GetTypes().ToList()
                .FindAll(t => t.BaseType?.FullName == baseClass.FullName);
        }

        

        internal static string JSON<T>(T obj) => JsonConvert.SerializeObject(obj);
        internal static T JSON<T>(string json) => JsonConvert.DeserializeObject<T>(json);
        internal static T JSONFromFile<T>(string path)
        {
            using System.IO.StreamReader sr = new System.IO.StreamReader(path);
            return JSON<T>(sr.ReadToEnd());
        }

        internal static object[] RunExecutable(string exePath, string arguments = null, bool wait = false)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                CreateNoWindow = true,
                UseShellExecute = true,
                FileName = exePath,
                WindowStyle = ProcessWindowStyle.Normal,
                Arguments = arguments
            };

            try
            {
                // Start the process with the info we specified.
                // Call WaitForExit and then the using statement will close.
                using (Process exeProcess = Process.Start(startInfo))
                {
                    if(wait)
                        exeProcess.WaitForExit();
                }
            }
            catch(Exception e)
            {
                Log.LogS(e);
            }
            return null;
        }

        internal static (T, V) RandomElement<T, V>(Dictionary<T, V> dict)
        {
            KeyValuePair<T, V> keypair = dict.ElementAt(Rng.Next(0, dict.Count()));
            return (keypair.Key, keypair.Value);
        }

        internal static T RandomElement<T>(List<T> list)
        {
            return list[Rng.Next(0, list.Count())];
        }

        internal static T RandomElement<T>(IEnumerable<T> list)
            => list == null || list.Count() == 0 ? default : list.ElementAt(Rng.Next(0, list.Count()));

        internal static T RandomElement<T>(params T[] list) 
            => list == null || list.Length == 0 ? default : list[Rng.Next(0, list.Length)];

        internal static T RandomElement<T>() where T : Enum
        {
            var list = (T[])Enum.GetValues(typeof(T));
            return list.ElementAt((Rng).Next(0, list.Count()));
        }

        internal static bool Divisible(int a, int b)
        {
            return a % b == 0;
        }

        internal static string Display(int num)
        {
            (int min, string s)[] dis =
            {
                (1000000, "M"),
                (1000, "K"),
            };

            for (int i = 0; i < dis.Length; i++)
                if (CheckDisplay(num, dis[i].min, dis[i].s, out string r))
                    return r;
            return num.ToString();
        }

        public static string XpDetail(int level, long xp, int per)
        {
            long next = Quadratic.F_longQuad(level + 1, per, 0, 0);
            return $"[{level}] {Display(xp)}/{Display(next)} XP | {Display(next - xp)}";
        }

        internal static string Display(long num)
        {
            (long min, string s)[] dis =
            {
                (1000000, "M"),
                (1000, "K"),
            };

            for(int i = 0; i < dis.Length; i++)
            if (CheckDisplay(num, dis[i].min, dis[i].s, out string r))
                return r;
            return num.ToString();
        }

        static bool CheckDisplay(int n, int min, string s, out string r)
        {
            if(n > min)
            {
                r = (Math.Floor(n / (min / 10.00)) / 10) + s;
                return true;
            }
            r = null;
            return false;
        }

        static bool CheckDisplay(long n, long min, string s, out string r)
        {
            if (n > min)
            {
                r = (Math.Floor(n / (min / 10.00)) / 10) + s;
                return true;
            }
            r = null;
            return false;
        }

        public static void Map<T>(List<T> @this, Func<T, int, bool> func)
        {
            int count = @this.Count;
            for(int i = 0; i < count; i++)
            {
                if (!func(@this[i], i)) i++;
                else
                {
                    @this.RemoveAt(i);
                    count = @this.Count;
                }
            }
        }

        public static void Map<T> (T[] @this, Action<T, int> func)
        {
            int count = @this.Length;
            for (int i = 0; i < count; i++)
                func(@this[i], i);
        }

        public static async System.Threading.Tasks.Task MapAsync<T>
            (List<T> @this, Func<T, int, System.Threading.Tasks.Task<bool>> func)
        {
            int count = @this.Count;
            for (int i = 0; i < count; i++)
            {
                if (!await func(@this[i], i)) i++;
                else
                {
                    @this.RemoveAt(i);
                    count = @this.Count;
                }
            }
        }

        public static async System.Threading.Tasks.Task MapAsync<T>
            (T[] @this, Func<T, int, System.Threading.Tasks.Task> func)
        {
            int count = @this.Length;
            for (int i = 0; i < count; i++)
                await func(@this[i], i);
        }
    }
    static class Time
    {
        internal const int Milisecond = 1;
        internal const int Second = 1000;
        internal const int Minute = 60000;
        internal const int Hour = 3600000;
        internal const int Day = 86400000;
    }
}
