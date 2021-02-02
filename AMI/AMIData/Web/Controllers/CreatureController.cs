using AMI.Neitsillia.NPCSystems;
using AMYPrototype;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace AMI.AMIData.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CreatureController : MainController<NPC>
    {
        static AMIData.MongoDatabase Database => Program.data.database;

        internal override NPC PrivitizeObject(NPC obj)
        {
            obj.inventory = null;
            obj.equipment = null;
            return obj;
        }

        [HttpGet("id/{name}")]
        public async Task Get(string name)
        {
            NPC npc = Database.LoadRecord("Creature", MongoDatabase.FilterEqual<NPC, string>(
                "displayName", name));
            await ToJson(npc);
        }

        [HttpGet("search/{filter}")]
        public async Task GetContains(string filter)
        {
            var npcs = await Database.LoadRecordsContain<NPC>("Creature", "displayName", filter);
            await ToJson(npcs.ToArray());
        }
    }
}
