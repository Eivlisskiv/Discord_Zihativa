using AMI.Methods;
using AMI.Neitsillia.Areas.Strongholds;
using AMI.Neitsillia.User.PlayerPartials;
using NeitsilliaEngine;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AMI.Neitsillia.Areas.AreaPartials
{
    public partial class Area
    {
        public SandBox sandbox;

        internal static async Task<Area> NewStronghold(string argname, int size, Area area, Player player)
        {
            Area stronghold = new Area(false)
            {
                name = argname,
                level = Verify.Min(player.level, area.level),
                description = $"A Stronghold built by {player.name}",
                ePassiveRate = 100,
                passiveEncounter = new string[] { "Npc" },
                realm = area.realm,
                continent = area.continent,
                kingdom = area.kingdom,
                type = AreaType.Stronghold,
                junctions = new List<Junction> { new Junction(area, 0, player.areaPath.floor) },
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
