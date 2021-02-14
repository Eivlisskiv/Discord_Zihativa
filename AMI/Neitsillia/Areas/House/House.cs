using AMI.AMIData;
using AMI.Neitsillia.User.PlayerPartials;
using AMYPrototype;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AMI.Neitsillia.Areas.House
{
    [MongoDB.Bson.Serialization.Attributes.BsonIgnoreExtraElements]
    public class House
    {
        private const int HOUSE_PRICE = 6544;
        public static long HousePrice(int level) => Math.Max(HOUSE_PRICE, HOUSE_PRICE * level);

        static MongoDatabase Database => Program.data.database;

        public static async Task<House> Load(ulong id)
            => await Database.LoadRecordAsync<House, ulong>(id);

        private static async Task Save(House house)
            => await Database.UpdateRecordAsync(null, 
                MongoDatabase.FilterEqual<House, ulong>("_id", house._id), house);

        public ulong _id;

        public List<string> junctions;
        public Sandbox.Sandbox sandbox;

        public House(Player player)
        {
            _id = player.userid;
            junctions = new List<string>() { player.Area.AreaId };

            player.KCoins -= HousePrice(player.Area.level);
            sandbox = new Sandbox.Sandbox();
        }

        public async Task Save() => await Save(this);
    }
}
