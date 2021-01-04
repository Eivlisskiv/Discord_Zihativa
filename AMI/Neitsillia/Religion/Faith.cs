using AMI.Neitsillia.User.PlayerPartials;
using System;
using System.Collections.Generic;

namespace AMI.Neitsillia.Religion
{
    class Faith
    {
        internal static readonly Dictionary<string, (string diety, int tier)> Sigils =
        new Dictionary<string, (string, int)>()
        {
            //Avlimia
            {"Lesser Sigil Of Avlimia", ("Avlimia", 1) },
            //Bakora
            {"Lesser Sigil Of Bakora", ("Bakora", 1) },
        };

        internal static readonly Dictionary<string, string> Blessings =
        new Dictionary<string, string>()
        {
            {"Avlimia", "Blessing Of Avlimia"},
            {"Bakora", "Blessing Of Bakora"}
        };

        public string _id;
        public string diety;
        public int devotion;
        public DateTime lastPrayer;

        public Faith(string id)
        {
            _id = id;
        }

        internal string Pray(Player player, string sigil)
        {
            if (lastPrayer.AddDays(1) >= DateTime.UtcNow) return "You've already shown your devotion today.";

            (string diety, int tier) = Sigils[sigil];
            if (diety != this.diety)
            {
                this.diety = diety;
                devotion = 0;
            }

            devotion += Math.Min(tier, 100 - devotion);

            if (!Blessings.ContainsKey(diety) || Blessings[diety] == null) return $"{diety} has no blessings to offer.";

            player.Status(Blessings[diety], 100, devotion);

            lastPrayer = DateTime.UtcNow;
            player.SaveFileMongo();

            return $"{diety} has heard your prayer.";
        }

    }
}
