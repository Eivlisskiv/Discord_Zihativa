using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AMI.Neitsillia.Gambling
{
    class GamblingUserData
    {
        public enum Game { };

        public string _id;
        public Game game;

        public string hand;

        public long bet;

        public string turn;
    }
}
