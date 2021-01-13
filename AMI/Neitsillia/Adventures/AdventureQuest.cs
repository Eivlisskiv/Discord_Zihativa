using System;

namespace AMI.Neitsillia.Adventures
{
    public class AdventureQuest
    {
        public double hoursTime;

        public string TimeLeft(DateTime start)
        {
            DateTime end = start.AddHours(hoursTime);
            if(DateTime.UtcNow < end)
                return $"Quest time left: {User.Timers.CoolDownToString(end - DateTime.UtcNow)}";

            return "Quest completed.";
        }
    }
}
