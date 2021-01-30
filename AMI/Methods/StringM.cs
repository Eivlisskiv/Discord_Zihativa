using AMI.Methods.Graphs;
using AMI.Neitsillia.Areas.Arenas;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace AMI.Methods
{
    static class StringM
    {
        public static string UpperAt(string a, params char[] z)
        {
            if (a == null || a.Length == 0)
                return null;
            if (z.Length < 1)
                z = new char[] { ' ', '-' };

            //return Regex.Replace(a.ToUpper(), 
            //    $"(?<![{ArrayM.ToString(z, "")}])[a-zA-Z]",
            //    x => x.ToString().ToLower());

            char[] b = a.ToCharArray();
            string c = b[0].ToString().ToUpper();
            for (int i = 1; i < b.Length; i++)
            {
                if (Verify.IsInArray(b[i - 1], z))
                    c += b[i].ToString().ToUpper();
                else
                    c += b[i].ToString().ToLower();
            }
            return c;
        }

        internal static bool RegexName(string @string, int min = 3, int max = 30, bool @throw = true)
        {
            if (@string.Length < min || @string.Length > max)
                throw Module.NeitsilliaError.ReplyError(
                        $"Names must be between {min} and {max} characters.");
            if (!Regex.Match(@string, @"^([a-zA-Z]|'|-|’|\s)+$").Success)
            {
                if(@throw)
                    throw Module.NeitsilliaError.ReplyError(
                        $"Names must only contain A to Z, (-), ('), (’) and spaces.");
                return false;
            }
            return true;
        }

        public static string TrimAt(string totrim, int toremove, bool fromstart = true)
        {
            string result = null;
            char[] str = totrim.ToCharArray();
            if (fromstart)
            {
                for (int i = toremove; i < str.Length; i++)
                    result += str[i];
            }
            else
            {
                for (int i = 0; i < toremove; i++)
                    result += str[i];
            }
            return result;
        }
        public static string UpperFormat(string s)
        {
            //return UpperAt(s, ' ');
            return Regex.Replace(s.ToLower(),
                @"\b[a-zA-Z]",
                x => x.Value.ToUpper());
            
        }

        public static string UpperFormat(params string[] s)
        {
            return s != null && s.Length > 0 ?
                UpperFormat(ArrayM.ToString(s, " ")) : null;
        }
    }
}
