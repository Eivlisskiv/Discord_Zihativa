namespace AMI.Neitsillia.Gambling.Cards
{
    public class Card
    {
        public enum  Shape { Club, Spade, Heart, Diamond }

        public int number;
        public Shape shape;

        public string Name
        {
            get
            {
                switch(number)
                {
                    case 0: return "Joker";
                    case 1: return "Ace";
                    case 2: return "Two";
                    case 3: return "Three";
                    case 4: return "Four";
                    case 5: return "Five";
                    case 6: return "Six";
                    case 7: return "Seven";
                    case 8: return "Eight";
                    case 9: return "Nine";
                    case 10: return "Ten";
                    case 11: return "Jack";
                    case 12: return "Queen";
                    case 13: return "King";
                }

                return number.ToString();
            }
        }

        public string Image
        {
            get
            {
                switch(shape)
                {
                    case Shape.Club: return "♣️";
                    case Shape.Spade: return "♠️";
                    case Shape.Heart: return "♥️";
                    case Shape.Diamond: return "♦️";
                }
                return null;
            }
        }

        public Card(int num, Shape type)
        {
            number = num;
            shape = type;
        }

        public string ShortName()
        {
            return
                (number > 1 && number < 11 ? number.ToString() : Name) +
                Image;
        }

        public string LongName()
        {
            return number == 0 ? Name : $"{Name} Of {shape}s";
        }

        public override string ToString()
        {
            return ShortName();
        }
    }
}
