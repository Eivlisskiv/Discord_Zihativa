using AMI.Neitsillia.Gambling.Cards;
using AMI.Neitsillia.User;
using AMI.Neitsillia.User.PlayerPartials;
using AMI.Neitsillia.User.UserInterface;
using AMYPrototype;
using Discord;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AMI.Neitsillia.Gambling.Games
{
    class Blackjack : GamblingGame, IGamblingGame
    {
        public static Dictionary<string, string> Actions = new Dictionary<string, string>
        {
            {EUI.card_hit, "Hit" },
            {EUI.card_stand, "Stand" },
            {EUI.cancel, "Quit" },
        };

        public Blackjack(Player active) : base(active)
        {
            deck = Deck.Load(player);
            title = "Blackjack";
            gamesInfo.TryGetValue(title, out description);
            options = new string[]
            {
                $"{EUI.card_hit} Hit: Get another card",
                $"{EUI.card_stand} Stand: Take no more cards.",
                $"{EUI.cancel} Exit the game. No bet refunds."
            };
        }

        public async Task StartGame(IMessageChannel chan, int bet)
        {
            SetHand(bet, 2, true);
            await EndRound(chan);
        }

        public void Action(string action)
        {
            switch(action)
            {
                case "Hit":
                    player.GamblingHand.cards.AddRange(deck.PickRandom());
                    player.GamblingHand.active = GetScore(player.GamblingHand) < 21;
                    break;
                case "Stand":
                    player.GamblingHand.active = false;
                    break;

                default: return;
            }
        }

        public async Task EndTurn()
        {
            ReadHands();
            if (roundEnded)
                await EndRound();
            else
            {
                await player.SendMessageToDM("Awaiting other player(s) action(s)");
                player.SaveFileMongo();
            }
        }

        public void HousePlay(Hand house)
        {
            if (house.turn != null || !house.active) return;

            int score = GetScore(house);
            if (score < 10 || Program.Chance((21 - score) * (score <= 15 ? 10 : 1)))
            {
                house.cards.AddRange(deck.PickRandom());
                score = GetScore(house);
                house.turn = "Hit";
            }
            else
            {
                house.active = false;
                house.turn = "Stand";
            }

            if (house.active) house.active = score < 21;
        }

        public void CompareTop(Hand hand, int? s = null)
        {
            int score = s ?? GetScore(hand);
            if (score > 21)
            {
                hand.turn = "Bust";
                return;
            }

            if (score == top)
            {
                winners.Add(hand._id);
            }
            else if (score > top)
            {
                top = score;
                winners = new List<string> { hand._id };
            }
        }

        public string GetResult(Player player)
        {
            Hand hand = player.GamblingHand;

            int score = GetScore(hand);
            if (score > 21)
            {
                hand.active = false;
                return "Bust: " + score;
            }
            else if (!gameEnded)
                return "Cards Score: " + score;

            string s = "Lost";

            if(score == top)
            {
                s = "Won";
                player.KCoins += totalBet / winners.Count;
            }

            return s + $" with {score} points";
        }

        internal int GetScore(Hand hand)
        {
            int aces = 0;
            int score = 0;
            hand.cards.ForEach(c =>
            {
                if (c.number == 1) aces++;
                else if (c.number > 10) score += 10;
                else score += c.number;

            });
            if(aces > 0)
            {
                if (score < 11)
                {
                    score += 11 + (aces - 1);
                }
                else score += aces;
            }
            return score;
        }
    }
}
