using AMI.AMIData;
using AMI.Neitsillia.Collections;
using AMI.Neitsillia.User.PlayerPartials;
using AMYPrototype;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AMI.Neitsillia.Areas.House
{
    public class House
    {
        private const int HOUSE_PRICE = 6544;
        public static long HousePrice(int level) => Math.Min(HOUSE_PRICE, HOUSE_PRICE * level);

        static MongoDatabase Database => Program.data.database;

        public static async Task<House> Load(ulong id)
            => await Database.LoadRecordAsync<House, ulong>(id);

        private static async Task Save(House house)
            => await Database.UpdateRecordAsync(null, 
                MongoDatabase.FilterEqual<House, ulong>("_id", house._id), house);

        public ulong _id;

        public List<string> junctions;

        public int storageSpace;
        public Inventory storage;

        public House(Player player)
        {
            _id = player.userid;
            junctions = new List<string>() { player.Area.AreaId };

            storageSpace = 20;
            storage = new Inventory();

            player.KCoins -= HousePrice(player.Area.level);
        }

        public async Task Save() => await Save(this);
    }
}
