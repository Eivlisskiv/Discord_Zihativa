using AMI.Neitsillia.User.PlayerPartials;

namespace AMI.Neitsillia.Reputation
{
    class Reputation
    {
        public string _id;

        public int karma;

        public Reputation(Player player)
        {
            _id = player._id;
        }
    }
}
