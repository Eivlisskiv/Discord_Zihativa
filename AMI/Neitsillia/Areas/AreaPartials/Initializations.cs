using AMI.AMIData;
using AMI.Methods;
using AMI.Module;
using NeitsilliaEngine;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AMI.Neitsillia.Areas.AreaPartials
{
    public partial class Area
    {
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
            try
            {
                return Database.LoadRecord(table.ToString(), MongoDatabase.FilterEqual<Area, string>("AreaId", areaId))
                    ?? (table == AreaPath.Table.Dungeons ? Database.LoadRecord("Area", MongoDatabase.FilterEqual<Area, string>("AreaId", areaId)) :
                    Database.LoadRecord("Dungeons", MongoDatabase.FilterEqual<Area, string>("AreaId", areaId)));
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

        /// <summary>
        /// Use Area.LoadArea instead
        /// </summary>
        /// <param name="jsonOnly"></param>
        [JsonConstructor]
        public Area(bool jsonOnly) { }
        public Area(AreaType type, string name, Area parentArea)
        {
            this.name = name;
            this.type = type;
            realm = parentArea.realm;
            continent = parentArea.continent;
            kingdom = parentArea.kingdom;
            grandparent = parentArea.parent;
            parent = parentArea.name;

            AreaId = GeneratePath(true);

            level = parentArea.level;

            if (type != AreaType.Dungeon && type != AreaType.Arena)
                junctions = new List<Junction>();
        }
    }
}
