using AMI.AMIData.Webhooks;
using AMI.Methods;
using AMYPrototype;
using Discord.WebSocket;
using DiscordBotsList.Api;
using DiscordBotsList.Api.Objects;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace AMI.AMIData
{
    class DiscordBotList_Top
    {

        public AuthDiscordBotListApi DblApiAuth;

        public readonly ulong botID = 465565429645967398;

        private readonly string accountKey;

        internal bool connected
        {
            get;
            private set;
        }

        public DiscordBotList_Top(string token)
        {
            accountKey = token;
            connected = false;
        }

        internal async Task Connect()
        {
            if (connected) return;
            try
            {
                DblApiAuth = new AuthDiscordBotListApi(botID, accountKey);
                await DblApiAuth.GetMeAsync();

                connected = true;
            }
            catch (Exception)
            {
                return;
            }
        }

        internal void UpdateServerCount(DiscordSocketClient client)
        {
            if (!connected) return;
            if (client.CurrentUser.Id == botID)
                DblApiAuth.UpdateStats(client.Guilds.Count);
        }

        internal async Task<List<IDblEntity>> GetVotersAsync()
        {
            if (!connected) return new List<IDblEntity>();
            return await (await GetSelf()).GetVotersAsync();
        }

        internal string WebsiteUrl => "https://top.gg/bot/" + botID;

        internal int CrateAmount()
        {
            DateTime utc = DateTime.UtcNow;
            return utc.DayOfWeek == DayOfWeek.Friday || utc.DayOfWeek == DayOfWeek.Saturday 
                || utc.DayOfWeek == DayOfWeek.Sunday ? 2 : 1;
        }

        internal async Task<IDblSelfBot> GetSelf()
        {
            if (!connected) return null;
            try
            {
                return await DblApiAuth.GetMeAsync();
            } catch (Exception)
            { return null; }
        }

        internal async Task<bool> HasVoted(ulong id, int sinceCycle = 1)
        {
            if (!connected) return true;
            try
            {
                if (sinceCycle <= 1) return await DblApiAuth.HasVoted(id);
                return await DblApiAuth.HasVoted(id);

            } catch (Exception) { return true; }
        }

        public void HandleRequest(string result)
        {
            if (result == null) Console.WriteLine("Failed Request");
            else if (result.Length > 2)
            {
                ulong[] voters = Utils.JSON<ulong[]>(result);
                foreach (ulong id in voters)
                    _ = Neitsillia.User.BotUser.Load(id).NewVote();
                Console.WriteLine("Request processed");
            }
            else Console.WriteLine("Empty Request");
        }

        internal (string, bool) IsListedServer(ulong id)
        {
            string url = null;
            try
            {
                using (WebClient client = new WebClient())
                {
                    url = "https://top.gg/servers/" + id;
                    string htmlCode = client.DownloadString(url);
                }
            }
            catch (Exception)
            {
                url = null;
                return (url, false);
            }
            return (url, true);
        }
    }
}
