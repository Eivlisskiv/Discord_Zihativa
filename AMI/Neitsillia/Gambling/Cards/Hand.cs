using System.Collections.Generic;

namespace AMI.Neitsillia.Gambling.Cards
{
    public class Hand
    {
        public enum Game { Blackjack };

        public string _id;
        public Game game;
        public List<Card> cards;

        public bool active; //Can the player do an action on turn?

        public long bet;
        public string turn;

        public bool won;

        public Hand(string id, Game g)
        {
            _id = id;
            game = g;
            cards = new List<Card>();
            active = true;
            won = false;
        }
    }
}
