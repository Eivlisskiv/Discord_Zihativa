namespace AMI.Neitsillia.NeitsilliaCommands.Social.Dynasty
{
    class DynastyMember
    {
        //for list of members
        public ulong UserId;
        public DynastyMemberRanks Rank;

        public DynastyMember(ulong id, DynastyMemberRanks rank)
        {
            this.UserId = id;
            this.Rank = rank;
        }
    }
}
