using AMI.Neitsillia.Items.Quests;
using System;

namespace AMI.Neitsillia.User
{
    class DailyQuest
    {
        public DateTime next;
        public Quest quest;
        public int level;

        public DailyQuest(int l)
        {
            next = DateTime.UtcNow.AddDays(-1);
            level = l;
        }
    }
}
