using AMI.AMIData.Servers;
using AMI.Module;
using AMI.Neitsillia.User;
using AMI.Neitsillia.User.PlayerPartials;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Diagnostics;

namespace AMI.Commands
{
    public class CustomSocketCommandContext : SocketCommandContext
    {
        internal GuildSettings guildSettings;
        internal string Prefix => guildSettings?.prefix ?? "~";

        private BotUser _botUser;
        internal BotUser BotUser => _botUser ??= BotUser.Load(User.Id);

        private Player loaded;
       

        internal Player Player
        {
            get
            {
                return loaded ??= Player.Load(BotUser);
            }
        }
        internal Player GetPlayer(Player.IgnoreException ie)
        {
            return loaded ??= Player.Load(BotUser, ie);
        }

        public string Content
        {
            get
            {
                int i = Message.Content.IndexOf(' ');
                return i > -1 ? Message.Content[i..] : null;
            }
        }

        public CustomSocketCommandContext(DiscordSocketClient client, SocketUserMessage msg)
            : base(client, msg)
        {

        }

        public bool WIPCheck()
        {
            switch(User.Id)
            {
                case 201875246091993088:
                    return true;
                default:
                    throw NeitsilliaError.ReplyError("Feature is work in progress");
            }
        }

        public bool AdminCheck()
        {
            switch (User.Id)
            {
                case 201875246091993088:
                    return true;
                default:
                    throw NeitsilliaError.ReplyError("You may not use this command");
            }
        }
    }
}
