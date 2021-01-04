using AMI.AMIData;
using AMI.Neitsillia.Adventures;

namespace AMI.Neitsillia.User.PlayerPartials
{
    partial class Player
    {
        public DataBaseRelation<string, Adventure> AdventureKey;

        internal Adventure Adventure
        {
            get
            {
                if (AdventureKey?.Data != null) return AdventureKey.Data;
                else if (AdventureKey != null)
                {
                    AdventureKey?.Delete();
                    AdventureKey = null;
                }
                return null;
            }
            set
            {
                if (value != null && AdventureKey == null)
                    AdventureKey = new DataBaseRelation<string, Adventure>(_id, value);
                else AdventureKey.Data = value;
            }
        }

        public bool IsInAdventure => Adventure != null;
    }
}
