using System;

namespace AMI.Neitsillia.User
{
    public class Sheet
    {
        public int age;
        public string gender = "Unknown";
        public string appearance = "Unknown";
        public string personality = "Unknown";
        public string lore = "Unknown";

        public static string[] properties = new string[] 
        {"age", "appearance", "lore", "personality", "gender"};

        public bool ModifyProperty(string property, string value)
        {
            switch (property)
            {
                case "age":
                    if (!int.TryParse(value, out age))
                        throw Module.NeitsilliaError.ReplyError("Age must be a number");
                    break;
                case "appearance":
                appearance = value; break;
                case "personality":
                personality = value; break;
                case "lore":
                lore = value; break;
                case "gender":
                gender = value; break;
            }
            return true;
        }
        private int VerifyValue(string value)
        {
            if (value == null)
                return 0;
            else if (!int.TryParse(value, out int result))
            {
                Console.WriteLine(value + " Could not be int.Parsed()");
                return 0;
            }

            return int.Parse(value);
        }
        private string NullToNone(string value)
        {
            if (value == null || value == "")
                return "None";
            return value;
        }
    }
}
