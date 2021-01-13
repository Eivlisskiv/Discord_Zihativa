using Microsoft.AspNetCore.Mvc;
using Neitsillia.Items.Item;
using System.Threading.Tasks;

namespace AMI.AMIData.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ItemController : MainController<Item>
    {
        internal override Item PravitizeObject(Item obj) => obj;

        [HttpGet("{name}")]
        public async Task Get(string name)
        {
            Item item = Item.LoadItem(name);
            await ToJson(item);
        }

        
    }
}
