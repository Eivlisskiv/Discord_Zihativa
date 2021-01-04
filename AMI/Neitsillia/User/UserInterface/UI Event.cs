using AMI.AMIData.Events;
using AMI.Methods;
using AMI.Neitsillia.Collections;
using Discord;
using Discord.WebSocket;
using Neitsillia.Items.Item;
using System;
using System.Threading.Tasks;

namespace AMI.Neitsillia.User.UserInterface
{
    partial class UI
    {
        static partial void InitialiseOptionDelegates()
        {
            OptionsLoad.Add(MsgType.Event, ui =>
            {
                ui.options = new System.Collections.Generic.List<string>();

                if (OngoingEvent.Ongoing.eventinfo.shop != null)
                    ui.options.Add(EUI.trade);

            });

            OptionsLoad.Add(MsgType.EventShop, ui =>
            {
                ui.options = new System.Collections.Generic.List<string>()
                {
                    EUI.uturn
                };

                if (ui.data == null)
                    for (int i = 0; i < OngoingEvent.Ongoing.eventinfo.shop.stock.Length; i++)
                        ui.options.Add(EUI.GetNum(i + 1));
                else ui.options.AddRange(new[] { EUI.GetLetter(8), EUI.GetLetter(21), EUI.GetLetter(23), EUI.GetLetter(12) });

            });
        }

        public async Task Event(SocketReaction reaction, IUserMessage msg)
        {
            switch (reaction.Emote.ToString())
            {
                case EUI.trade:
                    await OngoingEvent.Ongoing.OpenShop(player, msg.Channel, -1, true);
                    break;
            }
        }

        public async Task EventShop(SocketReaction reaction, IUserMessage msg)
        {
            string emote = reaction.Emote.ToString();
            if (emote == EUI.uturn)
            {
                if (data != null) await OngoingEvent.Ongoing.OpenShop(player, msg.Channel);

                else await player.NewUI(await msg.Channel.SendMessageAsync(
                    embed: OngoingEvent.Ongoing.EmbedInfo()), MsgType.Event);

                return;
            }

            int i = data != null ? int.Parse(data) : EUI.GetNum(emote) - 1;

            if (data == null) await OngoingEvent.Ongoing.OpenShop(player, msg.Channel, i, true);
            else if (i >= 0 && i < OngoingEvent.Ongoing.eventinfo.shop.stock.Length) //Is buying an item by count
            {
                //Drop from start 1 to index
                int amount = 0;
                var shop = OngoingEvent.Ongoing.eventinfo.shop;
                int currency = player.Currency.Get(shop.currency);

                if (emote == EUI.GetLetter(8)) amount = 1;
                else if (emote == EUI.GetLetter(21)) amount = 5;
                else if (emote == EUI.GetLetter(23)) amount = 10;
                else if (emote == EUI.GetLetter(12)) amount = NumbersM.FloorParse<int>(
                    ((double)currency) / shop.stock[i].count);

                if (currency < shop.stock[i].count * amount)
                    await msg.Channel.SendMessageAsync($"Missing {shop.stock[i].count * amount - currency} {shop.currency} for this purchase");
                else
                {
                    string collected = "";
                    if (shop.Stackable(i))
                    {
                        StackedItems item = new StackedItems(shop.ParseItem(i, player.level), amount);
                        item.item.Scale(player.level);
                        if (player.CollectItem(item, true))
                        {
                            int price = shop.stock[i].count * amount;
                            collected += $"Bought {item} for {price} {shop.currency}";
                            player.Currency.Mod(shop.currency, -price);
                        }
                        else collected = $"Inventory may not contain {item}";
                    }
                    else
                    {
                        string warning = null;
                        collected = null;
                        for (int k = 0; k < amount; k++)
                        {
                            Item item = shop.ParseItem(i, player.level);
                            item.Scale(player.level);
                            if (player.CollectItem(item, 1, true))
                                collected += $"Bought {item} for {shop.stock[i].count} {shop.currency}" + Environment.NewLine;
                            else
                            {
                                warning = $"Inventory full! Bought {k}/{amount} items";
                                amount = k;
                            }
                        }

                        if (collected != null) collected = $"```{collected}```" + warning;
                        else if (warning != null) collected = warning;

                        if(amount > 0)
                            player.Currency.Mod(shop.currency, -(amount * shop.stock[i].count));
                    }

                    if(collected != null) await msg.Channel.SendMessageAsync(collected);
                    await OngoingEvent.Ongoing.OpenShop(player, msg.Channel, i, true);
                }
            }

        }
    }
}
