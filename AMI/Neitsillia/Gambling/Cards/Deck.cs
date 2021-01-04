using AMI.Neitsillia.User.PlayerPartials;
using AMYPrototype;
using Discord;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AMI.Neitsillia.Gambling.Cards
{
    class Deck
    {
        static AMIData.MongoDatabase Database => AMYPrototype.Program.data.database;
        static Random rng => AMYPrototype.Program.rng;

        internal static Deck Load(Player player)
        {
            if (player.GamblingHand == null) return null; //They should have a hand if they have a deck?
            string id = player.IsSolo ? player._id : player.Party.partyName;
            return Database.LoadRecord(null, AMIData.MongoDatabase.FilterEqual<Deck, string>("_id", id));
        }

        public string _id;
        public List<Card> cards;

        public Hand house;

        public ulong guildId = 0;
        public ulong channelId = 0;

        internal IMessageChannel Channel
        {
            get
            {
                return (IMessageChannel)(guildId == 0 ? Program.clientCopy.GetChannel(channelId)
                    : Program.clientCopy.GetGuild(guildId)?.GetChannel(channelId));
            }
            set
            {
                channelId = value.Id;
                if (value is IGuildChannel gchan) guildId = gchan.GuildId;
                Save();
            }
        }

        public Deck(string id, bool shufflesInsert = true, bool hasJokers = false)
        {
            _id = id;
            cards = new List<Card>();

            Card.Shape[] shapes = (Card.Shape[])Enum.GetValues(typeof(Card.Shape));
            for (int i = 1; i <= 13; i++)
            {
                foreach (Card.Shape s in shapes)
                {
                    Card card = new Card(i, s);
                    cards.Insert(shufflesInsert ? 0 : rng.Next(0, cards.Count), card);
                }
                    
            }

            if (hasJokers)
            {
                cards.Insert(shufflesInsert ? 0 : rng.Next(cards.Count), new Card(0, Card.Shape.Diamond));
                cards.Insert(shufflesInsert ? 0 : rng.Next(cards.Count), new Card(0, Card.Shape.Spade));
            }
        }

        internal void Save()
        {
            Database.UpdateRecord(null, null, this);
        }

        internal async System.Threading.Tasks.Task Delete()
        {
            await Database.DeleteRecord<Deck>(null, _id);
        }

        internal List<Card> Pick(int amount = 1, bool rngPick = false)
        {
            int index = rngPick ? 0 : rng.Next(0, cards.Count - amount);

            List<Card> picks = cards.GetRange(index, amount);
            cards.RemoveRange(index, amount);

            return picks;
        }

        internal List<Card> PickRandom(int amount = 1)
        {
            List<Card> picks = new List<Card>();
            for (int i = 0; i < amount; i++)
            {
                int index = rng.Next(cards.Count);
                picks.Add(cards[index]);
                cards.RemoveAt(index);
            }

            return picks;
        }

        internal void Shuffle() => cards.OrderBy(x => rng.Next(cards.Count));
    }
}
