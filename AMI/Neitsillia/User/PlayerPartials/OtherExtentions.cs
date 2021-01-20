using AMI.AMIData;
using AMI.AMIData.Events;
using AMI.Neitsillia.Collections;
using AMI.Neitsillia.Gambling.Cards;
using AMI.Neitsillia.NPCSystems.Companions;
using AMI.Neitsillia.NPCSystems.Companions.Pets;
using AMI.Neitsillia.Religion;

namespace AMI.Neitsillia.User.PlayerPartials
{
    partial class Player
    {
        public DataBaseRelation<string, PetList> PetListKey;
        internal PetList PetList
        {
            get
            {
                return (PetListKey ??= new DataBaseRelation<string, PetList>(_id, null)).Data 
                    ?? (PetListKey.Data = new PetList(_id));
            }
            set { (PetListKey ??= new DataBaseRelation<string, PetList>(_id, null)).Data = value; }
        }

        public DataBaseRelation<string, Hand> GamblingHandKey;
        internal Hand GamblingHand
        {
            get { return (GamblingHandKey ??= new DataBaseRelation<string, Hand>(_id, null)).Data; }
            set { (GamblingHandKey ??= new DataBaseRelation<string, Hand>(_id, null)).Data = value; }
        }

        public DataBaseRelation<string, ProgressData> ProgressDataKey;
        internal ProgressData ProgressData
        {
            get { return (ProgressDataKey ??= new DataBaseRelation<string, ProgressData>(_id, new ProgressData(_id))).Data; }
            set { (ProgressDataKey ??= new DataBaseRelation<string, ProgressData>(_id, null)).Data = value; }
        }

        public DataBaseRelation<string, Faith> FaithKey;
        internal Faith Faith
        {
            get { return (FaithKey ?? (FaithKey = new DataBaseRelation<string, Faith>(_id, new Faith(_id)))).Data; }
            set { (FaithKey ?? (FaithKey = new DataBaseRelation<string, Faith>(_id, null))).Data = value; }
        }

        public DataBaseRelation<string, Tools> ToolsKey;
        internal Tools Tools
        {
            get { return (ToolsKey ?? (ToolsKey = new DataBaseRelation<string, Tools>(_id, null))).Data; }
            set { (ToolsKey ?? (ToolsKey = new DataBaseRelation<string, Tools>(_id, null))).Data = value; }
        }

        public DataBaseRelation<string, EggPocket> EggPocketKey;
        internal EggPocket EggPocket
        {
            get { return (EggPocketKey ?? (EggPocketKey = new DataBaseRelation<string, EggPocket>(_id, null))).Data; }
            set { (EggPocketKey ?? (EggPocketKey = new DataBaseRelation<string, EggPocket>(_id, null))).Data = value; }
        }

        private PlayerCurrency _currency;
        internal PlayerCurrency Currency
        {
            get => _currency ?? (_currency = PlayerCurrency.Load(_id));
        }
    }
}
