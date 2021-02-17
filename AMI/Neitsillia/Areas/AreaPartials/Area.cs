using AMI.AMIData;
using AMI.Methods;
using AMI.Neitsillia.Encounters;
using AMI.Neitsillia.NPCSystems;
using AMI.Neitsillia.User.PlayerPartials;
using AMYPrototype;
using Discord;
using Neitsillia.Items.Item;
using NeitsilliaEngine;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AMI.Neitsillia.Areas.AreaPartials
{
    [MongoDB.Bson.Serialization.Attributes.BsonIgnoreExtraElements]
    public partial class Area
    {
        static MongoDatabase Database => Program.data.database;

        //private static Cache<string, Area> areasCache = new Cache<string, Area>();
        //private static Cache<string, Area> dungeonsCache = new Cache<string, Area>();

        [MongoDB.Bson.Serialization.Attributes.BsonId]
        public string AreaId { get; set; }
        //
        public string name;
        public int floors;
        public int level;
        //info
        public string description;
        //Encounters Rate
        public int eLootRate;
        public string[][] loot;
        public int eMobRate;
        public List<string>[] mobs;
        public int ePassiveRate;
        public string[][] passives;//To deprecate
        public string[] passiveEncounter;

        public int eQuestRate;

        public string realm, continent, kingdom, grandparent, parent;
        public string native, faction;
        public AreaType type;

        public List<Junction> junctions;

        #region Gets
        public EmbedBuilder AreaInfo(int floor, bool dispopu = false, int popuPage = 0)
        {
            EmbedBuilder areainfo = new EmbedBuilder();
            areainfo.WithTitle(name);
            if (description != null) areainfo.WithDescription(description);

            string infolist = realm + ", " + continent + ", " + kingdom;
            if (parent != null)  infolist += ", " + parent;

            infolist += Environment.NewLine +
                "Location Type: " + type + Environment.NewLine;

            if (level > 0) infolist += "Base Level: " + level + Environment.NewLine;
            if (floor > 0)  infolist += "Floor: " + floor + Environment.NewLine;

            GetPopulation(AreaExtentions.Population.Type.Population);
            GetPopulation(AreaExtentions.Population.Type.Bounties);

            if (_population != null && _population.Count > 0) infolist += "Population: " + _population.Count + Environment.NewLine;
            if (_bounties != null && _bounties.Count > 0)  infolist += "Bounties: " + _bounties.Count + Environment.NewLine;
            if (sandbox != null) infolist += $"Area Leader: <@{sandbox.leader}>";

            areainfo.AddField("Information", infolist);
            areainfo.WithImageUrl(Campaigns.PremadeMap.Load(this).url);

            return areainfo;
        }
        /// <summary>
        /// Gets the Area's file path from its adress
        /// </summary>
        /// <param name="addAreaName">true: get the path for the file, false: get the path for its folder</param>
        /// <returns></returns>
        public string GeneratePath(bool addAreaName = true)
        {
            string areaPath = null;

            if (realm != null) areaPath += realm + "\\";

            if (continent != null) areaPath += continent + "\\";

            if (kingdom != null)  areaPath += kingdom + "\\";

            if (grandparent != null)  areaPath += grandparent + "\\";
            else if (parent != null)  areaPath += parent + "\\";
            else areaPath += name + "\\";

            if (addAreaName) areaPath += name;

            return areaPath;
        }

        public override string ToString()
        {
            if (parent == null)
                return name;
            else
                return name + " Of " + parent;
        }

        public bool IsDungeon => type == AreaType.Dungeon || type == AreaType.Arena;
        #endregion

        #region Sets
        internal void SetRates(int loot, int mob, int passive, int quest)
        {
            int total = loot + mob + passive + quest;

            eLootRate = (loot*100)/total;
            eMobRate = (mob * 100)/ total;
            ePassiveRate = (passive * 100)/ total;
            eQuestRate = (quest * 100)/ total;
        }
        #endregion

        public bool CanTravelTo(int floor, string locationName, ref string reason)
        {
            foreach (Junction j in junctions)
            {
                if (j.destination == locationName && floor >= j.floorRequirement)
                    return true;
                else if (j.destination == locationName && floor < j.floorRequirement)
                {
                    reason = "you have not reached the required floor to access that area";
                    return false;
                }
            }
            reason = "You may not access that location from your current area";
            return false;
        }

        internal async Task UploadToDatabase()
        {
            await Program.data.database.UpdateRecordAsync(
                type == AreaType.Dungeon || type == AreaType.Arena ? "Dungeons" : "Area",
                "_id", AreaId, this);
        }



        public bool IsNonHostileArea()
        {
            switch(type)
            {
                case AreaType.Stronghold:
                case AreaType.Tavern:
                case AreaType.Town:
                case AreaType.ArenaLobby:
                case AreaType.BeastMasterShop:
                    return true;
            }
            return false;
        }

        internal bool ValidTable(IEnumerable<string>[] table) =>  table != null && table.Length > 0;
       
        public NPC GetAMob(Random rng, int floor)
        {
            int mlevel = GetAreaFloorLevel(rng, floor);
            int rtier = ArrayM.IndexWithRates(mobs.Length, rng);
            return NPC.GenerateNPC(mlevel, mobs[rtier][ArrayM.IndexWithRates(mobs[rtier].Count, rng)]);
        }

        internal int GetAreaFloorLevel(Random rng, int floor = -1)
        {
            double range = 0.20;
            int fl = NumbersM.NParse<int>(level * ( 1 + (floor / (5.00 * level))));

            int min = NumbersM.NParse<int>(fl * (1 - range));
            int max = NumbersM.NParse<int>(fl * (1 + range));
            return rng.Next(min, max + 1);
        }
    }
}
