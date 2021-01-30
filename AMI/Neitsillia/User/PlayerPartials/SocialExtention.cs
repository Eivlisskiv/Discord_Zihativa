using AMI.AMIData;
using AMI.Neitsillia.NeitsilliaCommands;
using AMI.Neitsillia.NeitsilliaCommands.Social.Dynasty;

namespace AMI.Neitsillia.User.PlayerPartials
{
    partial class Player
    {
        public DataBaseRelation<string, Party> PartyKey;

        public DynastyTicket dynasty;

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
