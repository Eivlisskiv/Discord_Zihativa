using Microsoft.AspNetCore.Mvc;
using AMI.Neitsillia.Items.ItemPartials;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AMI.AMIData.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ItemController : MainController<Item>
    {
        static readonly string[] ITEM_TABLES = { "Item", "Skavi", "Unique Item", "Event Items" };
        static AMIData.MongoDatabase Database => AMYPrototype.Program.data.database;
        internal override Item PrivitizeObject(Item obj) => obj;

        [HttpGet("id/{name}")]
        public async Task Get(string name)
        {
            Item i = null;
            for (int k = 0; k < ITEM_TABLES.Length && i == null; k++)
                i = Database.LoadRecord(ITEM_TABLES[k], MongoDatabase.FilterEqual<Item, string>("_id", name));

            await ToJson(i);
        }

        [HttpGet("search/{filter}")]
        public async Task GetContains(string filter)
        {
            if (filter == null) await Json(null);

            List<Item> items = new List<Item>();
            for (int k = 0; k < ITEM_TABLES.Length; k++)
                items.AddRange(await Database.LoadRecordsContain<Item>(ITEM_TABLES[k], "_id", filter));

            await ToJson(items.ToArray());
        }
    }
}
