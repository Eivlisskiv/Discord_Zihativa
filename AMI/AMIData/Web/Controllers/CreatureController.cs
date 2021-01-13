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

        internal override NPC PravitizeObject(NPC obj)
        {
            obj.inventory = null;
            obj.equipment = null;
            return obj;
        }

        [HttpGet("{name}")]
        public async Task Get(string name)
        {
            NPC npc = Database.LoadRecord("Creature", MongoDatabase.FilterEqual<NPC, string>(
                "displayName", name));
            await ToJson(npc);
        }
    }
}
