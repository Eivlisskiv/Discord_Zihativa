using AMI.AMIData;
using AMI.Methods;
using AMI.Module;
using AMI.Neitsillia.Collections;
using AMI.Neitsillia.NPCSystems;
using AMI.Neitsillia.User.PlayerPartials;
using AMI.Neitsillia.User.UserInterface;
using AMYPrototype;
using AMYPrototype.Commands;
using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AMI.Neitsillia.Shopping
{
    class PendingTransaction
    {
        public static MongoDatabase Database => Program.data.database;

        public enum Transaction { Buy = -1, Sell = 1 };

        internal static async Task<string> Accept(Player player, string data)
        {
            NPC npc = player.Encounter.npc;
            PendingTransaction transaction;
            if (data.EndsWith(";"))//Selling
            {
                string[] selection = data.TrimEnd(';').Split(';');
                transaction = new PendingTransaction(player, selection, Transaction.Sell, true);

                int count = transaction.Count;
                for (int i = 0; i < count; i++)
                {
                    StackedItems si = transaction.Get(i, true);

                    long price = ShopCommands.GetPrice(si.item.GetValue(),
                        npc.stats.PriceMod(), player.stats.PriceMod(),
                        (int)transaction.transation) * si.count;

                    transaction.TotalPrice += price;

                    npc.inventory.Add(si, -1);
                }

                player.KCoins += transaction.TotalPrice;
                npc.KCoins -= transaction.TotalPrice;

            }
            else //Buying
            {
                transaction = Database.LoadRecord(null,
                MongoDatabase.FilterEqual<PendingTransaction, Guid>("_id", Guid.Parse(data)));

                if (transaction == null) return "Transaction no longer available";

                player.KCoins -= transaction.TotalPrice;
                npc.KCoins += transaction.TotalPrice;

                int invSize = player.InventorySize();
                foreach (var si in transaction.items)
                    if (!player.inventory.Add(si, invSize))
                    {
                        transaction.Delete();
                        throw NeitsilliaError.ReplyError("Inventory can not contain all items in this order. Transaction canceled.");
                    }

            }

            await player.ui.TryDeleteMessage();
            player.ui = null;
            player.SaveFileMongo();

            return $"```{Dialog.GetDialog(npc, Dialog.tradingBusiness)}```";
        }

        internal static string Cancel(Player player, string data)
        {
            if (data == null) return null;

            if (data != null && data.EndsWith(";"))
            {
                _ = player.ui?.TryDeleteMessage();
                player.ui = null;
                return (player.Encounter?.npc != null ?
                $"```{Dialog.GetDialog(player.Encounter.npc, Dialog.offerCancelled)}```" :
                "Transaction canceled");
            }

            PendingTransaction transaction = Database.LoadRecord(null, 
                MongoDatabase.FilterEqual<PendingTransaction, Guid>("_id", Guid.Parse(data)));

            if (transaction == null)
            {
                _ = player.ui?.TryDeleteMessage();
                player.ui = null;
                return "Transaction not found";
            }

            foreach (var si in transaction.items)
                player.Encounter.npc.inventory.Add(si, -1);

            player.Encounter.Save();

            transaction?.Delete();
            _ = player.ui?.TryDeleteMessage();

            return (player.Encounter?.npc != null ?
                $"```{Dialog.GetDialog(player.Encounter.npc, Dialog.offerCancelled)}```" :
                "Transaction canceled");
        }

        public Guid _id;
        public Transaction transation;
        public List<StackedItems> items = new List<StackedItems>();
        
        public long TotalPrice;

        private Player player;
        private NPC npc;

        private int Count => selections.Count;

        private List<(int index, int amount)> selections;
        string selection;
        string errorList;

        public PendingTransaction(Player player, string[] inputs, Transaction t, bool complete = false)
        {
            transation = t;
            if(t == Transaction.Buy)
                _id = Guid.NewGuid();

            selections = new List<(int index, int amount)>();
            for(int i=0;i<inputs.Length;i++)
            {
                var data = Verify.IndexXAmount(inputs[i]);
                int index = 0;
                if (!complete)
                {
                    data.index--;
                    if (selections.Count > 1)
                        while (index < selections.Count &&
                              selections[index].index > data.index)
                            index++;
                }
                else index = i;
                selections.Insert(index, data);
            }

            //selections.Sort((x, y) => x.index.CompareTo(y.index));

            this.player = player;
            npc = player.Encounter.npc;
        }

        internal void Save()
        {
            Database.SaveRecord(null, this);
        }

        internal void Delete()
        {
            Database.DeleteRecord<PendingTransaction>(null, _id.ToString()).Wait();
        }

        private StackedItems Get(int i, bool complete = false)
        {
            int index = selections[i].index;
            int amount = selections[i].amount;

            switch (transation)
            {
                case Transaction.Buy:
                    {
                        if (index < 0 || index > npc.inventory.Count) return null;

                        StackedItems si = npc.inventory.Splice(index, amount);
                        items.Add(si);

                        return si;
                    }
                case Transaction.Sell:
                    {
                        if (index < 0 || index > player.inventory.Count) return null;

                        amount = Math.Min(player.inventory.GetCount(index), selections[i].amount);

                        selection += $"{index}x{amount};";
                        return player.inventory.Splice(index, amount, complete);
                    }
            }
            return null;
        }

        internal EmbedFieldBuilder[] GetEmbedFields()
        {
            List<EmbedFieldBuilder> fields = new List<EmbedFieldBuilder>();
            int count = Count;
            for(int i=0; i<count; i++)
            {
                StackedItems si = Get(i);
                if (si != null)
                {
                    long price = ShopCommands.GetPrice(si.item.GetValue(),
                        npc.stats.PriceMod(), player.stats.PriceMod(),
                        (int)transation) * si.count;

                    fields.Add(DUtils.NewField($"**{si.count}x {si.item.name} for {price} Kuts**",
                        si.item.StatsInfo(), true));
                    TotalPrice += price;
                }
            }
            return fields.ToArray();
        }

        internal Embed GetEmbed()
        {
            var fields = GetEmbedFields();

            if (fields.Length == 0) throw NeitsilliaError.ReplyError("Item selection is empty of invalid.");

            return DUtils.BuildEmbed(transation.ToString() + "ing",
                $"Total Price: {TotalPrice} {Environment.NewLine}" +
                $"Your wallet: {player.KCoins} => {player.KCoins + (TotalPrice * (int)transation)}", null, player.userSettings.Color(),
                fields).Build();
        }

        internal async Task SendTransaction(Player player, ISocketMessageChannel channel)
        {
            Embed embed = GetEmbed();

            switch (transation)
            {
                case Transaction.Buy:
                    {
                        if (player.KCoins >= TotalPrice)
                        {
                            Save();
                            string data = _id.ToString();
                            await player.NewUI(await channel.SendMessageAsync("Please confirm the purchase:",
                            embed: embed), MsgType.ConfirmTransaction, data);
                        }
                        else
                            await channel.SendMessageAsync($"**You do not have enough Kutsyei Coins.**",
                               embed: embed);
                    }break;
                case Transaction.Sell:
                    {
                        if (npc.KCoins >= TotalPrice)
                        {
                            await player.NewUI(await channel.SendMessageAsync("Please confirm the transaction:",
                            embed: embed), MsgType.ConfirmTransaction, selection);
                        }
                        else
                            await channel.SendMessageAsync($"```{Dialog.GetDialog(player.Encounter.npc, Dialog.notenoughK)}```",
                            embed: embed);
                    }break;
            }
        }

        
    }
}
