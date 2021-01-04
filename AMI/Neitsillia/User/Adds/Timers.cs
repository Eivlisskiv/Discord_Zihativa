using Newtonsoft.Json;
using System;

namespace AMI.Neitsillia.User
{
    class Timers
    {
        public ulong id;
        public DateTime dailyLoot;
        public DateTime adventureTime;
        public DateTime restTime;
        public DateTime floorJumpCooldown;
        //Tools
        public DateTime sickleUsage;
        public DateTime axeUsage;
        public DateTime pickaxeUsage;

        [JsonConstructor]
        public Timers(bool json = true) { }
        public Timers(ulong userID)
        {
            id = userID;
        }

        public double[] AdventureTime()
        {
            if (adventureTime == null || adventureTime.Year == 1)
                return new double[] { -1, -1};
            double time = (DateTime.UtcNow - adventureTime).TotalHours;
            double hours = Math.Floor(time);
            double minutes = Math.Round(time - hours, 2);
            double[] results =
            {hours, minutes};
            return results;
        }
        public void StartAdventure()
        {
            adventureTime = DateTime.UtcNow;
        }

        internal void EndRest()
        {
            restTime = new DateTime(1, 1, 1);
        }
        internal bool CanFloorJump() => (DateTime.UtcNow - floorJumpCooldown).TotalSeconds > 0;
        internal static string CoolDownToString(DateTime time, string els = "Ready")
        {
            if (time <= DateTime.UtcNow)
                return els;
            return CoolDownToString(time - DateTime.UtcNow);
        }
        internal static string CoolDownToString(TimeSpan left)
        {
            if (left.Days > 14)
                return $"{Math.Round(left.TotalDays)}Days";
            else if (left.Days > 0)
                return $"{left.Days}Days {left.Hours}h";
            else if (left.Hours > 0)
                return $"{left.Hours}h {left.Minutes}m";
            else if (left.Minutes > 0)
                return $"{left.Minutes}m {left.Seconds}s";
            return $"{left.Seconds}s";
        }
    }
}
