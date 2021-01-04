using AMI.AMIData;
using AMI.Methods;
using AMI.Module;
using AMI.Neitsillia.Areas.Strongholds;
using AMI.Neitsillia.Encounters;
using AMI.Neitsillia.NPCSystems;
using AMI.Neitsillia.User.PlayerPartials;
using AMYPrototype;
using Discord;
using Neitsillia.Items.Item;
using NeitsilliaEngine;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AMI.Neitsillia.Areas.AreaPartials
{
    [MongoDB.Bson.Serialization.Attributes.BsonIgnoreExtraElements]
    partial class Area
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
        public List<string>[] loot;
        public int eMobRate;
        public List<string>[] mobs;
        public int ePassiveRate;
        public string[][] passives;//To deprecate
        public string[] passiveEncounter;

        public int eQuestRate;

        public string realm, continent, kingdom, parent;
        public string native, faction;
        public AreaType type;

        public List<Junction> junctions;

        public SandBox sandbox;

        #region Loading
        public static Area Load(AreaPath path) => LoadArea(path.path, path.table);

        public static Area LoadArea(string primary, string secondary = null, AreaPath.Table table = AreaPath.Table.Area)
        {
            Area area = null;
            string path = null;
            if ((area = LoadArea(primary, table)) != null)
                return area;

            else if (secondary != null)
            {
                Log.LogS($"Area Path: {path} was not found >> Source: {primary}");
                return LoadArea(secondary, null);
            }
            Log.LogS($"Area Path: {path} was not found >> Source: {primary} | {secondary}");
            return null;
            //throw new Exception("Area Failed to Load. Error Logged");
        }
        static Area LoadArea(string areaId, AreaPath.Table table)
        {
            //Area area = table == AreaPath.Table.Area ? areasCache.Load(areaId) : dungeonsCache.Load(areaId);
            //if (area != null) return area;
            try
            { 
                return Database.LoadRecord(table.ToString(), MongoDatabase.FilterEqual<Area, string>("AreaId", areaId)) 
                    ?? (table == AreaPath.Table.Dungeons ? Database.LoadRecord("Area", MongoDatabase.FilterEqual<Area, string>("AreaId", areaId)) :
                    Database.LoadRecord("Dungeons", MongoDatabase.FilterEqual<Area, string>("AreaId", areaId)));

                //(table == AreaPath.Table.Area ? areasCache : dungeonsCache).Save(areaId, area);

                //return area;
            }
            catch (Exception e)
            {
                if (e is NeitsilliaError error)
                    Log.LogS(error.ExtraMessage);
                else
                    _ = Handlers.UniqueChannels.Instance.SendToLog(e);
                return null;
            }
        }

        public static Area LoadFromName(string name)
        {
            List<Area> List = Database.LoadRecords("Area", MongoDatabase.FilterRegex<Area>("AreaId", name + "$"));

            if (List.Count == 1) return List[0];

            if (List.Count < 1) throw NeitsilliaError.ReplyError("No area found");
            
            else
            {
                string elements = null;
                List.ForEach(a => {
                    elements += a.AreaId + Environment.NewLine;
                });
                throw NeitsilliaError.ReplyError("Please precise the area id." + Environment.NewLine + elements);
            }
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Use Area.LoadArea instead
        /// </summary>
        /// <param name="jsonOnly"></param>
        [JsonConstructor]
        public Area(bool jsonOnly){}
        public Area(AreaType type, string name, Area parentArea)
        {
            this.name = name;
            this.type = type;
            realm = parentArea.realm;
            continent = parentArea.continent;
            kingdom = parentArea.kingdom;
            parent = parentArea.name;
            AreaId = GeneratePath(true);

            level = parentArea.level;

            if (type != AreaType.Dungeon && type != AreaType.Arena)
                junctions = new List<Junction>();
        }
        #endregion

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

            if (realm != null) areaPath += realm + @"\";

            if (continent != null) areaPath += continent + @"\";

            if (kingdom != null)  areaPath += kingdom + @"\";

            if (parent != null)  areaPath += parent + @"\";
            else areaPath += name + @"\";

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

        internal async Task UploadToDatabase(bool skipChecks = false)
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

        bool ValidTable(IEnumerable<string>[] table)
        {
            return table != null && table.Length > 0;
        }

        public void AutoExplore(Player player, Random rng, bool depricated)
        {
            if (player.Encounter == null)
                player.NewEncounter(new Encounter("Adventure", player), false);
            if (player.stamina > 0)
            {
                int x = rng.Next(1, eLootRate + eMobRate + ePassiveRate + eQuestRate + 1);
                if (x <= eLootRate)
                {
                    int t = ArrayM.IndexWithRates(loot.Length, rng);
                    player.Encounter.AddLoot(Item.LoadItem(loot[t][ArrayM.IndexWithRates(loot[t].Count, rng)]));
                }
                else if (x <= eMobRate + eLootRate)
                {
                    NPC mob = GetAMob(rng, player.areaPath.floor);
                    double playerPower = player.PowerLevel();
                    double mobPower = mob.PowerLevel();
                    double mod = playerPower - mobPower;
                    double result = rng.Next(1, 200) + mod;
                    if (result >= 140)
                        player.Encounter.AddLoot(Item.LoadItem(mob.MobDrops(1)[0]));
                    else if (result <= 100)
                        player.health--;
                }
                else if (x <= ePassiveRate + (eMobRate + eLootRate))
                {
                    player.Encounter.xpToGain += 10;
                    player.Encounter.koinsToGain += 1;
                }
                else
                {
                    if (player.health + 1 < player.Health())
                        player.health++;
                    player.stamina += Verify.Max(5, player.Stamina() - player.stamina);
                }
                player.stamina--;
            }
            else if(rng.Next(101) <= 20)
            {
                if (player.health + 1 < player.Health())
                    player.health++;
                player.stamina += Verify.Max(5, player.Stamina() - player.stamina);
            }
        }
        
        

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

        internal static async Task<Area> NewStronghold(string argname, int size, Area area, Player player)
        {
            //public string native, faction;

            //public SandBox sandbox;
            Area stronghold = new Area(false)
            {
                name = argname, level = Verify.Min(player.level, area.level),
                description = $"A Stronghold built by {player.name}",
                ePassiveRate = 100,
                passiveEncounter = new string[] { "Npc" },
                realm = area.realm, continent = area.continent, kingdom = area.kingdom,
                type = AreaType.Stronghold, junctions = new List<Junction> { new Junction(area, 0, player.areaPath.floor) },
                sandbox = new SandBox(player.userid, size)
            };
            stronghold.AreaId = stronghold.GeneratePath();

            area.junctions.Add(new Junction(stronghold, player.areaPath.floor, 0));
            await area.UploadToDatabase();
            await stronghold.UploadToDatabase();
            return stronghold;
        }
    }
}
