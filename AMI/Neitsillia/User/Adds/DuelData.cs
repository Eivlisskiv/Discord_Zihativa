namespace AMI.Neitsillia.User
{
    public class DuelData
    {
        public string opponentPlayerPath;
        public string target = "Default";
        public int coinsBet;

        public string abilityName;
        public ulong replyToChannel; //"guildid/channelid"

        public DuelData(string enemypath)
        {
            opponentPlayerPath = enemypath;
        }
    }
}
