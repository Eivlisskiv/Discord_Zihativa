using AMI.Methods;
using AMYPrototype;
using Newtonsoft.Json;
using System.IO;

namespace AMI.Neitsillia.User
{
    public class USettings
    {
        public int[] RGB = new int[3];
        public ulong userID;

        [JsonConstructor]
        public USettings(bool json = false) { }
        public USettings(ulong iD)
        {
            userID = iD;
        }
        public void ModifyMultiValue(string property, string[] values)
        {
            if (property == "color")
                for (int i = 0; i < values.Length; i++)
                    RGB[i] = int.Parse(values[i]);

        }

        public Discord.Color Color()
        {
            return new Discord.Color(RGB[0], RGB[1], RGB[2]);
        }
    }
}
