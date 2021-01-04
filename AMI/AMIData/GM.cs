using Discord;
using Newtonsoft.Json;
using System;

namespace AMI.Module
{
    class GM
    {
        public ulong id;
        public string username;
        public int gmLevel;

        //{Coins, Item Tier Point, Mob Rank Points}
        public long[] limits = new long[]
            {0, 0, 0};
        public DateTime date;

        public string campaignPath;

        [JsonConstructor]
        public GM(bool json)
        {
            
        }
        public GM(IUser user, int level)
        {
            id = user.Id;
            username = user.Username;
            gmLevel = level;
            SetLimits();
        }
        public GM(ulong Id, string Username, int level)
        {
            id = Id;
            username = Username;
            gmLevel = level;
        }
        public static Predicate<GM> FindWithID(ulong id)
        {
            return delegate (GM gm) { return gm.id == id; };
        }
        public void VerifyTimer()
        {
            if (date.AddMonths(1) <= DateTime.Now.AddDays(DateTime.Now.Day * -1))
            {
                date = DateTime.Now.AddDays(DateTime.Now.Day * -1);
                date.AddHours(date.Hour * -1);
                date.AddMinutes(date.Minute * -1);
                SetLimits();
            }
        }
        public void ChangeLevel(int level)
        {
            this.gmLevel = level;
            SetLimits();
        }
        void SetLimits()
        {
            for (int i = 0; i < limits.Length; i++)
                limits[i] = AMI.AMIData.OtherCommands.GameMasterCommands.gmLimits[gmLevel][i];
        }
    }
}
