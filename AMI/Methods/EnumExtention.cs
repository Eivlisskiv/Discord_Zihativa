using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AMI.Methods
{
    public static class EnumExtention
    {
        public static bool IsEnum<T>(string name, out T @enum) where T : Enum
        {
            if(Enum.TryParse(typeof(T), name, true, out object @out))
            {
                @enum = (T)@out;
                return true;
            }
            @enum = GetValues<T>()[0];
            return false;
        }


        public static string[] GetNames<T>() where T : Enum
            => Enum.GetNames(typeof(T));

        public static T[] GetValues<T>() where T : Enum
            => (T[])Enum.GetValues(typeof(T));
    }
}
