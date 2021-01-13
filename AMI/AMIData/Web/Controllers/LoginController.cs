using System;
using System.Collections.Generic;
using System.Net;
using AMI.AMIData.Webhooks;
using AMI.Methods;
using AMI.Neitsillia.User;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace AMI.AMIData.Web.Controllers
{
    [Route("api/login")]
    [ApiController]
    public class LoginController : ControllerBase
    {
        [HttpGet("discord/{token}")]
        public IActionResult GetDiscord(string token)
        {
            HttpWebRequest request = WebServer.Request("https://discord.com/api/users/@me");
            request.Headers.Add(HttpRequestHeader.Authorization, "Bearer " + token);
            string discordResponse = WebServer.ReadRequest(request);

            //Parse discord user data
            Dictionary<string, string> data = Utils.JSON<Dictionary<string, string>>(discordResponse);

            ulong userId = ulong.Parse(data["id"]);
            BotUser botUser = BotUser.Load(userId);

            return Ok("{" +
                $"discord: {discordResponse}," + Environment.NewLine +
                $"server: {{ {Utils.JSON(botUser)} }}," +
                $"characters: {{ {Utils.JSON(BotUser.GetCharFiles(userId))} }}," +
                "}");
        }

        // POST api/<ValuesController>
        //[HttpPost]
        //public void Post([FromBody] string value)
        //{
        //}

        //// PUT api/<ValuesController>/5
        //[HttpPut("{id}")]
        //public void Put(int id, [FromBody] string value)
        //{
        //}

        //// DELETE api/<ValuesController>/5
        //[HttpDelete("{id}")]
        //public void Delete(int id)
        //{
        //}
    }
}
