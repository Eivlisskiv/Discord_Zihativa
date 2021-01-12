using AMI.Methods;
using AMI.Neitsillia.Areas.AreaPartials;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace AMI.AMIData.Web.Controllers
{
    [Route("api/areas")]
    [ApiController]
    public class AreasController : ControllerBase
    {
        private async Task Json(object obj)
        {
            string json = Utils.JSON(obj);
            Response.Headers.Add("Content-Type", "application/json");
            await Response.WriteAsync(json);
        }

        private async Task Error(string error)
        {
            Response.Headers.Add("Content-Type", "application/json");
            await Response.WriteAsync($"{{error: {error} }}");
        }

        // GET api/<AreasController>/5
        [HttpGet("{name}")]
        public async Task Get(string name)
        {
            Area area = null;
            try { area = Area.LoadFromName(name); }
            catch (Exception) { }

            if (area == null) await Error("not found");
            else await Json(area);
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
