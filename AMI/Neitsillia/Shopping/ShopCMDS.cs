using AMI.Commands;
using AMI.Methods;
using AMI.Neitsillia;
using AMI.Neitsillia.Collections;
using AMI.Neitsillia.NPCSystems;
using AMI.Neitsillia.Shopping;
using AMI.Neitsillia.User;
using AMI.Neitsillia.User.PlayerPartials;
using AMI.Neitsillia.User.UserInterface;
using AMYPrototype.Commands;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Neitsillia.Items.Item;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AMI.Module
{
    public class ShopCommands : ModuleBase<CustomSocketCommandContext>
    {
        public static EmbedBuilder AllShops(EmbedBuilder embed)
        {
            EmbedBuilder allshops = embed;
            allshops.WithTitle("Trading");
            allshops.WithDescription("Trading is currently only available with NPCs, you must be in an interaction with an NPC to trade.");
            allshops.AddField("Commands", "```" +
                "~Trade |> Attempts to displays the NPC's inventory for trade"+ Environment.NewLine +   
                "~ItemInfo ''ItemName'' |> Displays overall information on the item, search is with name" + Environment.NewLine +
                "~Buy #slot #amount |> Attempts to purchase #amount of the item sloted #slot of the NPC" + Environment.NewLine +
                "~IInfo #slot |> Displays specific information of the sloted #slot item in the player's inventory" + Environment.NewLine +
                "~Sell #slot #amount |>Attempt to sell #amount of the sloted #slot item in the player's inventory to the NPC" + "```");
            allshops.WithFooter("parameters with default values are optional and will use the default value when not indicated by user." + Environment.NewLine +
                "Any string value with spaces must be surrounded by quotes: " + '"' + "value" + '"');
            return allshops;
        }
        [Command("Trade")]
        public async Task ViewNPCInv(int viewType = 1)
        {
            await ViewNPCInv(Context.Player, Context.Channel, viewType, false);
        }
        internal static async Task ViewNPCInv(Player player, ISocketMessageChannel chan, int page, bool edit)
        {
            if (player.Encounter != null && player.Encounter.IsNPC())
            {
                NPC n = player.Encounter.npc;
                page = Verify.MinMax(page, (n.inventory.Count/15) + 1, 1);
                if (n.profession == ReferenceData.Profession.Creature)
                    await chan.SendMessageAsync("You may not trade with a creature");
                else if (n.inventory != null && n.inventory.Count > 0)
                {
                    string itemList = null;
                    for (int i = ((page - 1) * 15); i < n.inventory.Count && i < (page*15); i++)
                    {
                        var inst = n.inventory.inv[i];
                        itemList += $" {i+1}|`{inst}`{inst.item.CompareTo(player.equipment)}| " +
                            $"{GetPrice(inst.item.GetValue(),n.stats.PriceMod(),player.stats.PriceMod(), -1)}" +
                            $"~~K~~" + Environment.NewLine;
                    }

                    EmbedBuilder embed = new EmbedBuilder();
                    embed.WithTitle($"{n.name}'s Inventory");
                    embed.WithDescription(itemList);

                    await player.EnUI(edit, null, embed.Build(), chan, MsgType.NPCInv, page.ToString());
                }
                else await chan.SendMessageAsync($"```{Dialog.GetDialog(n, Dialog.nothingToSell)}```");
            }
            else await chan.SendMessageAsync("You are not in an interaction.");
        }

        [Command("InspectNpcItem")]
        [Alias("ininfo")]
        public async Task InspectNpcItem(int npcInventorySlot)
        {
            int index = npcInventorySlot - 1;
            Player player = Context.Player;
            if (!player.IsEncounter(Neitsillia.Encounters.Encounter.Names.NPC))
                await DUtils.Replydb(Context, "You are not in an interaction.");
            else
            {
                NPC n = player.Encounter.npc;
                Item item = null;
                if (n.profession == ReferenceData.Profession.Creature)
                    await DUtils.Replydb(Context, "You may not trade with a creature");
                else if ((item = n.inventory.GetItem(index)) == null)
                    await DUtils.Replydb(Context, "Item invalid");
                else
                    await ReplyAsync(embed: item.EmdebInfo(player.UserEmbedColor()).Build());
            }
        }

        [Command("Buy")][Alias("BulkBuy")]
        [Summary("Buy item(s) from the NPC you are currently interacting with, use `~trade` to view NPC's inventory. ex: `~Buy 1 5x10` " +
            "will purchase 1 of the items in the slot 1 and 10 of the item in the slot 5 of the npc's inventory.")]
        public async Task BuyItem(params string[] indexXamount)
        {
            Player player = Context.Player;

            if (!player.IsEncounter("npc"))
                await ReplyAsync("You must be in an interaction with a n NPC to Buy. Find NPCs by exploring in towns");
            else
            {
                PendingTransaction transaction = new PendingTransaction(
                player, indexXamount, PendingTransaction.Transaction.Buy);

                await transaction.SendTransaction(player, Context.Channel);
            }
        }
        
        [Command("Sell")][Alias("BulkSell")]
        [Summary("Sell item(s) to the npc")]
        public async Task SellItem(params string[] indexXamount)
        {
            Player player = Context.Player;
            if (!player.IsEncounter("npc"))
                await ReplyAsync("You must be in an interaction with a n NPC to Sell. Find NPCs by exploring in towns");
            else
            {
                PendingTransaction transaction = new PendingTransaction(
                player, indexXamount, PendingTransaction.Transaction.Sell);

                await transaction.SendTransaction(player, Context.Channel);
            }
        }
        string BulkSell(Player player, List<int[]> i)
        {
            string grandResult = null;
            NPC n = player.Encounter.npc;
            foreach (int[] d in i)
            {
                Item item = player.inventory.GetItem(d[0]);
                if (item != null)
                {
                    int amount = Verify.MinMax(d[1], player.inventory.GetCount(d[0]), 1);
                    int price = GetPrice(item.GetValue(), n.stats.PriceMod(), player.stats.PriceMod(), 1);
                    if (n.KCoins < price * amount)
                    {
                        grandResult += "Insufficient funds to continue purchases";
                        break;
                    }
                    else
                    {
                        n.AddItemToInv(item, amount);
                        n.KCoins -= price * amount;
                        player.KCoins += price * amount;
                        player.inventory.Remove(d[0], amount);
                        grandResult += $"Sold {amount} {item.name} for {price * amount} ~~K~~";
                    }
                    grandResult += Environment.NewLine;
                }
            }
            n.GetsAProfession(ReferenceData.Profession.Merchant);
            return grandResult;
        }
        //
        List<(int index, int amount)> BulkShopAction(string[] args, out int invalids)
        {
            invalids = 0;
            Player player = Context.Player;
            if (player.IsEncounter(Neitsillia.Encounters.Encounter.Names.NPC))
            {
                List<(int index, int amount)> bulks = new List<(int index, int amount)>();
                foreach (string s in args)
                {
                    try
                    {
                        var ia = Verify.IndexXAmount(s);
                        ia.index--;
                        if (!bulks.Exists(x => x.index == ia.index))
                        bulks.Add(ia);
                    }
                    catch (Exception) { invalids++; }
                }
                if (bulks.Count > 0)
                {
                    bulks.Sort((x, y) => y.index.CompareTo(x.index));
                    return bulks;
                }
            }
            return null;
        }

        [Command("Recruit")]
        public async Task RecruitNPC()
        {
            Player player = Player.Load(Context.BotUser);
            if (!player.IsEncounter("NPC"))
                await DUtils.DeleteBothMsg(Context, await ReplyAsync($"{player.name} is not in an encounter with an NPC"));
            else if (player.Party == null)
                await DUtils.DeleteBothMsg(Context, await ReplyAsync($"{player.name} must be in a Party to recruit an NPC"));
            else if(player.Party.UpdateFollower(player.Encounter.npc))
                await DUtils.DeleteBothMsg(Context, await ReplyAsync($"This NPC is already in your party."));
            else if (player.Encounter.npc.profession == ReferenceData.Profession.Peasant)
                await DUtils.DeleteBothMsg(Context, await ReplyAsync($"```{Dialog.GetDialog(player.Encounter.npc, Dialog.peasantRecruit)}```"));
            else if (player.Party.MemberCount >= 4)
                await DUtils.DeleteBothMsg(Context, await ReplyAsync($"{player.name}'s party is full."));
            else
            {

                NPC n = player.Encounter.npc;
                if (player.level - player.stats.GetCHA() > n.level && player.userid != 201875246091993088)
                    await DUtils.DeleteBothMsg(Context, await ReplyAsync(
                        $"```{Dialog.GetDialog(n, Dialog.weakRecruit)}```"));
                else
                {
                    long cost = NumbersM.FloorParse<long>(((player.level + player.Rank() + n.level + n.Rank()) * 39)
                        * (1.00 - (Stats.recruitPricePerCha * player.stats.GetCHA())));
                
                    if (player.KCoins < cost)
                        await DUtils.DeleteBothMsg(Context, await ReplyAsync(
                            $"{player.name} does not have the required coins to recruit {n.displayName}. " +
                            $"Missing {cost - player.KCoins} from total {cost}"));
                    else
                        await ComfirmRecruitNPC(player, cost, Context.Channel);
                }
            }
        }
        async Task ComfirmRecruitNPC(Player player, long cost, ISocketMessageChannel chan)
        {
            NPC n = player.Encounter.npc;
            player.KCoins -= cost;
            n.KCoins += cost;
            player.Party.NPCMembers.Add(n);
            player.EncounterKey.Delete().Wait();
            player.QuestTrigger(Neitsillia.Items.Quests.Quest.QuestTrigger.RecruitNPC);
            await player.PartyKey.SaveAsync();
            await chan.SendMessageAsync($"{n.displayName} was recruited to {player.Party.partyName} for {cost} coins.");
        }
        [Command("NPC")]
        public async Task EncounterNPCInfo()
        {
            Player player = Player.Load(Context.BotUser, Player.IgnoreException.Resting);
            if (!player.IsEncounter("NPC"))
                await DUtils.DeleteBothMsg(Context, await ReplyAsync("No NPCs to inspect."));
            else
            {
                NPC n = player.Encounter.npc;
                EmbedBuilder em = new EmbedBuilder();
                await ReplyAsync(embed: n.NPCInfo(player.UserEmbedColor(), true, false, false, false).Build());
            }
        }
        /// <summary>
        /// Gets the item's price by comparing both party's charisma with a maximum of 60%
        /// in price difference
        /// </summary>
        /// <param name="item">the item</param>
        /// <param name="npc">the npc</param>
        /// <param name="player">the player</param>
        /// <param name="dir"> -1 if player buying, +1 if player selling</param>
        /// <returns></returns>
        internal static int GetPrice(long itemValue, double npcMod, double playerMod, int dir)
        {
            dir = Verify.MinMax(dir, 1, -1);
            double mod = 1 + (dir * (playerMod -
                (npcMod + 2)));
            if(dir < 0)
                mod = Verify.Min(mod, 0.90);
            else
                mod = Verify.MinMax(mod, 0.90, 0.30);
            int final = Verify.Min(Convert.ToInt32(itemValue * mod), 1);
            return final;
        }


    }
}
