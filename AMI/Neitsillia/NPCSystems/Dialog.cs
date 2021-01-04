using AMYPrototype;
using System;

namespace AMI.Neitsillia.NPCSystems
{
    class Dialog
    {
        public static string[] saidAs = new string[]
        { "said " };
        public static string[] nothingToSell = new string[]
        {
            "I have nothing to sell.", "Try somewhere else, I have no stock."
        };
        public static string[] notenoughK = new string[]
        {
            "I can not afford that.", "My pockets are too light for that.",
        };
        public static string[] notenoughI = new string[]
        {
            "I don't have the amount you require.", "I can't sell you more than I have..." , "I don't sell eggs before they're laid."
        };
        public static string[] tradingBusiness = new string[]
        {
            "I thank you for your business.", "I hope we can continue this commerce.", "Another day, another deal."
        };
        public static string[] notInterested = new string[]
        {
            "I'm not interested in that item.", "Sorry, but I'm not buying that."
        };
        public static string[] weakRecruit = 
        {
            "I must refuse, I would only slow you down.", "The places someone of your strength visits are too dangerous for me.",
            "I may not follow you to my death, you are far out of my league."
        };
        public static string[] peasantRecruit =
        {
            "My place is here, traveler.", "I have much to tend to here, I cannot follow you.", "Adventures are not for me, I wish you luck in your quest."
        };
        public static string[] offerCancelled = new string[]
        {
            "I'm sorry we could not make this trade.", "Perhaps something else from my inventory interests you."
        };

        public static string GetDialog(NPC npc, string[] subject, string[] emotion = null, string[] asWhat = null)
        {
            Random r = Program.rng;
            string dialog = $" \"{subject[r.Next(subject.Length)]}\" ";
            dialog += GetSaid(npc, r);
            if (emotion != null)
                dialog += emotion[r.Next(emotion.Length)] + " ";
            if (asWhat != null)
                dialog += asWhat[r.Next(asWhat.Length)];
            return dialog;
        }
        static string GetSaid(NPC n, Random r)
        {
            int index = r.Next(0, 2);
            string str = saidAs[r.Next(saidAs.Length)];
            if (index == 0)
                str += "the " + n.profession;
            else
                str += n.name;
            return str;
        }
    }
}
