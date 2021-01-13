using Discord;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AMI.Methods;
using AMYPrototype;
using Newtonsoft.Json;
using Discord.WebSocket;
using AMYPrototype.Commands;
using AMI.Neitsillia.User.UserInterface;

namespace AMI.AMIData.Servers
{
    [MongoDB.Bson.Serialization.Attributes.BsonIgnoreExtraElements]
    class GuildSettings
    {
        static MongoDatabase Database => Program.data.database;
        //private static Cache<ulong, GuildSettings> cache = new Cache<ulong, GuildSettings>();

        internal enum AssignedChannels { Main, Notification, Activity, Suggestion };

        [MongoDB.Bson.Serialization.Attributes.BsonId]
        public ulong guildID;
        public bool Ignore;
        public string prefix;
        public List<Channel> enabledChannels;
        public Channel mainChannel;
        public Channel notificationChannel;
        public Channel gameNotification;
        public Channel suggestionChannel;

        public long activityScore;
        public static string savePath = @".\Settings\Guilds\";

        public List<Tuple<ulong, int>> tiersRoles;

        //public List<Guid> dynasties;

        private SocketGuild _guild;
        internal SocketGuild Guild
        {
            get { return _guild ?? (_guild = Program.clientCopy.GetGuild(guildID)); }
            set { _guild = value is SocketGuild && value.Id == guildID ? value : Program.clientCopy.GetGuild(guildID); }
        }

        public static List<GuildSettings> LoadGuildSettings()
        {
            return Database.LoadRecords<GuildSettings>("Guilds");
        }
        public GuildSettings(IGuild g)
        {
            guildID = g.Id;
            //guildName = g.Name;
            enabledChannels = new List<Channel>();
            prefix = "~";
        }
        [JsonConstructor]
        public GuildSettings()
        { }
        internal static GuildSettings Load(SocketMessage s)
        {
            if (!(s.Channel is SocketGuildChannel chan))
                return null;
            return Load(chan.Guild);
        }

        public static GuildSettings Load(IGuild guild)
        {
            if (guild == null) return null;
            GuildSettings g = Load(guild.Id) ?? new GuildSettings(guild);
            g.Guild = (SocketGuild)guild;
            return g;
        }

        internal SocketTextChannel GetChannel(AssignedChannels t)
        {
            switch(t)
            {
                case AssignedChannels.Suggestion: return Guild.GetTextChannel(this.suggestionChannel.id);
            }
            return null;
        }

        public static GuildSettings Load(ulong id)
        {
            return Program.data.database.LoadRecord("Guilds", MongoDatabase.FilterEqual<GuildSettings, ulong>("guildID", id));

            //GuildSettings g = cache?.Load(id);
            //if(g == null && (g = ) != null)
            //    cache.Save(g.guildID, g);
            //return g;
        }
        public static GuildSettings LoadJSONGuild(string guildID)
        {
            return FileReading.LoadJSON<GuildSettings>(savePath + guildID);
        }
        public bool AddEnabledChannel(IChannel a)
        {
            for (int i = 0; i < enabledChannels.Count; i++)
            {
                if (enabledChannels[i].id == a.Id)
                    return false;
            }
            enabledChannels.Add(new Channel(a));
            return true;
        }
        public bool RemoveEnabledChannel(IChannel argChannel)
        {
            for (int i = 0; i < enabledChannels.Count; i++)
            {
                if (enabledChannels[i].id == argChannel.Id)
                {
                    enabledChannels.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }
        public async Task Notify(string message, EmbedBuilder embed)
        {
            await Guild.GetTextChannel(notificationChannel.id).SendMessageAsync(message, false, embed?.Build());
        }
        public void SaveSettings()
        {
            Program.data.database.UpdateRecord("Guilds",
                MongoDatabase.FilterEqual<GuildSettings, ulong>("guildID", guildID), this);
        }

        public EmbedBuilder GetInfo(SocketGuild guild = null)
        {
            Guild = guild;
            EmbedBuilder embed = DUtils.BuildEmbed(Guild.Name,
                $"User Count: {Guild.Users.Count} {Environment.NewLine}" +
                $"Role Count: {Guild.Roles.Count} {Environment.NewLine}" +
                $"Channel Count: {Guild.Channels.Count}", 
                Guild.Id.ToString(), Color.DarkRed,
                    DUtils.NewField("Bot Data",
                    $"Prefix: `{prefix}` {Environment.NewLine}" +
                    $"Main Channel: {mainChannel?.ToString() ?? "None"} {Environment.NewLine}" +
                    $"Bot Notification Channel: {notificationChannel?.ToString() ?? "None"} {Environment.NewLine}" +
                    $"Game Notification Channel: {gameNotification?.ToString() ?? "None"}{Environment.NewLine}" +
                    $"Suggestion Channel: {suggestionChannel?.ToString() ?? "None"}", true)
                );
            if(tiersRoles != null && tiersRoles.Count > 0)
            {
                string r = null;
                for(int i = 0; i < tiersRoles.Count; i++)
                {
                    var t = tiersRoles[i];
                    r += $"{i + 1} <@&{t.Item1}> : {t.Item2} pts {(i + 1 < tiersRoles.Count ? Environment.NewLine : null)}";
                }
                embed.AddField(DUtils.NewField("Server Memberships", r, true));
            }
            embed.WithThumbnailUrl(Guild.IconUrl);
            embed = Guild.SplashUrl != null ? embed.WithImageUrl(Guild.SplashUrl) : embed;
            return embed;
        }

        internal string UpdateTiers(IRole role, bool isAdd, int price)
        {
            if (tiersRoles == null) tiersRoles = new List<Tuple<ulong, int>>();

            if(isAdd)
            {
                if (tiersRoles.Find(x => x.Item1 == role.Id ) != null)
                    return "Role is already registered as tier.";
                tiersRoles.Add(new Tuple<ulong, int>(role.Id, price));
                SaveSettings();
                return "Role was added as tier " + tiersRoles.Count;
            }
            else
            {
                int i = tiersRoles.FindIndex(x => x.Item1 == role.Id);
                if (i < -1)
                {
                    tiersRoles.RemoveAt(i);
                    SaveSettings();
                    return "Role was removed from tiers";
                }
                return "Role is not currently registered as tier.";
            }
        }

        internal async Task<(string, Embed)> SendSuggestion(string content, IUser author, IGuild guild)
        {
            EmbedBuilder embed = DUtils.BuildEmbed(null,
                content, author.Id.ToString(), new Color(Program.rng.Next(256),
                Program.rng.Next(256), Program.rng.Next(256)),
                DUtils.NewField("Status", "Pending"));

            embed.WithAuthor(author);
            ITextChannel sendSuggestions = (ITextChannel)await guild.GetChannelAsync(suggestionChannel.id);
            Embed e = embed.Build();
            var reply = await sendSuggestions.SendMessageAsync(guildID == 637709809671471118 ? "<@&717444744812167239>" : null, embed: e);

            await reply.AddReactionsAsync(new[] { EUI.ToEmote(EUI.ok),
                EUI.ToEmote(EUI.cancel) });

            System.Threading.Thread.Sleep(500);

            await reply.ModifyAsync(x =>
            {
                x.Content = $"Use `{prefix}suggest` in any channel to make a suggestion to this server, " +
                $"or in my DMs to make a suggestion to my support server."
                + Environment.NewLine + $"Id: `{reply.Id}`";
            });

            return (reply.GetJumpUrl(), e);
        }
    }
}
