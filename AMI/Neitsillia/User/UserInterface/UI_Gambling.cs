using AMI.Methods;
using AMI.Module;
using AMI.Neitsillia.Gambling.Games;
using AMI.Neitsillia.NeitsilliaCommands;
using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AMI.Neitsillia.User.UserInterface
{
    public partial class UI
    {
        private static void InitO_Gambling()
        {
            OptionsLoad.Add(MsgType.GamblingGames, ui =>
            {
                ui.options = new List<string>();
                switch (ui.data)
                {
                    case "Tavern":
                        ui.options.Add(EUI.Dice(1));
                        ui.options.Add(EUI.GetNum(0));
                        break;
                }
            });

            OptionsLoad.Add(MsgType.DiceGame, ui =>
                ui.options = new List<string>()
                {
                    EUI.prev,
                    EUI.Dice(1),
                    EUI.Dice(2),
                    EUI.next,
                    EUI.two, EUI.five, EUI.zero,
                    EUI.cancel
                });

            OptionsLoad.Add(MsgType.GameBet, ui =>
                ui.options = new List<string>()
                {
                    EUI.prev,
                    EUI.next,
                    EUI.two, EUI.five, EUI.zero,
                    EUI.ok,
                });

            OptionsLoad.Add(MsgType.CardGame, ui =>
            {
                Type type = GamblingGame.GetGameType(ui.data);
                Dictionary<string, string> actions = Utils.GetVar
                    <Dictionary<string, string>>(type, "Actions", true);
                ui.options = new List<string>(actions.Keys);
            });
        }

        public async Task GamblingGames(SocketReaction reaction, IUserMessage msg)
        {
            switch (data)
            {
                case "Tavern":
                    {
                        string s = reaction.Emote.ToString();
                        int i = EUI.Dice(s);
                        if (i > -1)
                        {
                            switch (i)
                            {
                                case 1: await GamblingCommands.DiceGame_EvenOdd(player, Channel); break;
                            }
                        }
                        else
                        {
                            i = EUI.GetNum(s);
                            string[] games =
                            {
                                "Blackjack",
                            };
                            if (i > games.Length) return;

                            await GamblingGame.SelectInitialBet(player, Channel, games[i]); break;
                        }

                    }
                    break;
            }
        }

        public async Task GameBet(SocketReaction reaction, IUserMessage _)
        {
            //"{name};{bet amount};{has agreed}"
            string[] d = data.Split(';');
            int bet = int.Parse(d[1]);

            switch (reaction.Emote.ToString())
            {
                case EUI.prev: bet -= 10; break;
                case EUI.next: bet += 10; break;

                case EUI.two: bet *= 2; break;
                case EUI.five: bet *= 5; break;
                case EUI.zero: bet *= 10; break;

                case EUI.ok:
                    if (player.IsSolo)
                    {
                        await GamblingGame.Initialise(
                            player, d[0], bet, Channel);
                        return;
                    }
                    bool accepted = false;
                    if(d.Length == 3) bool.TryParse(d[2], out accepted);

                    await GamblingGame.ConfirmInitialBet(
                        player, d[0], bet, accepted, Channel);
                    return;
            }

            await GamblingGame.SelectInitialBet(player, Channel, d[0], bet);
        }

        public async Task DiceGame(SocketReaction reaction, IUserMessage _)
        {
            string[] d = data.Split(';');
            //$"{coins};{streak}
            int.TryParse(d[0], out int coins);
            int.TryParse(d[1], out int streak);

            int i = EUI.Dice(reaction.Emote.ToString());
            if (i != -1)
                await GamblingCommands.DiceGame_EvenOdd(player, Channel, i, coins, streak);
            switch (reaction.Emote.ToString())
            {
                case EUI.cancel: await GameCommands.ShortStatsDisplay(player, Channel); break;

                case EUI.prev: await GamblingCommands.DiceGame_EvenOdd(player, Channel, -1, Math.Max(coins - 10, 10), streak); break;
                case EUI.next: await GamblingCommands.DiceGame_EvenOdd(player, Channel, -1, coins + 10, streak); break;

                case EUI.two: await GamblingCommands.DiceGame_EvenOdd(player, Channel, -1, coins * 2, streak); break;
                case EUI.five: await GamblingCommands.DiceGame_EvenOdd(player, Channel, -1, coins * 5, streak); break;
                case EUI.zero: await GamblingCommands.DiceGame_EvenOdd(player, Channel, -1, coins * 10, streak); break;
            }
        }
        public async Task CardGame(SocketReaction reaction, IUserMessage msg)
        {
            if (reaction.Emote.ToString() == EUI.cancel)
            {
                await QuitCardGame(reaction);
                return;
            }

            if (player.GamblingHand.turn != null)
            {
                await Channel.SendMessageAsync(
                    $"You've already played your turn for this round: {player.GamblingHand.turn}");
                return;
            }

            Type type = GamblingGame.GetGameType(data);
            Dictionary<string, string> actions = Utils.GetVar<Dictionary<string, string>>(type, "Actions", true);

            if (actions == null) return;

            actions.TryGetValue(reaction.Emote.ToString(), out string action);
            if (action != null)
            {
                IGamblingGame game = GamblingGame.CreateInstance(type, player);
                game.Action(action);

                player.GamblingHand.turn = action;
                player.GamblingHandKey.Save();

                await game.EndTurn();
            }
        }

        private async Task QuitCardGame(SocketReaction reaction)
        {
            var deck = Gambling.Cards.Deck.Load(player);
            if (deck?.house != null)
            {
                deck.house.bet += player.GamblingHand.bet;
                deck.Save();
            }
            await player.GamblingHandKey.Delete();
            player.SaveFileMongo();
            await Channel.SendMessageAsync("You quit the game");
        }
    }
}
