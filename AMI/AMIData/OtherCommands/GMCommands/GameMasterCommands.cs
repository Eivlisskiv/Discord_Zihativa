using AMI.AMIData.Servers;
using AMI.Commands;
using AMI.Methods;
using AMI.Module;
using AMI.Neitsillia;
using AMI.Neitsillia.Areas.AreaPartials;
using AMI.Neitsillia.Collections;
using AMI.Neitsillia.Crafting;
using AMI.Neitsillia.Encounters;
using AMI.Neitsillia.Items.Abilities;
using AMI.Neitsillia.Items.Perks.PerkLoad;
using AMI.Neitsillia.NPCSystems;
using AMI.Neitsillia.Social.Mail;
using AMI.Neitsillia.User;
using AMI.Neitsillia.User.PlayerPartials;
using AMI.Neitsillia.User.UserInterface;
using AMYPrototype;
using AMYPrototype.Commands;
using Discord;
using Discord.Commands;
using Discord.Rest;
using AMI.Neitsillia.Items.ItemPartials;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace AMI.AMIData.OtherCommands
{
    public partial class GameMaster : ModuleBase<AMI.Commands.CustomSocketCommandContext>
    {
        internal static MongoDatabase Database => AMYPrototype.Program.data.database;
        //level 0; GMs wanna-be, low and always random, monthly limits
        //level 1; ^+ accepted GMs, get a decent monthly limit
        //level 2; ^+ higher limits
        //level 3; ^+ full game GM, can affect gameplay, no limits
        //level 4; ^+ Full Administrator

        //{Coins, xp, Item Tier Point, Mob Rank Points}
        public static readonly long[][] gmLimits = new long[][]
        {
            new long[] {100, 1000, 10, 10}, //0
            new long[] {3500, 10000, 100, 100}, //1
            new long[] {100000, 1000000, 1000, 1000}, //2
            new long[] {999999999, 999999999, 999999999}, //3
            new long[] {999999999, 999999999, 999999999}, //4
        };

        static readonly string gmPath = @".\Settings\GMs";
        List<GM> GMs { get => Program.data.gms; }

        #region Functions
        void LoadGMs()
        {
            Program.data.gms = new List<GM>();
            if (File.Exists(gmPath))
                Program.data.gms = FileReading.LoadJSON<List<GM>>(gmPath);
            GM user = GMs.Find(GM.FindWithID(201875246091993088));
            if (user == null)
            {
                GMs.Add(new GM(201875246091993088, "Uyhuri", 4));
                SaveGMFile();
            }
        }
        internal GM GetGM(ulong id) => GMs.Find(GM.FindWithID(id));

        internal async Task<bool> IsGMLevel(int level, ulong id = 0)
        {
            LoadGMs();
            //
            if (id == 0)
                id = Context.User.Id;
            GM user = GMs.Find(GM.FindWithID(id));
            if (user != null && (user.gmLevel == 4 || (user.gmLevel >= level)))
                return true;
            //
            await DUtils.Replydb(Context, "You may not perform this action");
            return false;
        }
        void AddGM(IUser user, int level)
        {
            ulong guildID = Context.Guild.Id;
            GM m = GMs.Find(GM.FindWithID(user.Id));
            if (m == null)
            {
                GMs.Add(new GM(user, level));
                ReplyAsync("GM <@" + user.Id + "> level " + level + " added");
                SaveGMFile();
            }
            else
                ReplyAsync("GM <" + user.Id + "> is already level " + level);
        }
        void SaveGMFile()
        {
            if (!Directory.Exists(".\\Settings"))
                Directory.CreateDirectory(".\\Settings");
            FileReading.SaveJSON(GMs, gmPath);
        }
        bool IsChannelManager()
        {
            return ((IGuildUser)Context.User).GuildPermissions.ManageChannels;
        }
        async Task<bool> HasFuel(long amount, int type, ulong id = 0)
        {
            if (id == 0)
                id = Context.User.Id;
            if (GMs == null)
                LoadGMs();
            //
            GM user = GMs.Find(GM.FindWithID(id));
            user.VerifyTimer();
            if (user == null)
                return false;
            else if ((user.limits[type] >= amount) || (user.gmLevel == 4 || user.gmLevel == 3))
                return true;
            else
            {
                TimeSpan untilReset = user.date.AddMonths(1) - DateTime.Now;
                await DUtils.Replydb(Context, $"The amount breaches your monthly limit. Limit: {user.limits[type]}" +
                    $" Reset in: {untilReset.Days}:{untilReset.Hours}:{untilReset.Minutes}");
            }
            return false;
        }
        #endregion

        #region Commands

        #region Get Info
        [Command("Mod GM")]
        public async Task ModGM(IUser user, int level)
        {
            ulong id = Context.User.Id;
            if (IsGMLevel(2, id).Result)
            {
                level = Verify.MinMax(level, 4, -1);
                GM target = GMs.Find(GM.FindWithID(user.Id));
                int currentLevel = -1;
                if (target != null)
                    currentLevel = target.gmLevel;
                if (user.Id == Context.User.Id)
                    await DUtils.Replydb(Context, "You may not change your own GM level");
                else if (IsGMLevel(level + 1, id).Result && IsGMLevel(currentLevel + 1, id).Result)
                {
                    string message;
                    if (target == null)
                    {
                        GMs.Add(new GM(user, level));
                        message = "New GM with server added";
                    }
                    else
                    {
                        target.ChangeLevel(level);
                        message = "GM level modified";
                    }
                    await DUtils.Replydb(Context, message);
                    SaveGMFile();
                }
            }
        }
        [Command("PlayerInfo")]
        public async Task PlayerInfo(string arg)
        {
            if (await IsGMLevel(4))
            {
                if (!ulong.TryParse(Regex.Replace(arg, "[<>!@]", ""), out ulong id))
                    throw NeitsilliaError.ReplyError("id invalid");
                IUser user = Context.Client.GetUser(id);
                if (user == null)
                    throw NeitsilliaError.ReplyError("User not found");
                BotUser buser = BotUser.Load(id);
                if (buser.dateInscription.Year == default && buser.loaded != null)
                {
                    buser.dateInscription = DateTime.UtcNow;
                    buser.Save();
                }
                var cl = BotUser.GetCharFiles(id);
                EmbedBuilder embed = DUtils.BuildEmbed(user.Username,
                    $"Member since {buser.dateInscription}"
                    + Environment.NewLine + $"Membership: {buser.membershipLevel}",
                    null,
                    new Color(),
                    DUtils.NewField("Characters",
                    cl != null && cl.Count > 0? string.Join(Environment.NewLine, cl) : "No Characters",
                    true),
                    DUtils.NewField("Discord Stats",
                    user.Username + "#" + user.Discriminator
                    + Environment.NewLine + $"Status: {user.Status}"
                    + Environment.NewLine + $"Created: {user.CreatedAt.Date.ToLongDateString()}"
                    ,
                    true)
                    );
                await ReplyAsync(embed: embed.Build());
            }
        }

        [Command("GM Help")]
        public async Task GMCommandHelp(int level = 1)
        {
            level = Verify.MinMax(level, 4, 0);
            if (IsGMLevel(level).Result)
                await DUtils.Replydb(Context, embed: GMHelp(level).Build());
        }
        EmbedBuilder GMHelp(int level)
        {
            EmbedBuilder help = new EmbedBuilder();
            help.WithTitle("Game Master Commands Level " + level);
            if (level == 0)
            {
                help.WithDescription("These GM are proving their skills.");
                help.AddField("Apprentice Game Master Tier",
                    "Currently no permissions." + Environment.NewLine);
            }
            else if (level == 1)
            {
                help.WithDescription("Newly accepted GMs");
                help.AddField("Server Game Master Tier", ""
                    + "~CMD Log ''Text'' |> Logs Text in the bot log file" + Environment.NewLine
                    );
            }
            if (level == 2)
            {
                help.WithDescription("Level 2 GMs can add and remove level 1 and lower GMs using: ~Mod GM @user ''GM level'' ");
                help.AddField("Semi Game Master Tier",
                     "~Area Population ''Name'' |> View the list of NPC in the specified Area" + Environment.NewLine
                    );
            }
            else if (level == 3)
            {
                help.WithDescription("Level 3 GMs can add, remove and modify level 2 and lower GMs using: ~Mod GM @user ''GM level''. " +
                    "They have no Grants monthly limits.");
                help.AddField("Full Game Master Tier",
                    "~Grant XP @user ''amount'' |> Gives amount of xp to @user" + Environment.NewLine
                    + "~Grant Coins @user ''amount'' |> Gives amount of Kutsyei coins to @user" + Environment.NewLine
                    + "~Grant Item @user ''item name'' |> Gives item to @user's inventory" + Environment.NewLine
                    + "~Grant Schematic @user ''item name'' |> Gives item's schematic to @user's schematics is item has a schematic" + Environment.NewLine
                    );
            }
            else if (level == 4)
            {
                help.WithDescription("Level 4 GMs can add, remove and modify level 0 to 4 GMs using: ~Mod GM @user ''GM level'' ");
                help.AddField("Admin Game Master Tier",
                    "~Download File ''path'' |> The bot will reply with the specified file" + Environment.NewLine
                    + "~Upload To ''path'' |> The bot will download the attachments to the path" + Environment.NewLine
                    + "~Wipe Area ''name'' |> The population of the specified Area will be reset to null" + Environment.NewLine
                    + "~New NPC ''Area'' #amount ''profession'' #level |> Adds #amount of ''profession'' NPC at #level into ''Area''" +
                    ", #amount default = 1, profession default = Child, #level default = 0|" + Environment.NewLine
                    + "~Animate Area ''Area'' #Turns |> For #Turns an NPC will act in ''Area'' every minute."
                    );
            }
            return help;
        }
        [Command("Display GMs")]
        public async Task DisplayGms()
        {
            string message = null;
            LoadGMs();
            foreach (GM g in GMs)
            {
                if (Context.Client.GetUser(g.id) != null || g.gmLevel == 4)
                    message += g.gmLevel + " | " + g.username + " |" +
                        "<@" + g.id + '>' + Environment.NewLine;
            }
            EmbedBuilder embed = new EmbedBuilder();
            embed.WithTitle("Current Server GMs and Level 4 GMs");
            embed.Description = message;
            await DUtils.Replydb(Context, embed: embed.Build());

        }
        [Command("GM Limits")]
        public async Task DisplayGMLimits()
        {
            if (IsGMLevel(0).Result)
            {
                GM g = GMs.Find(GM.FindWithID(Context.User.Id));
                EmbedBuilder e = new EmbedBuilder();
                e.WithTitle($"GM {g.username} Level {g.gmLevel}");
                e.AddField("Limits",
                    $"Coins: {g.limits[0]} / {gmLimits[g.gmLevel][0]} {Environment.NewLine}"
                    + $"XP: {g.limits[1]} / {gmLimits[g.gmLevel][1]} {Environment.NewLine}"
                    + $"Item Tier: {g.limits[2]} / {gmLimits[g.gmLevel][2]} {Environment.NewLine}"
                    //+ $"Mob Spawn: {g.limits[3]} / {gmLimits[g.gmLevel][3]} {Environment.NewLine}"  
                    );
                await DUtils.Replydb(Context, embed: e.Build());
            }
        }

        #endregion

        #region Server Management
        [Command("Disable Channel")]
        public async Task DisableChannel() => await EnableChannel(0);
        [Command("Enable Channel")]
        public async Task EnableChannel(int value = 1)
        {
            if (IsChannelManager() || IsGMLevel(4).Result)
            {
                if (value < 0 || value > 1)
                    await DUtils.Replydb(Context, "Incorrect value");
                else if (Context.Guild != null)
                {
                    GuildSettings guild = Context.guildSettings;
                    if (File.Exists(GuildSettings.savePath + guild.guildID))
                        guild = GuildSettings.LoadJSONGuild(Context.Guild.Id.ToString());


                    if (guild.enabledChannels.Count < 1 && value == 0)
                        await DUtils.Replydb(Context, "There are no channel restrictions");
                    if (value == 1)
                    {
                        if (!guild.AddEnabledChannel(Context.Channel))
                            await DUtils.Replydb(Context, "Channel already enabled.");
                        else
                        { guild.SaveSettings(); await DUtils.DeleteContextMessageAsync(Context); }
                    }
                    else
                    {
                        if (!guild.RemoveEnabledChannel(Context.Channel))
                            await DUtils.Replydb(Context, "Channel already disabled");
                        else
                        { guild.SaveSettings(); await DUtils.DeleteContextMessageAsync(Context); }
                    }
                }
                else await DUtils.Replydb(Context, "No Guild found.");
            }
        }
        internal static async Task<bool> VerifyChannel(CustomSocketCommandContext context, GuildSettings guildset)
        {
            try
            {
                if (context.Guild == null || guildset == null || guildset.enabledChannels == null) return true;

                context.guildSettings = guildset;

                guildset.SaveSettings();

                if (guildset.enabledChannels.Count < 1) return true;
                if (guildset.enabledChannels.FindIndex(c => c.id == context.Channel.Id) > -1)  return true;

                string content = context.Message.Content.ToLower()[(guildset.prefix ?? "~").Length..];
                if (content.StartsWith("setchannel")) return true;
                try
                {
                    if (guildset.mainChannel != null)
                        await DUtils.Replydb(context, "Channel disabled, main channel: " + " <#" + guildset.mainChannel.id + ">",
                            lifetime: 2);
                    else
                        await DUtils.Replydb(context, "Channel disabled, no main channel to display", lifetime: 0.0834);
                }
                catch (Exception) { }
                return false;
            }
            catch (Exception e) { Log.LogS(e); return true; }
        }

        [Command("SetChannel")]
        [Summary("Set the current channel as `channelType` for this server. Use SetChannel with no argument to view options.")]
        public async Task SetChannel(string channelType = null)
        {
            GuildSettings gset = Context.guildSettings ?? throw NeitsilliaError.ReplyError("Only server channels can be used for this command");
            if (IsChannelManager() || await IsGMLevel(4))
            {
                switch (channelType?.ToLower())
                {
                    case "enable":
                        await EnableChannel(1);
                        break;
                    case "disable":
                        await EnableChannel(0);
                        break;
                    case "suggestions":
                    case "suggestion":
                        {
                            gset.suggestionChannel = new Channel(Context.Channel);
                            gset.SaveSettings();
                            await DUtils.Replydb(Context, "Server suggestions will be sent here!");
                        }
                        break;
                    case "main":
                        {
                            gset.mainChannel = new Channel(Context.Channel);
                            gset.SaveSettings();
                            await DUtils.Replydb(Context, "Channel set!");
                        }
                        break;
                    case "sub":
                    case "subscription":
                        {
                            gset.notificationChannel = new Channel(Context.Channel);
                            gset.SaveSettings();
                            await DUtils.Replydb(Context, "This channel will be the one to receive Neitsillia notifications!");
                            
                        }
                        break;
                    case "activity":
                        {
                            gset.gameNotification = new Channel(Context.Channel);
                            gset.SaveSettings();
                            await DUtils.Replydb(Context, "This channel will be the one to receive Neitsillia activity reports!");
                        }break;

                    default:
                        await DUtils.Replydb(Context, "Channel type is invalid. Options are:"
                            + Environment.NewLine + "SetChannel Main"
                            + Environment.NewLine + "SetChannel Sub"
                            + Environment.NewLine + "SetChannel Activity"
                            + Environment.NewLine + "SetChannel Suggestions"
                            + Environment.NewLine + "SetChannel Enable"
                            + Environment.NewLine + "SetChannel Disable"
                            );
                        break;
                }
            }
        }
        #endregion

        [Command("SuggestionResponse", true)][Alias("Suggestres")]
        public async Task SuggestionResponse(ulong msgid, [Remainder] string message)
        {
            GuildSettings gset = null;
            if (Context.Guild == null && await IsGMLevel(3))
                gset = GuildSettings.Load(201877884313403392);
            else if (Context.Guild.GetUser(Context.User.Id).GuildPermissions.ManageMessages || await IsGMLevel(3))
                gset = GuildSettings.Load(Context.Guild);

            if(gset != null)
            {
                await ReplyAsync(
                await Program.data.UpdateSuggestion(gset,
                    msgid, message));
            }
        }
        [Command("BugResponse", true)] [Alias("bugres")]
        public async Task BugReportResponse(ulong msgid, [Remainder] string message)
        {
            Context.AdminCheck();
            await ReplyAsync(
                await Program.data.UpdateBugReport(msgid, message));
        }

        //Events
        [Command("TriggerPuzzle")]
        public async Task TriggerPuzzle(IUser user, string name = "~Random", int type = 0, string reward = "~Random")
        {
            if (await IsGMLevel(3))
            {
                Player player = Player.Load(user.Id);
                player.NewEncounter(new Encounter(Encounter.Names.Puzzle, player, 
                    $"{name};{Verify.MinMax(type, Enum.GetValues(typeof(Puzzle.Reward)).Length - 1)};{reward}"));
                await player.NewUI("You've encountered a puzzle. What lays behind?", player.Encounter.GetEmbed().Build(), Context.Channel, MsgType.Puzzle);
            }
            await DUtils.DeleteContextMessageAsync(Context);
        }

        [Command("Send Mail")]
        public async Task SendMail(IUser user, string subject, string body, int kuts = 0, params string[] items)
        {
            Mail mail = new Mail(user.Id, subject, body, kuts);

            if(items != null && items.Length > 0)
            {
                mail.content = new Inventory();
                for (int i = 0; i < items.Length; i++)
                {
                    string[] data = items[i].Split('x', 'X', '*');
                    string[] itemd;
                    if (!int.TryParse(data[0], out int count))
                    {
                        count = 1;
                        itemd = data[0].Split(';');
                    }
                    else itemd = data[1].Split(';');

                     
                    Item item = Item.LoadItem(itemd[0].Replace('_', ' '));
                    if (item != null) 
                    {
                        if (items.Length > 1 && int.TryParse(itemd[1], out int tier))
                            item.Scale(tier);

                        mail.content.Add(item, count, -1);
                    }
                }
            }

            await mail.Save();
        }

        #region Message Manipulation
        [Command("Send Message")]
        public async Task SendMessage(ulong guildID, ulong channelID, string message)
        {
            if (IsGMLevel(4).Result)
                await Handlers.DiscordBotHandler.Client.GetGuild(guildID).GetTextChannel(channelID).SendMessageAsync(message);
            await DUtils.DeleteContextMessageAsync(Context);
        }
        [Command("Notify", true)]
        public async Task Notify(string notification, [Remainder] string message)
        {
            if (IsGMLevel(4).Result)
            {
                EmbedBuilder noti;
                switch (notification)
                {
                    case "patch-notes":
                    case "patch-note":
                    case "patchnote":
                    case "patchnotes":
                    case "patch note":
                    case "patch notes":
                            noti = Other.UpdateLog();
                        break;

                    default:
                        if (message.Length == 0) throw NeitsilliaError.ReplyError("There is no content to write in this notification");
                        noti = DUtils.BuildEmbed(notification, message); 
                        break;
                }
                //
                noti.WithFooter("~Help Server |> To view server settings commands");
                await ReplyAsync($"Sent to {(await SendToSubscribed(notification, noti?.Build())).Count} Subbed server channels.");
            }
        }
        public static async Task<List<RestUserMessage>> SendToSubscribed(string notification, Embed embed = null)
        {
            List<GuildSettings> gs = GuildSettings.LoadGuildSettings();
            List<RestUserMessage> msgs = new List<RestUserMessage>();
            int sentTo = 0;
            foreach (GuildSettings g in gs)
            {
                if (g != null && g.notificationChannel != null && g.notificationChannel.id != 0)
                {
                    try
                    {
                        if (g.Guild != null)
                        {
                            var chan = g.Guild.GetTextChannel(g.notificationChannel.id);
                            if (chan != null)
                            {
                                msgs.Add(await chan.SendMessageAsync(notification, false, embed));
                                sentTo++;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Log.LogS(e);
                    }
                }
            }
            Console.WriteLine("Notification sent to " + sentTo + " server's channels");
            return msgs;
        }
        public static async Task ReportActivityToServers(string notification, EmbedBuilder noti)
        {
            List<GuildSettings> gs = GuildSettings.LoadGuildSettings();
            int sentTo = 0;
            foreach (GuildSettings g in gs)
            {
                if (g != null && g.gameNotification != null && g.gameNotification.id != 0)
                    try
                    {
                        if (noti != null)
                            await Program.clientCopy.GetGuild(g.guildID).GetTextChannel(g.gameNotification.id)
                              .SendMessageAsync(notification, false, noti.Build());
                        else
                            await Program.clientCopy.GetGuild(g.guildID).GetTextChannel(g.gameNotification.id)
                          .SendMessageAsync(notification);
                        sentTo++;
                    }
                    catch (Exception e) { Log.LogS(e); }
            }
            Console.WriteLine("Notification sent to " + sentTo + " server's channels");
        }
        [Command("DeleteMessage")]
        public async Task DeleteMessage(string messageURL)
        {
            if(await IsGMLevel(4))
            {
                string[] copms = messageURL.Replace("https://discordapp.com/channels/", "").Split('/');

                if(!ulong.TryParse(copms[0], out ulong guildID)) guildID = Context.Channel.Id;

                ulong chanID = ulong.Parse(copms[1]);
                ulong msgID = ulong.Parse(copms[2]);
                var m = await Program.clientCopy.GetGuild(guildID).GetTextChannel(chanID).GetMessageAsync(msgID);
                await m.DeleteAsync();
            }
        }
        #endregion

        #region NPC Manipulation
        [Command("AnimRate")]
        public void AnimRate(int seconds)
        {
            Context.AdminCheck();

            PopulationHandler.SetPerSecond(seconds);
        }

        [Command("New NPC")]
        public async Task New_NPC(string areaName, int amount = 1, string profession = "Child", int level = 0)
        {
            if (IsGMLevel(4).Result)
            {
                //areaName = Area.AreaDataExist(areaName);
                if (areaName != null)
                {
                    NPC[] npcs = new NPC[amount];
                    for (int i = 0; i < amount; i++)
                        npcs[i] = NPC.NewNPC(level, profession, null);

                    Area area = Area.LoadFromName(areaName);
                    string populationId = area.GetPopulation(Neitsillia.Areas.AreaExtentions.Population.Type.Population)._id;

                    EmbedBuilder noti = new EmbedBuilder();
                    noti.WithTitle($"{area.name} Population");
                    amount = 0;
                    if (npcs != null && npcs.Length > 0)
                    {
                        foreach (NPC n in npcs)
                        {
                            if (n != null)
                            {
                                if (n.profession == ReferenceData.Profession.Child)
                                {
                                    if (area.parent == null)
                                        n.origin = area.name;
                                    else
                                        n.origin = area.parent;
                                    n.displayName = n.name + " Of " + n.origin;
                                }
                                else
                                    n.displayName = n.name;
                                PopulationHandler.Add(populationId, n);
                                amount++;
                            }
                        }
                    }

                    if (amount != 0) await ReplyAsync("NPCs created");
                    else await ReplyAsync("No new NPC were created");
                }
                else await DUtils.Replydb(Context, "Area not found.");
            }
        }
        [Command("New Bounty")]
        public async Task New_Bounty(string areaName, string creatureName = null, int floor = 0, int level = 1, string grantDrop = null)
        {
            if (IsGMLevel(4).Result)
            {
                areaName = StringM.UpperAt(areaName);
                //areaName = Area.AreaDataExist(areaName);
                if (areaName != null)
                {
                    Area area = Area.LoadFromName(areaName);
                    floor = Verify.MinMax(floor, area.floors);
                    //
                    NPC mob = null;
                    if (creatureName != null)
                    {
                        mob = NPC.GenerateNPC(Verify.Min(level, 0),
                            StringM.UpperAt(creatureName));
                    }
                    if (mob == null)
                        mob = area.GetAMob(Program.rng, floor);
                    //
                    if (grantDrop != null)
                    {
                        Item grant = Item.LoadItem(grantDrop);
                        if (grant != null)
                            mob.AddItemToInv(grant, 1, true);
                    }
                    
                    //
                    PopulationHandler.Add(area, mob);

                    await DUtils.DeleteContextMessageAsync(Context);
                }
                else await DUtils.Replydb(Context, "Area not found.");
            }
        }
        [Command("Encounter Bounty")]
        public async Task EncounterBounty()
        {
            if(await IsGMLevel(4))
            {
                Player player = Player.Load(Context.BotUser);
                await ReplyAsync(embed:player.Area.ForceBountyEncounter(player).Build());
                player.SaveFileMongo();
            }
        }
        #endregion

        [Command("ModEvent")]
        public async Task ManageEvent(string action, string eventName = null, int length = 5)
        {
            if (!await IsGMLevel(4)) return;

            switch (action?.ToString())
            {
                case "calendar":
                    {

                    }break;
                case "stop": 
                    if(Events.OngoingEvent.Ongoing == null) { await ReplyAsync("No current event"); return; }
                    Events.OngoingEvent.Ongoing.endTime = DateTime.UtcNow;
                    await Events.OngoingEvent.StartWait();
                    break;
                case "start":
                    if(eventName == null) { await ReplyAsync("No event name given"); return; }
                    await Events.OngoingEvent.StartUnscheduledEvent(eventName, length);
                    break;
                case "extend":
                    await ReplyAsync(Events.OngoingEvent.ExtendOngoing(length));
                    break;
            }
        }

        #region Bot Settings and Info
        [Command("Bot Info")]
        public async Task GetBotInfo(string props)
        {
            if (IsGMLevel(4).Result)
            {
                var client = Context.Client; //Program.clientCopy;
                EmbedBuilder embed = new EmbedBuilder();
                embed.WithTitle(client.CurrentUser.Username + " Info");

                if (Program.data.activity == null)
                    Program.data.LoadActivity();

                switch (props.ToLower())
                {
                    case "activity":
                        {
                            Program.data.activity.activeUsers.Sort((x, y) => y.count.CompareTo(x.count));
                            Program.data.activity.Save();

                            EmbedBuilder e = new EmbedBuilder();
                            e.AddField(Program.data.activity.ToString(), Program.data.activity.ListTopUsers(5));
                            var top = PlayerActivity.LoadTop();
                            if (top != null)
                                e.AddField(top.ToString(), top.ListTopUsers(5));
                            await DUtils.Replydc(Context, embed: e.Build());
                        }
                        break;
                    case "servers":
                    case "server":
                        {
                            new Thread(async () => {

                                var dbl = Program.dblAPI;
                                int privateGuildsCount = client.Guilds.Count;
                                string list = "";
                                foreach (Discord.WebSocket.SocketGuild a in client.Guilds)
                                {
                                    var result = dbl.IsListedServer(a.Id);
                                    if (result.Item2)
                                    {
                                        list += $"{a.Name} ({result.Item1 ?? a.Id.ToString()})" + Environment.NewLine;
                                        privateGuildsCount--;
                                    }
                                    if (list.Length >= 1000)
                                    {
                                        await ReplyAsync(list);
                                        list = "";
                                    }
                                }
                                await ReplyAsync((list.Length == 0 ? "No Public Server Found" : list) + Environment.NewLine + 
                                    $"And {privateGuildsCount} Private Guilds. For a total of {client.Guilds.Count}");

                            }).Start();
                        }
                        break;
                    default:
                        {
                            EmbedBuilder e = new EmbedBuilder();

                            e.WithTitle(client.CurrentUser.Username);
                            e.WithDescription(
                                "Servers: " + client.Guilds.Count + Environment.NewLine +
                                "Activity: "+ Program.data.activity.total);


                            await DUtils.Replydb(Context, embed: e.Build());
                        }break;
                }
            }
        }
        [Command("IgnoreServer")]
        public async Task IgnoreServer(string serverId, bool ignore = true)
        {
            if (await IsGMLevel(4))
            {
                string path = $"Settings\\Guilds\\{serverId}";
                if (File.Exists(path))
                {
                    GuildSettings set = GuildSettings.LoadJSONGuild(serverId);
                    set.Ignore = ignore;
                    await ReplyAsync("Server Ignore set to: " + ignore);
                }
                else await ReplyAsync("No server saved with that Id.");
            }
        }
        [Command("Maintenance")]
        public async Task Maintenance(string option, int hourseta = 2)
        {
            if (await IsGMLevel(4))
            {
                switch (option?.ToLower())
                {
                    case "saves": await SaveFilesMaintenance(Context.Channel); break;
                    case "server":
                        await SendToSubscribed("Server Maintenance", DUtils.BuildEmbed("Incoming maintenance",
                            "The bot will be down for maintenance." + Environment.NewLine
                            + $"EDT: {hourseta} Hours", null, Color.DarkBlue).Build());
                        await Program.Exit("Shutting for Maintenance");
                        break;
                }
            }
        }
        internal static async Task SaveFilesMaintenance(Discord.WebSocket.ISocketMessageChannel chan)
        {
            Program.SetState(Program.State.Paused);
            await SendToSubscribed("[Zihativa] is now disabled for character files maintenance.");
            List<Player> players = await Database.LoadRecordsAsync<Player>("Character");
            foreach (Player player in players)
            {
                Thread.Sleep(1000);
                try
                {
                    await GameCommands.FixFile(player, chan);
                }
                catch (Exception e) { await chan.SendMessageAsync($"Error with {player._id} : {e.Message}"); }
            }
            Program.SetState(Program.State.Ready);
            await SendToSubscribed("[Zihativa] is now enabled.");
        }
        #endregion

        #endregion
    }
}
