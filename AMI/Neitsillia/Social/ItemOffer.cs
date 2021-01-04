using AMI.AMIData;
using AMI.Neitsillia.Collections;
using AMI.Neitsillia.User.PlayerPartials;
using AMI.Neitsillia.User.UserInterface;
using Discord;
using Discord.WebSocket;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AMI.Neitsillia.NeitsilliaCommands.Social
{
    class ItemOffer
    {
        internal enum OfferQuery {Receiver, Sender};
        [BsonId]
        public Guid _id;
        public ulong sender;
        public ulong receiver;
        public StackedItems offer;
        public long pricePer;
        public string note;
        public DateTime whenSent;

        public ItemOffer(ulong s, ulong r, StackedItems o, long price, string n)
        {
            sender = s;
            receiver = r;
            offer = o;
            pricePer = price;
            note = n;
            whenSent = DateTime.UtcNow;
            Save();
        }
        void Save() => AMYPrototype.Program.data.database
            .SaveRecordAsync("Offer", this).Wait();
        internal static async Task GetOffers(Player player, int page, OfferQuery q, ISocketMessageChannel chan)
        {
            int itemPerPage = 5;
            
            ItemOffer[] array = null;
            EmbedBuilder em = player.UserEmbedColor(new EmbedBuilder());
                switch (q)
                {
                    case OfferQuery.Receiver:
                        array = await GetOffers(player.userid);
                        em.WithTitle("Received Offers");
                        break;
                    case OfferQuery.Sender:
                        array = await SentOffers(player.userid);
                        em.WithTitle("Sent Offers");
                        break;
                }
            if (array != null && array.Length > 0)
            {
                page = Methods.Verify.MinMax(page, Methods.NumbersM.CeilParse<int>(array.Length / 5.00));
                int x = 1;
                List<Guid> guids = new List<Guid>();
                for (int p = (itemPerPage * page); p < (itemPerPage * (page + 1))
                    && p < array.Length; p++, x++)
                {
                    em.AddField($"{EUI.GetNum(x)} {array[p]._id}", array[p].ToInfo(false));
                    guids.Add(array[p]._id);
                }
                em.WithFooter("Use reactions to inspect Offer and use accept and deny/delete options");
                await player.NewUI(await chan.SendMessageAsync(embed: em.Build()), MsgType.OfferList, $"{page}.{array.Length}.{q}.{JsonConvert.SerializeObject(guids)}");
            }
            else await chan.SendMessageAsync("No offers to display");
        }
        internal async Task InspectOffer(Player player, IMessageChannel chan)
        {
            //ItemOffer offer = await IdLoad(id);
            EmbedBuilder em = player.UserEmbedColor();
            em.WithTitle("Offer Inspection");
            if(sender == player.userid)
                em.WithDescription($"{EUI.cancel} To delete offer and retrieve item(s)");
            else if (receiver == player.userid)
                em.WithDescription($"{EUI.cancel} To deny offer | {EUI.ok} To accept offer");
            em.AddField(_id.ToString(), ToInfo(true));
            await player.NewUI(await chan.SendMessageAsync(embed: em.Build()), MsgType.InspectOffer, JsonConvert.SerializeObject(_id));
        }
        //async Task<>
        internal static ItemOffer IdLoad(Guid id)
        {
            //return await AMYPrototype.Program.data.database.LoadRecordAsync("Offer",
            //    PMongoDB.FilterEqual<ItemOffer, Guid>("_id", id));
            return AMYPrototype.Program.data.database.LoadRecord("Offer",
                MongoDatabase.FilterEqual<ItemOffer, Guid>("_id", id));
        }
        internal static async Task IdDelete(Guid id)
        {
            await AMYPrototype.Program.data.database.DeleteRecord<ItemOffer, Guid>("Offer", id);
        }
        internal async Task DeleteAsync()
        {
            await AMYPrototype.Program.data.database.DeleteRecord<ItemOffer, Guid>("Offer", _id);
        }
        internal string ToInfo(bool itemDescription)
        {
            string r = $"From <@{sender}> to <@{receiver}> At {whenSent.ToLongDateString()}" + Environment.NewLine
                + $"{offer} For {pricePer} coins, Total: {pricePer} Kutsyei Coins" + Environment.NewLine
                + $"Note: `{note}`" + Environment.NewLine;
            if(itemDescription)
                r += offer.item.StatsInfo();
            return r;
        }
        internal static async Task<ItemOffer[]> GetOffers(ulong r)
        {
            return (await AMYPrototype.Program.data.database.LoadRecordsAsync("Offer",
                MongoDatabase.FilterEqual<ItemOffer, ulong>("receiver", r))).ToArray();
        }
        internal static async Task<ItemOffer[]> SentOffers(ulong s)
        {
            return (await AMYPrototype.Program.data.database.LoadRecordsAsync("Offer",
                MongoDatabase.FilterEqual<ItemOffer, ulong>("sender", s))).ToArray();
        }
    }
}
