using AMI.Neitsillia.User.PlayerPartials;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace AMI.AMIData.Web.Controllers
{
    [Route("api/Character")]
    [ApiController]
    public class CharacterController : MainController<Player>
    {
        internal override Player PravitizeObject(Player obj)
        {
            return obj;
        }

        [HttpGet]
        public async Task Get(ulong id, string name)
        {
            try
            {
                Player player = Player.Load($"{id}\\{name}", Player.IgnoreException.All);
                if (await NullError(player)) return;
                await ToJson(player);
            }
            catch(Exception e)
            {
                await Error(e.Message);
            }
        }
    }
}
