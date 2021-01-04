using AMI.Methods;
using AMI.Neitsillia.NeitsilliaCommands;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;

namespace AMI.Neitsillia.Campaigns
{
    class Campaign
    {
        internal static string cmpgPath = @"Data/Campaigns/";

        public string name = "Undefined";
        public ulong owner;

        public int[] progress = { 0, 0 };
        public List<MileStone> milestones = new List<MileStone>();

        [JsonConstructor]
        public Campaign(bool json) { }
        internal static Campaign Load(string name)
        {
            if (File.Exists(name))
                return FileReading.LoadJSON<Campaign>(name);

                foreach (FileInfo file in new DirectoryInfo(
                    cmpgPath).GetFiles())
                    if(file.Name == name)
                        return FileReading.LoadJSON<Campaign>(file.FullName);
            return null;
        }
    }
    class MileStone
    {
        public List<Chapter> chapters = new List<Chapter>();
    }
    class Chapter
    {
        public List<string> objectives = new List<string>();
    }
}
