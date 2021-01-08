using AMI.Methods;
using AMYPrototype;
using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AMI.Handlers
{
    class UniqueChannels
    {
        private static UniqueChannels _instance;
        public static UniqueChannels Instance => _instance ?? (_instance = new UniqueChannels());

        Dictionary<string, IMessageChannel> channels;

        public UniqueChannels()
        {
            channels = new Dictionary<string, IMessageChannel>();

            GetChannel("Suggestion", Program.isDev ? (ulong)795723733615509534 : 531191291543945227, 201877884313403392);
            GetChannel("BugReport", Program.isDev ? (ulong)795723756731498506 : 741897200317104149, 201877884313403392);
            GetChannel("Population", Program.isDev ? (ulong)795723814259392544 : 795706716137586738, 201877884313403392);
        }

        public IMessageChannel GetChannel(string name, ulong? id = null, ulong? gid = null)
        {
            try
            {
                if (channels.ContainsKey(name)) return channels[name];
                else if (id.HasValue)
                {
                    var channel = (IMessageChannel)(gid.HasValue ?
                    Program.clientCopy.GetGuild(gid ?? 0).GetChannel(id ?? 0)
                    : Program.clientCopy.GetChannel(id ?? 0));
                    if (channel != null)
                    {
                        channels.Add(name, channel);
                        return channel;
                    }
                }
            }
            catch (Exception) { }
            return null;
        }

        public async Task<IUserMessage> SendMessage(string name, string content = null, Embed embed = null)
            => await GetChannel(name)?.SendMessageAsync(content, embed: embed);

        internal async Task SendToLog(Exception e, string extra = null, IMessageChannel chan = null)
        {
            string s = chan == null ? null : (chan is IGuildChannel gchan ? $"Guild: {gchan.GuildId} " : "DMs ") + $"Channel: {chan.Id}";
            if (e is AggregateException ea)
            {
                await SendToLog(extra + Environment.NewLine + s + Environment.NewLine
                    + $"{DateTime.Now} Exception Type: {ea.GetType()} => {ea.Message} {Environment.NewLine} Check Console for details ");
                foreach (Exception eae in ea.InnerExceptions)
                    Log.LogS(eae);
            }
            else
            {
                await SendToLog(extra + Environment.NewLine + s + Environment.NewLine
                 + $"{DateTime.Now} Exception Type: {e.GetType()} => {e.Message} {Environment.NewLine}{e.StackTrace} ");
            }
        }

        internal async Task SendToLog(string msg)
        {
            if (msg.Length > 1800) msg = msg.Substring(0, 1800) + "...";
            else
                try
                {
                    switch (Program.clientCopy.CurrentUser.Id)
                    {
                        case 465565429645967398:
                            await GetChannel("Log", 610529221017600071, 201877884313403392)?.SendMessageAsync($"``` {msg} ```");
                            break;
                        case 535577053651664897:
                            await GetChannel("Log", 796556989811523596, 201877884313403392)?.SendMessageAsync($"``` {msg} ```");
                            break;
                    }
                }
                catch (Exception)
                {
                    if(!channels.TryGetValue("AdminDM", out IMessageChannel chan))
                    {
                        chan = await Program.clientCopy.GetUser(201875246091993088).GetOrCreateDMChannelAsync();
                        channels.Add("AdminDM", chan);
                    }
                    try { await chan.SendMessageAsync($"``` {msg} ```"); } catch (Exception) { }
                }
        }
    }
}
