using AMI.Neitsillia.Areas.AreaPartials;
using System.Collections.Generic;

namespace AMI.Neitsillia.Campaigns
{
    class PremadeMap
    {
        static AMIData.MongoDatabase Database => AMYPrototype.Program.data.database;

        static Dictionary<string, string> urls = new Dictionary<string, string>();

        public string _id;
        public string image;

        public PremadeMap(string areaName, string url)
        {
            _id = areaName;
            image = url;
        }

        internal static string Load(string name)
        {
            if (urls.TryGetValue(name, out string url)) return url;

            PremadeMap loaded = Database.LoadRecord(null, AMIData.MongoDatabase.FilterEqual<PremadeMap, string>("_id", name));

            if (loaded != null) urls.Add(loaded._id, loaded.image);

            return loaded?.image;
        }

        internal static (string url, string name) Load(Area area)
            => (Load(area.AreaId) ?? Load(area.GeneratePath(false) + area.parent) ?? Load(area.kingdom)
                ?? Load(area.continent) ?? Load(area.realm), area.name);

        internal static void Save(PremadeMap map)
        {
            if (urls.ContainsKey(map._id)) urls[map._id] = map.image;
            else urls.Add(map._id, map.image);

            Database.UpdateRecord(null, null, map);
        }
    }
}
