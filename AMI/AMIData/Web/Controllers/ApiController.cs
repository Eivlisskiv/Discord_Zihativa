using AMI.Methods;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace AMI.AMIData.Web.Controllers
{
    [Route("api/query")]
    [ApiController]
    public class QueryController : ControllerBase
    {
        static MongoDatabase Database => AMYPrototype.Program.data.database;

        private bool Authenticate()
        {
            if (!Request.Headers.TryGetValue("Authorization", out var tokenValue))
                return false;
            Log.LogS("request token recieved " + tokenValue);
            return true;

        }

        // GET /api/query/Character?query={level:{$gt:0}}
        [HttpGet("{table}")]
        public IActionResult Get(string table, string query, string fields)
        {
            //if (!Authenticate()) return "{\"error\": \"401\"}";

            return Ok($"{{ {Database.Query(table, query, fields)} }}");
        }

        [HttpPost("{table}")]
        public IActionResult Post(string table, [FromBody] string query, string fields)
        {
            //if (!Authenticate()) return "{\"error\": \"401\"}";

            return Ok($" {{ {Database.Query(table, query, fields)} }}");
        }
    }
}
