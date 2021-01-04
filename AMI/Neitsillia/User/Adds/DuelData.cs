using AMI.Neitsillia.Items;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AMI.Neitsillia.User
{
    class DuelData
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
