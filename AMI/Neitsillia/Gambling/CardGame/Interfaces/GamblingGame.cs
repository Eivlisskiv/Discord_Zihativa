using AMI.Methods;
using AMI.Module;
using AMI.Neitsillia.Gambling.Cards;
using AMI.Neitsillia.NeitsilliaCommands;
using AMI.Neitsillia.User;
using AMI.Neitsillia.User.PlayerPartials;
using AMI.Neitsillia.User.UserInterface;
using AMYPrototype.Commands;
using Discord;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AMI.Neitsillia.Gambling.Games
{
    class GamblingGame
    {
        static AMIData.MongoDatabase Databse => AMYPrototype.Program.data.database;


        internal static Dictionary<string, string> gamesInfo = new Dictionary<string, string>()
        {
            {
                "Blackjack",
                "Attempt to reach a hand value of 21" +
                Environment.NewLine +" where aces are 11 or 1 (whichever is most convenient), " +
                "faces are worth 10 and all other cards are worth their respective numbers."
                + Environment.NewLine + "Getting over 21 will result in a bust: you lose."
            },
        };

        internal static async Task SelectInitialBet(Player player, IMessageChannel chan, string game, long bet = 10)
        {
            if (!player.IsLeader) throw NeitsilliaError.ReplyError("You must be party leader to start a group gambling game.");

            string description = null;

            gamesInfo.TryGetValue(game, out description);


            EmbedBuilder embed = DUtils.BuildEmbed(game, description, null, player.userSettings.Color(),
                DUtils.NewField("Initial Bet",
                $"Starting bet: {bet} Kuts {Environment.NewLine}" +
                $"{EUI.two} : Multiply bet by 2 {Environment.NewLine}" +
                $"{EUI.five} : Multiply bet by 5 {Environment.NewLine}" +
                $"{EUI.zero} : Multiply bet by 10 {Environment.NewLine}" +
                $"{EUI.prev} Reduce starting bet. {Environment.NewLine}" +
                $"{EUI.next} Increase starting bet. {Environment.NewLine}" +
                $"{EUI.ok} Start a game of {game} with this initial bet."
                ));

            await player.EditUI("", embed.Build(), chan, MsgType.CardGame, $"{game};{bet}");
        }

        internal static Type GetGameType(string name)
        {
            return Utils.GetTypesWithBaseClass(typeof(GamblingGame)).Find(c => c.Name == name);
        }

        internal static IGamblingGame CreateInstance(Type t, Player player)
        {
            return (IGamblingGame)Activator.CreateInstance(t, player);
        }

        internal static async Task Initialise(Player player, string name, int bet, IMessageChannel chan)
        {
            Type gameClass = GetGameType(name);
            IGamblingGame game = CreateInstance(gameClass, player);
            await game.StartGame(player.ui.Channel, bet);

            ((GamblingGame)game).deck.Channel = chan;
        }

        internal string title;
        internal string description;
        internal string[] options;

        internal Player player;
        internal Deck deck;

        internal bool gameEnded = false;
        internal bool roundEnded = false;
        string lastRoundActions;

        internal long totalBet = 0;
        internal int top;
        internal List<string> winners;

        public GamblingGame(Player active, string action = null)
        {
            player = active;
            winners = new List<string>();
        }

        internal void ReadHands()
        {
            gameEnded = true;
            roundEnded = true;

            if (!player.IsSolo)
            {
                lastRoundActions = null;
                foreach (PartyMember m in player.Party.members)
                {
                    Hand hand = m.Path == player._id ? player.GamblingHand :
                        Databse.LoadRecord("Hand",
                        AMIData.MongoDatabase.FilterEqual<Hand, string>("_id", m.Path));

                    if (hand != null)
                    {
                        totalBet += hand.bet;

                        lastRoundActions += $"{hand._id.Split('\\')[1]} > {hand.turn} {Environment.NewLine}";

                        if (hand.turn == null && hand.active) roundEnded = false;

                        if (hand.active) gameEnded = false;

                        ((IGamblingGame)this).CompareTop(hand);
                    }
                }
            }
            else
            {
                Hand hand = player.GamblingHand;
                totalBet += hand.bet;
                if (hand.active) gameEnded = false;
                ((IGamblingGame)this).CompareTop(hand);
            }

            if (deck.house != null)
            {
                totalBet += deck.house.bet;
                ((IGamblingGame)this).HousePlay(deck.house);
                ((IGamblingGame)this).CompareTop(deck.house);
                lastRoundActions += $"__House__ > {deck.house.turn} {Environment.NewLine}";
            }

            deck.Save();
        }

        internal void SetHand(int initBet, int cardCount, bool setHouse)
        {
            bool solo = player.IsSolo;
            deck = new Deck(solo ? player._id : player.Party.partyName);
            if (solo)
            {
                player.GamblingHand = new Hand(player._id, Hand.Game.Blackjack)
                {
                    bet = initBet,
                    cards = deck.PickRandom(2),
                };

                player.KCoins -= initBet;
                player.GamblingHandKey.Save();
            }
            else
            {
                foreach (PartyMember m in player.Party.members)
                {
                    Player p = player.userid == m.id ? player : m.LoadPlayer();
                    p.GamblingHand = new Hand(p._id, Hand.Game.Blackjack)
                    {
                        bet = initBet,
                        cards = deck.PickRandom(2)
                    };
                    p.KCoins -= initBet;
                    p.GamblingHandKey.Save();
                }
            }
            if (setHouse)
            {
                deck.house = new Hand("House", Hand.Game.Blackjack)
                {
                    bet = initBet,
                    cards = deck.PickRandom(2)
                };
            }

            deck.Save();
        }

        internal async Task EndRound(IMessageChannel chan = null)
        {
            string content = null;
            string endContent = null;
            if (gameEnded)
            {
                content = "Game Over, Top Score: " + top + Environment.NewLine;

                if (winners.Count > 0 && !winners.Contains("House"))
                {
                    foreach (string winner in winners)
                        content += winner.Split('\\')[1] + Environment.NewLine;

                    content += (winners.Count > 1 ? "have" : "has")
                        + $" won {totalBet / winners.Count} Kuts";
                }
                else content += "The house won";
            }
            else if(deck.house != null)
            {
                if (deck.house.active) deck.house.turn = null;
                deck.Save();
            }
            if (!player.IsSolo)
            {
                foreach (PartyMember m in player.Party.members)
                {
                    Player p = player.userid == m.id ? player : m.LoadPlayer();

                    if (p.GamblingHand != null)
                    {
                        endContent += $"<@{p.userid}> [{string.Join("] [", p.GamblingHand.cards)}]" + Environment.NewLine;

                        if (gameEnded)
                        {
                            await p.ui.EditMessage(content, GetEmbed(p, lastRoundActions).Build(), removeReactions: true);
                            await p.GamblingHandKey.Delete();
                            p.ui = null;
                            p.SaveFileMongo();
                        }
                        else
                        {
                            if (p.GamblingHand.active)
                            {
                                p.GamblingHand.turn = null;
                                p.GamblingHandKey.Save();
                            }
                            Embed embed = GetEmbed(p, lastRoundActions).Build();
                            await p.EnUI(chan == null, content, embed, await p.DMChannel(), MsgType.CardGame, title);
                        }
                    }
                }
            }
            else
            {
                player.GamblingHand.turn = null;

                Embed embed = GetEmbed(player, lastRoundActions).Build();
                if(!gameEnded)
                    await player.EnUI(chan == null, content, embed, chan ?? await player.DMChannel(), MsgType.CardGame, title);
                else
                {
                    await player.ui.EditMessage(content, embed, chan ?? await player.DMChannel(), removeReactions: true);
                    await player.GamblingHandKey.Delete();
                    player.ui = null;
                    player.SaveFileMongo();
                }
            }

            if (gameEnded)
            {
                if(deck.house != null)
                    endContent += $"__House__ [{string.Join("] [", deck.house.cards)}]" + Environment.NewLine;
                var ogChan = deck.Channel;
                if (ogChan != null)
                {
                    await ogChan.SendMessageAsync(content + Environment.NewLine + endContent);

                    Player leader = player.userid == player.Party.GetLeaderID() ? player : player.Party.GetLeader();

                    await SelectInitialBet(leader, ogChan, title, deck.house?.bet ?? 10);
                }

                await deck.Delete();
            }
        }

        internal EmbedBuilder GetEmbed(Player player, string lastRoundActions = null)
        {
            Hand hand = player.GamblingHand;

            EmbedBuilder embed = DUtils.BuildEmbed(title, description,
                null, player.userSettings.Color(),
                DUtils.NewField("Current Hand",
                $"Bet: {hand.bet} Kuts {Environment.NewLine}" +
                $"Cards: [{string.Join("] [", hand.cards)}] {Environment.NewLine}" + 
                $"**{((IGamblingGame)this).GetResult(player)}**")
            );
            if(lastRoundActions != null)
                embed.AddField("Last Round", lastRoundActions);

            return embed.AddField("Actions", 
                string.Join(Environment.NewLine, options));
        }

        
    }
}
