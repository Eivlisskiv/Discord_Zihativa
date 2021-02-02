using AMI.Neitsillia.Areas.AreaPartials;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace AMI.AMIData.Web.Controllers
{
    [Route("api/area")]
    [ApiController]
    public class AreasController : MainController<Area>
    {
        internal override Area PrivitizeObject(Area area)
        {
            //remove private data unless token is owner or admin
            if (area.sandbox != null)
            {
                area.sandbox.buildingBlueprints = null;
                area.sandbox.stock = null;
                area.sandbox.stats = null;
            }
            return area;
        }

        // GET api/<AreasController>/5
        [HttpGet("id/{address}")]
        public async Task Get(string address)
        {
            string id = address.Replace(';', '\\');
            Area area = null;
            try { area = Area.LoadArea(id); }
            catch (Exception) { }

            if (area == null) await Error("not found");
            else
            {
                await ToJson(area);
            }
        }

        [HttpPost()]
        public async Task Post([FromBody] string id)
        {
            Area area = Area.LoadArea(id, table: Neitsillia.Areas.AreaPath.Table.Area);
            if (area == null) await Error("not found");
            else await Json(area);
        }
    }
}
