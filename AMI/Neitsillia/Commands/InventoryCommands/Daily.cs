using AMI.Methods;
using AMI.Neitsillia.Collections;
using AMI.Neitsillia.User;
using AMI.Neitsillia.User.PlayerPartials;
using AMYPrototype;
using AMYPrototype.Commands;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Neitsillia.Items.Item;
using System;
using System.Threading.Tasks;

namespace AMI.Neitsillia.InventoryCommands
{
    public class Daily : ModuleBase<AMI.Commands.CustomSocketCommandContext>
    {
        [Command("Claim Crate")] [Alias("claimcrate", "claim vote")]
        [Summary("Due to the automatic vote rewarding system breaking, using this command every 12h will check if you've voted in the last 12 hours.")]
        public async Task ClaimCrate()
        {
            try
            {
                if (Program.dblAPI?.DblApiAuth == null)
                    await ReplyAsync("Currently not connected to top.gg API. Please try again later");
                else if (await Program.dblAPI.DblApiAuth.HasVoted(Context.User.Id))
                {
                    if (Context.BotUser.LastVote.AddHours(12) < DateTime.UtcNow)
                    {
                        await Context.BotUser.NewVote();
                        Context.BotUser.Save();
                    }
                    else await ReplyAsync("You've already claimed your crate for the last 12h.");
                }
                else await ReplyAsync("You have not yet voted in the last 12 hours or the vote has not yet gone through."
                    + Environment.NewLine + Program.dblAPI?.WebsiteUrl);

            }catch(Exception)
            {
                await ReplyAsync("Currently not connected to top.gg API. Please try again later");
            }
        }
        [Command("Daily")]
        public async Task DailyLoot() => await DailyRCheck(Context.Channel);
        static StackedItems GetDailyScale(int playerLevel)
        {
            Item item = Item.LoadItem("Healing Herb");
            int count = 40;
            if(playerLevel <= 3)
            {
                Item.LoadItem("Healing Herb");
                count = 40;
            }
            else if (playerLevel > 3 && playerLevel <= 8)
                count *= playerLevel;
            else if (playerLevel <= 8)
            {
                item = Item.LoadItem("Vhochait");
                count = playerLevel;
            }
            else if (playerLevel <= 15)
            {
                item = Item.LoadItem("Vhochait");
                count = playerLevel * 2;
            }
            else
            {
                item = Item.LoadItem("Vhochait");
                count = 30;
            }
            return new StackedItems(item, count);
        }
        public async Task DailyRCheck(ISocketMessageChannel chan)
        {
            string message = null;
            Player player = Player.Load(Context.BotUser);
            
            if (player.userTimers.dailyLoot <= DateTime.UtcNow || 
                (player.userTimers.dailyLoot.Day == DateTime.UtcNow.Day))
            {
                EmbedBuilder loot = DailyReward(player);
                if (loot.Footer != null)
                {
                    message = loot.Footer.Text;
                    loot.Footer.Text = " ";
                }
                DUtils.DeleteMessage(await chan.SendMessageAsync(message, embed: loot.Build()));
                await DUtils.DeleteContextMessageAsync(Context);

                player.QuestTrigger(Neitsillia.Items.Quests.Quest.QuestTrigger.CollectingDaily);
            }
            else
                DUtils.DeleteMessage(await chan.SendMessageAsync($"You greedy mortal, you must wait " +
                    $"{Timers.CoolDownToString(player.userTimers.dailyLoot)}."));
        }
        EmbedBuilder DailyReward(Player player)
        {
            int coinsgain = ReferenceData.dailyCoinsRates + (player.level * 3);
            string itemsLoot = null;
            string temp = null;
            //
            StackedItems dailyHealthItem = GetDailyScale(player.level);
            if (!player.CollectItem(dailyHealthItem))
            {
                long kgain = dailyHealthItem.item.baseValue * dailyHealthItem.count;
                player.KCoins += (kgain);
                temp += "+ " + (kgain) + " Kutsyei coins";
            }
            else
                temp = dailyHealthItem.ToString();
            itemsLoot += temp + Environment.NewLine;
            //
            StackedItems dailyressource = new StackedItems(Item.RandomItem(player.level,
                Program.rng.Next(3)), 1);

            if (dailyressource.item.tier < player.level * 1.5)
                dailyressource.count = Verify.MinMax(NumbersM.CeilParse<int>((player.level /5.00) - dailyressource.item.tier), 10, 1);
            if (!player.CollectItem(dailyressource))
            {
                player.KCoins += dailyressource.item.baseValue * dailyressource.count;
                temp = "+ " + (dailyressource.item.baseValue * dailyressource.count) + "Kutsyei coins";
            }
            else temp = dailyressource.ToString();
            itemsLoot += temp + Environment.NewLine;
            //
            int tier = Program.RandomInterval(player.level, 0.15);
            itemsLoot += GetDailyGear(player, tier * 5) + Environment.NewLine;

            if(player.level < 9)
                itemsLoot += GetDailyGear(player, 20, (int)Item.IType.Weapon) + Environment.NewLine;

            EmbedBuilder loot = new EmbedBuilder();
            //
            if (Program.dblAPI.connected && false)
                loot.WithFooter("Vote on DiscordBot List to receive and open Crates for more rewards. " +
                    Program.dblAPI.WebsiteUrl);

            player.KCoins += coinsgain;
            //

            loot.WithTitle("Daily Rations");
            player.UserEmbedColor(loot);
            loot.AddField("Kutsyei Coins", $"+{coinsgain}~~K~~");
            if (itemsLoot == null)
                itemsLoot = "None";
            loot.AddField("Items", itemsLoot);
            //
            player.userTimers.dailyLoot = DateTime.UtcNow.AddDays(1);
            player.SaveFileMongo();
            //
            return loot;
        }
        string GetDailyGear(Player player, int tier, int type = -1)
        {
            if (type < 0) type = Program.rng.Next(6, 12);
            Item dailyGear = Item.RandomItem(tier, type);
            if (!player.CollectItem(dailyGear, 1))
            {
                player.KCoins += dailyGear.baseValue;
                return "+ " + dailyGear.baseValue + " Kuts coins";
            }
            else return "1x " + dailyGear;
        }
    }
}
