using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace AMI.AMIData.Web.Controllers
{
    [Route("api/query")]
    [ApiController]
    class QueryController : MainController<object>
    {
        static MongoDatabase Database => AMYPrototype.Program.data.database;

        internal override object PrivitizeObject(object obj)
            => obj;

        // GET /api/query/Character?query={level:{$gt:0}}
        [HttpGet("{table}")]
        public async Task Get(string table, string query, string fields)
        {
            //if (!Authenticate()) return "{\"error\": \"401\"}";

            await Response.WriteAsync(Database.Query(table, query, fields));
        }
    }
}
