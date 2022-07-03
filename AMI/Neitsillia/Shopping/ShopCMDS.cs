using AMI.Commands;
using AMI.Methods;
using AMI.Neitsillia;
using AMI.Neitsillia.Collections;
using AMI.Neitsillia.NPCSystems;
using AMI.Neitsillia.Shopping;
using AMI.Neitsillia.User.PlayerPartials;
using AMI.Neitsillia.User.UserInterface;
using AMYPrototype.Commands;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using AMI.Neitsillia.Items.ItemPartials;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace AMI.Module
{
    [Name("Trading")]
    public class ShopCommands : ModuleBase<CustomCommandContext>
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
            => await ViewNPCInv(Context.Player, Context.Channel, viewType, false);

        internal static async Task ViewNPCInv(Player player, IMessageChannel chan, int page, bool edit)
        {
            if (player.Encounter != null && player.Encounter.IsNPC())
            {
                await ViewTradeInventory(player, chan, page, edit);
                return;
            }

            await chan.SendMessageAsync("You are not in an interaction.");
        }

        private static async Task ViewTradeInventory(Player player, IMessageChannel chan, int page, bool edit)
        {
            NPC n = player.Encounter.npc;
            page = Verify.MinMax(page, (n.inventory.Count / 15) + 1, 1);
            if (n.profession == ReferenceData.Profession.Creature)
            {
                await chan.SendMessageAsync("You may not trade with a creature");
                return;
            }
            else if (n.inventory != null && n.inventory.Count > 0)
            {
                EmbedBuilder embed = ListItems(player, page, n);

                await player.EnUI(edit, null, embed.Build(), chan, MsgType.NPCInv, page.ToString());
                return;
            }

            await chan.SendMessageAsync($"```{Dialog.GetDialog(n, Dialog.nothingToSell)}```");
            return;
        }

        private static EmbedBuilder ListItems(Player player, int page, NPC n)
        {
            List<string> itemList = new List<string>();
            string current = null;
            for (int i = ((page - 1) * 15); i < n.inventory.Count && i < (page * 15); i++)
            {
                StackedItems inst = n.inventory.inv[i];
                current += $"{i + 1}|{inst} {EUI.ItemType(inst.item.type)}{inst.item.CompareTo(player.equipment)}| " +
                    $"{GetPrice(inst.item.GetValue(), n.stats.PriceMod(), player.stats.PriceMod(), -1)}" +
                    $"Kuts" + Environment.NewLine;

                if (current.Length > 1000)
                {
                    itemList.Add(current);
                    current = null;
                }
            }

            itemList.Add(current);

            EmbedBuilder embed = new EmbedBuilder();
            embed.WithTitle($"{n.name}'s Inventory");
            embed.Fields = itemList.Select(s => DUtils.NewField("Items", s)).ToList();
            return embed;
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
                if ((player.level * 0.9) - player.stats.GetCHA() > n.level && player.userid != 201875246091993088)
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
        async Task ComfirmRecruitNPC(Player player, long cost, IMessageChannel chan)
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

        [Command("Npc Repair")]
        public async Task NPC_Repair(int item_slot)
        {
            Player player = Context.Player;
            if(player.Encounter.Name != Neitsillia.Encounters.Encounter.Names.NPC)
            {
                await ReplyAsync("You must be in an encounter with an npc to do this.");
                return;
            }

            item_slot--;
            Item item = player.inventory.GetItem(item_slot);
            if (!item.CanBeEquip())
            {
                await ReplyAsync($"{item} is not gear and may not be repaired.");
                return;
            }

            if (item.condition >= item.durability)
            {
                await ReplyAsync($"{item} does not need any repairs.");
                return;
            }

            NPC npc = player.Encounter.npc;
            if (npc.profession != ReferenceData.Profession.Blacksmith && !npc.schematics.Exists(s => s.name.Equals(item.originalName)))
            {
                await ReplyAsync($"{npc.name} may not repair a {item.originalName}.");
                return;
            }

            long price = GetCost(item, player, npc);
            if (player.KCoins < price)
            {
                await ReplyAsync($"You are missing {price - player.KCoins} Kutsyei Coins to have your {item} repaired by {npc.name}");
                return;
            }

            await player.NewUI($"Requesting item repair from {npc.name}", DUtils.BuildEmbed(
                $"Item Repair by {npc.name}", $"Item: {item} (Tier {item.tier})"
                + Environment.NewLine + $"Cost: {price} Coins", null, player.userSettings.Color
                ).Build(), Context.Channel, MsgType.NPCRepair, $"{item_slot}");

        }

        private static long GetCost(Item item, Player player, NPC npc) 
            => (item.tier * (100 - ((item.condition*100) / item.durability))) * Math.Max(1, npc.stats.GetCHA() - player.stats.GetCHA()) / 2;

        public static async Task ConfirmNPCRepair(Player player, int index, IMessageChannel chan)
        {
            Item item = player.inventory.GetItem(index);
            NPC npc = player.Encounter.npc;
            long price = GetCost(item, player, npc);

            player.KCoins -= price;
            item.condition = item.durability;

            npc.KCoins += price;

            player.SaveFileMongo();

            await chan.SendMessageAsync($"{npc.name} has repaired your {item} for {price}");
        }
    }
}
