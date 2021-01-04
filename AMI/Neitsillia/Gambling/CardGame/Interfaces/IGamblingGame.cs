using AMI.Neitsillia.Gambling.Cards;
using AMI.Neitsillia.User.PlayerPartials;
using Discord;
using System.Threading.Tasks;

namespace AMI.Neitsillia.Gambling.Games
{
    interface IGamblingGame
    {
        /// <summary>
        /// Set each player's hands depending on the game type with
        /// </summary>
        /// <param name="chan"></param>
        /// <param name="bet"></param>
        /// <returns></returns>
        Task StartGame(IMessageChannel chan, int bet);
        void Action(string action);
        Task EndTurn();

        /// <summary>
        /// Compare each player's score during the ReadHands to find the highest score
        /// or winning goal
        /// </summary>
        /// <param name="hand">Player's hand</param>
        void CompareTop(Hand hand, int? s = null);
        /// <summary>
        /// Compare the player's result with the top result
        /// Is called during the embed reply building
        /// </summary>
        /// <param name="player"></param>
        /// <returns>The string result to be placed in the embed</returns>
        string GetResult(Player player);
        /// <summary>
        /// If available, players as house and checks it against tops.
        /// </summary>
        void HousePlay(Hand house);
    }
}
