using AMI.Module;
using AMI.Neitsillia.Gambling.Games;
using AMI.Neitsillia.User;
using AMI.Neitsillia.User.PlayerPartials;
using AMI.Neitsillia.User.UserInterface;
using AMYPrototype;
using AMYPrototype.Commands;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;

namespace AMI.Neitsillia.NeitsilliaCommands
{
    public class GamblingCommands : ModuleBase<AMI.Commands.CustomSocketCommandContext>
    {
        static Random rng = Program.rng;

        [Command("Lottery")]
        public async Task LotteryInfo()
        {
            if (Program.data.lottery != null)
            {
                Player player = Context.Player;
                var lottery = Program.data.lottery;
                EmbedBuilder info = lottery.Info();
                player.UserEmbedColor(info);
                info.Description += Environment.NewLine;
                bool createUi = true;
                if (lottery.entries.Contains($"{player.userid}\\{player.name}"))
                {
                    info.Description += "This character already has a ticket";
                    createUi = false;
                }
                else if (player.KCoins < lottery.ticketValue)
                {
                    info.Description += "This character does not have enough coins to purchase " +
                          "a ticket";
                    createUi = false;
                }
                else info.Description += $"React with {EUI.ticket} to purchase a ticket.";
                IUserMessage msg = await ReplyAsync(embed: info.Build());
                if (createUi)
                    await player.NewUI(msg, MsgType.Lottery);
            }
            else await ReplyAsync("No Lottery currently available.");
            await AMYPrototype.Commands.DUtils.DeleteContextMessageAsync(Context);
        }
        [Command("_GameTest")]
        public async Task CardGamesShortcut()
        {
            Context.WIPCheck();

            await GamblingGame.SelectInitialBet(Context.Player, Context.Channel, "Blackjack");
        }

        internal static async Task TavernGames(Player player, IMessageChannel chan)
        {
            EmbedBuilder games = DUtils.BuildEmbed("Tavern Games", "Hello, Traveler. Would you like a gambling table?", null, player.userSettings.Color(), 
                DUtils.NewField("Dice Games", $"{EUI.Dice(1)} Even Odd"), 
                DUtils.NewField("Card Games", $"{EUI.GetNum(0)} Blackjack") 
                );
            await player.EditUI(null, games.Build(), chan, MsgType.GamblingGames, "Tavern");
        }

        internal static async Task DiceGame_EvenOdd(Player player, ISocketMessageChannel chan, int bet = -1, int coins = 10, int streak = 0)
        {
            if (player.Area.type != Areas.AreaType.Tavern) throw NeitsilliaError.ReplyError("You are no longer in a Tavern. You must be in a tavern to play these games.");
            if (player.KCoins < coins) throw NeitsilliaError.ReplyError("You don't have the funds to make this bet.");

            EmbedBuilder embed = DUtils.BuildEmbed("Dice Game : Even Odds", "Bet your kuts on the dice's result being an even number or an odd number.", null, player.userSettings.Color());

            if (bet > 0 && player.ui?.type == MsgType.DiceGame)
            {
                int roll = rng.Next(6) + 1;
                bool even = roll % 2 == 0;
                embed.AddField("Bet Placed", $"Bet: {(bet == 2 ? "Even" : "Odd")} {Environment.NewLine} Dice Roll: {roll} {Environment.NewLine} Win Streak : {streak} ");
                string result = null;
                if((bet == 2 && even) || (bet == 1 && !even))
                {
                    int reward = coins;
                    result = $"You won your bet! +{coins} Kuts";
                    if (streak > 0)
                    {
                        result += Environment.NewLine + $"With a bonus {streak * coins} Kuts for your winning streak!";
                        reward += streak * coins;
                    }
                    player.KCoins += reward;
                    streak++;
                }
                else
                {
                    int reward = coins;
                    result = $"You lost your bet! -{coins} Kuts";
                    if (streak > 0)
                        result += Environment.NewLine + $"You lost your {streak} winning streak! :(";
                    player.KCoins -= coins;
                    streak = 0;
                }

                embed.AddField(DUtils.NewField("Bet Result", result));
            }

            embed.AddField(DUtils.NewField("**Place Your Bet**", $"Current betting amount: {coins} Kuts"
                + Environment.NewLine + $"{EUI.two} : Multiply bet by 2"
                + Environment.NewLine + $"{EUI.five} : Multiply bet by 5"
                + Environment.NewLine + $"{EUI.zero} : Multiply bet by 10"
                + Environment.NewLine + $"{EUI.prev} : Reduce bet by 10 kuts"
                + Environment.NewLine + $"{EUI.next} : Increase bet by 10 kuts"
                + Environment.NewLine + $"{EUI.Dice(1)} : Bet on **Odd**"
                + Environment.NewLine + $"{EUI.Dice(2)} : Bet on **Even**"
                ));
            await player.EditUI("Place your bets, Traveler.", embed.Build(), chan, MsgType.DiceGame, $"{coins};{streak}");
        }
    }
}
