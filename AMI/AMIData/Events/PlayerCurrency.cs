using System.Collections.Generic;

namespace AMI.AMIData.Events
{
    class PlayerCurrency
    {
        public static PlayerCurrency Load(string id)
            => AMYPrototype.Program.data.database.LoadRecord("PlayerCurrency", MongoDatabase.FilterEqual<PlayerCurrency, string>("_id", id)) ?? new PlayerCurrency(id);

        public static void Save(PlayerCurrency pc)
            => AMYPrototype.Program.data.database.UpdateRecord("PlayerCurrency", MongoDatabase.FilterEqual<PlayerCurrency, string>("_id", pc._id), pc);

        public string _id;
        public Dictionary<string, int> currencies = new Dictionary<string, int>();

        public PlayerCurrency(string id)
        {
            _id = id;
        }

        public int Get(string name) => currencies.ContainsKey(name) ? currencies[name] : 0;

        public void Mod(string name, int amount, bool save = true)
        {
            if (currencies.ContainsKey(name)) currencies[name] += amount;
            else currencies.Add(name, amount);

            if (save) Save();
        }

        public void Save() => Save(this);
    }
}
