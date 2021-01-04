using AMI.AMIData;
using AMI.Neitsillia.NeitsilliaCommands;

namespace AMI.Neitsillia.User.PlayerPartials
{
    partial class Player
    {
        public DataBaseRelation<string, Party> PartyKey;

        internal Party Party
        {
            get
            {
                if (PartyKey == null) return null;
                if (PartyKey.Data == null)
                {
                    PartyKey = null;
                    return null;
                }
                return PartyKey.Data;
            }
        }

    }
}
