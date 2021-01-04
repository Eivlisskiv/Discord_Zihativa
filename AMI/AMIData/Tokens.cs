using AMI.Methods;

namespace AMI.AMIData
{
    public class Tokens
    {
        public enum Platforms { Windows, Linux}

        public string discord;
        public string dbl;

        public string mongoUser;
        public string mongoPass;

        public Platforms platform = Platforms.Linux;

        public Tokens() { }

        internal static Tokens Load(string filepath) => Utils.JSONFromFile<Tokens>(filepath);
    }
}
