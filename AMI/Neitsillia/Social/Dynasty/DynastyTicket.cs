using System;
using static AMI.Neitsillia.NeitsilliaCommands.Social.Dynasty.Dynasty;

namespace AMI.Neitsillia.NeitsilliaCommands.Social.Dynasty
{
    class DynastyTicket
    {
        //Link to user
        public Guid id;
        public string dynastyName;

        public ulong userId;
        public DynastyMemberRanks memberRank;

        public DynastyTicket(Dynasty dynasty, DynastyMember member)
        {
            id = dynasty._id;
            dynastyName = dynasty.Name;

            userId = member.UserId;
            memberRank = member.Rank;
        }

        internal string GetDynastyTitle(bool isMale)
        {
            string s = memberRank.ToString();
            switch(memberRank)
            {
                case DynastyMemberRanks.Duke:
                    s = isMale ? "Duke" : "Duchess";
                    break;
                case DynastyMemberRanks.Baron:
                    s = isMale ? "Baron" : "Baroness";
                    break;
                case DynastyMemberRanks.HighKnight:
                    s = "High Knight";
                    break;
            }
            return s + " of " + dynastyName;
        }
    }
}
