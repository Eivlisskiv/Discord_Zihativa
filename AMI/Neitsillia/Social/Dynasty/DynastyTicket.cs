using System;

namespace AMI.Neitsillia.NeitsilliaCommands.Social.Dynasty
{
    public class DynastyTicket
    {
        //Link to user
        public Guid id;
        public ulong userId;

        public string dynastyName;
        public string memberRank;

        internal string MemberTitle => $"{memberRank} of {dynastyName}";

        public DynastyTicket(Dynasty dynasty, DynastyMember member)
        {
            id = dynasty._id;
            dynastyName = dynasty.name;

            userId = member.userId;
            memberRank = dynasty.rankNames[member.rank];
        }

        public void Update(Dynasty dynasty, int rank)
        {
            dynastyName = dynasty.name;
            memberRank = dynasty.rankNames[rank];
        }
    }
}
