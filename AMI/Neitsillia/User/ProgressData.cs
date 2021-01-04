using System.Collections.Generic;

namespace AMI.Neitsillia
{
    public class ProgressData
    {
        public string _id;
        public List<string> CompletedQuests;

        public ProgressData(string id)
        {
            _id = id;
            CompletedQuests = new List<string>();
        }

        public bool QuestIsCompleted(int[] id)
        {
            return CompletedQuests.Contains($"{id[0]},{id[2]},#");
        }

        internal void CompletedNewQuests(int[] id)
        {
            string s = $"{id[0]},{id[2]},#";
            if (!CompletedQuests.Contains(s))
                CompletedQuests.Add(s);
        }
    }
}