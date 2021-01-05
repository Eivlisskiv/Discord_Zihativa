using AMI.AMIData.Servers;
using AMI.Commands;
using AMI.Methods;
using AMI.Module;
using AMI.Neitsillia;
using AMI.Neitsillia.Areas;
using AMI.Neitsillia.Areas.AreaPartials;
using AMI.Neitsillia.Crafting;
using AMI.Neitsillia.Encounters;
using AMI.Neitsillia.Items;
using AMI.Neitsillia.Items.Perks.PerkLoad;
using AMI.Neitsillia.NPCSystems;
using AMI.Neitsillia.User;
using AMI.Neitsillia.User.PlayerPartials;
using AMI.Neitsillia.User.UserInterface;
using AMYPrototype;
using AMYPrototype.Commands;
using Discord;
using Discord.Commands;
using Discord.Rest;
using Neitsillia.Items.Item;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace AMI.AMIData.OtherCommands
{
    public class GameMasterCommands : ModuleBase<AMI.Commands.CustomSocketCommandContext>
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
        internal GM GetGM(ulong id)
        { return GMs.Find(GM.FindWithID(201875246091993088)); }
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
        [Command("Full Sheet")]
        [Alias("Full Stats", "fstats", "fs")]
        public async Task Full_Sheet(string arg = null)
        {
            if (arg == null || await IsGMLevel(1))
            {
                Player player = null;
                if (arg != null)
                {
                    player = Player.Load(arg);
                    if (player != null) { }
                    else if (ulong.TryParse(arg, out ulong id))
                        player = Player.Load(id);
                    else if (ulong.TryParse(arg.Substring(2).Trim('>'), out id))
                        player = Player.Load(id);
                }
                if (player == null)
                    player = Player.Load(Context.User.Id, Player.IgnoreException.All); ;
                string path = $"Temp/{player.userid}-{player.name} Full Stats.txt";
                StreamWriter stats = new StreamWriter(path);
                //
                stats.WriteLine(player.FullInfo());
                //
                stats.Close();
                await Context.Channel.SendFileAsync(path);
                File.Delete(path);
                await Context.Message.DeleteAsync();
            }
        }
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
                    string message = "No Changes were made, unwanted result;";
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

        [Command("SimulateLotteryEnd"), Alias("SimLottery")]
        public async Task SimulateLotteryEnd()
        {
            if(await IsGMLevel(4))
            {
                var lottery = Program.data.lottery;
                await ReplyAsync(lottery.GetWinner(out Player p));
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

                string content = context.Message.Content.ToLower().Substring((guildset.prefix ?? "~").Length);
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
        public async Task SuggestionResponse(ulong msgid)
        {
            GuildSettings gset = null;
            if (Context.Guild == null && await IsGMLevel(3))
                gset = GuildSettings.Load(201877884313403392);
            else if (Context.Guild.GetUser(Context.User.Id).GuildPermissions.ManageMessages || await IsGMLevel(3))
                gset = GuildSettings.Load(Context.Guild);

            if(gset != null)
            {
                string content = Context.Message.Content;
                await ReplyAsync(
                await Program.data.UpdateSuggestion(gset,
                    msgid, content.Substring(content.IndexOf(' ', content.IndexOf(' ') + 1) + 1) 
                    ));
            }
        }
        [Command("BugResponse", true)] [Alias("bugres")]
        public async Task BugReportResponse(ulong msgid)
        {
            Context.AdminCheck();
            string content = Context.Message.Content;
            await ReplyAsync(
                await Program.data.UpdateBugReport(msgid, 
                    content.Substring(content.IndexOf(' ', content.IndexOf(' ') + 1) + 1)
                    ));
        }

        #region Cheats

        [Command("Grant XP")]
        [Alias("grantx")]
        public async Task GrantXP(IUser user, long xp)
        {
            if (HasFuel(xp, 1).Result)
            {
                Player player = Player.Load(user.Id, Player.IgnoreException.All);
                player.XPGain(xp);
                IUserMessage reply = await ReplyAsync(user.Mention + " gained " + xp + " XP points.");
                player.ui = new UI(reply, new List<string> { EUI.xp, EUI.inv, EUI.stats });
                player.SaveFileMongo();
                await DUtils.DeleteContextMessageAsync(Context);
                GMs.Find(GM.FindWithID(Context.User.Id)).limits[1] -= xp;
                SaveGMFile();
            }
        }
        [Command("Grant Coins")]
        [Alias("grantc")]
        public async Task GrantCoins(IUser user, long amount)
        {
            if (HasFuel(amount, 0).Result)
            {
                Player player = Player.Load(user.Id, Player.IgnoreException.All);
                player.KCoins += amount;
                player.SaveFileMongo();
                IUserMessage reply = await ReplyAsync(user.Mention + " gained " + amount + " Kutsyei coins.");
                player.ui = new UI(reply, new List<string> { EUI.xp, EUI.inv, EUI.stats });
                player.SaveFileMongo();
                await DUtils.DeleteContextMessageAsync(Context);
                GMs.Find(GM.FindWithID(Context.User.Id)).limits[0] -= amount;
                SaveGMFile();
            }
            await DUtils.DeleteContextMessageAsync(Context);
        }
        [Command("Grant Item")]
        [Alias("granti")]
        public async Task GrantItem(IUser user, string itemname, int amount = 1)
        {
            Item item = Item.LoadItem(itemname);
            if (item == null)
                await ReplyAsync("Item Not Found");
            else if (await HasFuel(item.tier, 2))
            {
                Player player = Player.Load(user.Id, Player.IgnoreException.All);
                string result = $"Could not collect {item.name}";
                if (player.CollectItem(item, amount))
                    result = $"Collected {item.name}";
                IUserMessage reply = await ReplyAsync(user.Mention + " " + result);
                player.ui = new UI(reply, new List<string> { EUI.xp, EUI.inv, EUI.stats });
                player.SaveFileMongo();
                await DUtils.DeleteContextMessageAsync(Context);
                GMs.Find(GM.FindWithID(Context.User.Id)).limits[2] -= item.tier;
                SaveGMFile();
            }
            await DUtils.DeleteContextMessageAsync(Context);
        }
        [Command("GrantUsable")]
        public async Task GrantUsable(IUser user, string usable, int level = 1, int amount = 1)
        {
            if (await HasFuel(level * 20, 2))
            {
                Player player = Player.Load(user.Id, Player.IgnoreException.All);
                switch(usable.ToLower())
                {
                    case "repairkit":
                    case "kit":
                        player.inventory.Add(Item.CreateRepairKit(level), amount, -1);
                        break;
                    case "rune":
                    case "runes":
                        player.inventory.Add(Item.CreateRune(level), amount, -1);
                        break;
                    default:
                        throw NeitsilliaError.ReplyError(string.Join(Environment.NewLine, "RepairKit, Kit", "Rune, Runes"));
                        
                }
                player.SaveFileMongo();
                await ReplyAsync($"Gave {player.name} {amount}x {usable}");
            }
        }
        [Command("Grant Upgraded Gear"), Alias("grantug")]
        public async Task GrantUpgradedGear(IUser user, int tier, params string[] namearg)
        {
            if (await IsGMLevel(4))
            {
                Item item = Item.LoadItem(ArrayM.ToUpString(namearg));
                if(tier > item.tier)
                    item.Scale(tier);

                Player player = Player.Load(user.Id, Player.IgnoreException.All);
                string result = $"Could not collect {item.name}";
                if (player.CollectItem(item, 1))
                    result = $"Collected {item.name}";
                IUserMessage reply = await ReplyAsync(user.Mention + " " + result);
                player.ui = new UI(reply, new List<string> { EUI.xp, EUI.inv, EUI.stats });
                player.SaveFileMongo();
                await DUtils.DeleteContextMessageAsync(Context);
            }
        }
        [Command("Grant Schematic")]
        [Alias("grants")]
        public async Task Grant_Schem(IUser user, params string[] argName)
        {
            if (await IsGMLevel(3))
            {
                string schemName = StringM.UpperAt(ArrayM.ToString(argName));
                Item item = Item.LoadItem(schemName);
                if (item.type != Item.IType.notfound)
                {
                    if (item.schematic.exists)
                    {
                        Player player = Player.Load(user.Id, Player.IgnoreException.All);
                        if (player.schematics == null)
                            player.schematics = new List<Schematic>();
                        if (player.schematics.FindIndex(Schematic.FindWithName(item.schematic.name)) == -1)
                        {
                            player.schematics.Add(item.schematic);
                            player.SaveFileMongo();
                            IUserMessage reply = await ReplyAsync(user.Mention + " received a " + schemName + " schematic");
                            player.ui = new UI(reply, new List<string> { EUI.xp, EUI.inv, EUI.stats, EUI.schem });
                            player.SaveFileMongo();
                            await DUtils.DeleteContextMessageAsync(Context);
                        }
                        else await DUtils.Replydb(Context, "Player already has this schematic");
                    }
                    else await DUtils.Replydb(Context, "Item does not have a schematic");

                }
                else await DUtils.Replydb(Context, "Item not found");
            }
        }
        [Command("Grant Temporary Schematic")]
        [Alias("grantts")]
        public async Task GrantTempSchem(IUser user, params string[] argName)
        {
            if (await IsGMLevel(3))
            {
                Item item = Item.NewTemporarySchematic(ArrayM.ToString(argName));
                if (item == null)
                    await ReplyAsync("Item Not Found");
                else if (HasFuel(item.tier, 2).Result)
                {
                    Player player = Player.Load(user.Id);
                    string result = $"Could not collect {item.name}";
                    if (player.CollectItem(item, 1))
                        result = $"Collected {item.name}";
                    IUserMessage reply = await ReplyAsync(user.Mention + " " + result);
                    player.ui = new UI(reply, new List<string> { EUI.xp, EUI.inv, EUI.stats });
                    player.SaveFileMongo();
                    await DUtils.DeleteContextMessageAsync(Context);
                    GMs.Find(GM.FindWithID(Context.User.Id)).limits[2] -= item.tier;
                    SaveGMFile();
                }
                await DUtils.DeleteContextMessageAsync(Context);
            }
        }
        [Command("Grant Ability")]
        [Alias("granta")]
        public async Task Grant_Ability(IUser user, string argName)
        {
            if (IsGMLevel(3).Result)
            {
                string abName = StringM.UpperAt(argName);
                Ability a = Ability.Load(abName);
                Player p = Player.Load(user.Id, Player.IgnoreException.All);
                if (p.HasAbility(a.name, out _))
                {
                    p.abilities.Add(a);
                    p.SaveFileMongo();
                    await ReplyAsync($"{p.name} learned {a.name}");
                }
                else await ReplyAsync($"{p.name} already knows {a.name}");
            }
        }
        [Command("RemoveAbility")]
        public async Task RemoveAbility(IUser user, int index)
        {
            if(await IsGMLevel(4))
            {
                Player player = Player.Load(user.Id, Player.IgnoreException.All, true);
                if (index < 0 || index > player.abilities.Count)
                    NeitsilliaError.ReplyError("Invalid index");
                player.abilities.RemoveAt(index);
                player.SaveFileMongo();
            }
        }
        [Command("Grant Ability XP")]
        [Alias("grantax")]
        public async Task Grant_Ability_XP(IUser user, string argName, long xp = 100)
        {
            if (IsGMLevel(3).Result)
            {
                string abName = StringM.UpperAt(argName);
                Player p = Player.Load(user.Id, Player.IgnoreException.All);
                if (p.HasAbility(abName, out int i))
                {
                    p.abilities[i].GainXP(xp, 1);
                    p.SaveFileMongo();
                    await DUtils.DeleteContextMessageAsync(Context);
                    await ReplyAsync(embed: p.abilities[i].InfoPage(p.UserEmbedColor(), true).Build());
                }
                else await DUtils.Replydb(Context, $"{p.name} already does not know {argName}", lifetime: 0.5);
            }
        }
        [Command("Grant Perk")]
        [Alias("grantp")]
        public async Task Grant_Perk(IUser user, params string[] argName)
        {
            if (await IsGMLevel(3))
            {
                string perkName = StringM.UpperAt(ArrayM.ToString(argName));
                Player p = Player.Load(user.Id, Player.IgnoreException.All);
                int i = p.HasPerk(perkName);
                if (i == -1)
                {
                    p.perks.Add(PerkLoad.Load(perkName));
                    p.SaveFileMongo();
                    await DUtils.DeleteContextMessageAsync(Context);
                }
                else await DUtils.Replydb(Context, $"{p.name} already has the perk {perkName}", lifetime: 0.5);
            }
        }
        [Command("Give Skill Point")]
        public async Task Give_SKill_Point(IUser user, int amount = 1)
        {
            if (await IsGMLevel(4))
            {
                Player player = Player.Load(user.Id, Player.IgnoreException.All);
                player.skillPoints += amount;
                player.SaveFileMongo();
            }
        }
        [Command("Give Spec Point")]
        [Alias("grantspecp")]
        public async Task Give_Spec_Point(IUser user, int amount = 1)
        {
            if (await IsGMLevel(4))
            {
                Player player = Player.Load(user.Id, Player.IgnoreException.All);
                if (player.Specialization != null)
                {
                    player.Specialization.specPoints += amount;
                    player.SaveFileMongo();
                }
            }
        }
        [Command("Grant Building Schem")]
        [Alias("grantbs")]
        public async Task GrantBuildingSchem(IUser user, params string[] args)
        {
            if (await IsGMLevel(4))
            {
                Player player = Player.Load(user.Id, Player.IgnoreException.All);
                Item item = Item.BuildingSchematic(ArrayM.ToUpString(args));
                player.inventory.Add(item, 1, -1);
                player.SaveFileMongo();
                await DUtils.DeleteContextMessageAsync(Context);
            }
        }
        [Command("GrantEgg")][Alias("grante")]
        public async Task Grant_Egg(IUser user, int tier = 0)
        {
            if (await IsGMLevel(4))
            {
                Player player = Player.Load(user.Id, Player.IgnoreException.All);
                if (player.EggPocket == null)
                    await ReplyAsync("Player has no egg pocket");
                else
                    await player.EggPocket.EquippEgg(Neitsillia.NPCSystems.Companions.EggTypes
                        .GetEgg(tier), player, Context.Channel);
            }
        }
        [Command("HatchEgg")]
        public async Task Grant_Pet(IUser user)
        {
            if (await IsGMLevel(4))
            {
                Player player = Player.Load(user.Id, Player.IgnoreException.All);
                if (player.EggPocket?.egg == null)
                    await ReplyAsync("Player has no egg");
                else
                    player.EggPocket.Hatch(player);
            }
        }

        [Command("ApplyPerk")]
        public async Task ApplyPerk(int index, string perkName)
        {
            if (await IsGMLevel(4))
            {
                Item item = Context.Player.inventory.GetItem(index - 1);
                if (item != null && item.CanBeEquip())
                {
                    item.perk = PerkLoad.Load(perkName);
                    Context.Player.SaveFileMongo();
                    await ReplyAsync("Perks replaced");
                }
                else await ReplyAsync("item is not gear");
            }
        }

        #endregion

        #region Area Cheats
        [Command("Move To Area")]
        [Alias("mtarea")]
        public async Task MoveToArea(IUser user, params string[] argName)
        {
            if (await IsGMLevel(4))
            {
                Player player = Player.Load(user.Id);
                Area area = Area.LoadFromName(StringM.UpperAt(ArrayM.ToString(argName)));
                player.EndEncounter();
                await player.SetArea(area);
                player.SaveFileMongo();
                EmbedBuilder areaInfo = player.UserEmbedColor(player.Area.AreaInfo(player.areaPath.floor));
                await player.NewUI(await Context.Channel.SendMessageAsync("You've entered " + player.Area.name, embed: areaInfo.Build())
                , MsgType.Main);
            }
        }
        [Command("Generate Dungeon")]
        [Alias("GenDungeon")]
        public async Task GenerateDungeon(IUser user, params string[] args)
        {
            if (await IsGMLevel(3))
            {
                Player player = Player.Load(user.Id);
                Area dungeon = null;
                if (args.Length > 0)
                    dungeon = Dungeons.ManualDungeon(StringM.UpperAt(ArrayM.ToString(args)), player.areaPath.floor, player.Area);
                else dungeon = Dungeons.Generate(player.areaPath.floor, player.Area);
                if (dungeon != null)
                {
                    await player.SetArea(dungeon);
                    await player.NewUI(await ReplyAsync(embed: dungeon.AreaInfo(player.areaPath.floor).Build()), MsgType.Main);
                }
                else await ReplyAsync("Dungeon not Found");
            }
        }
        [Command("AutoInvade")]
        public async Task AutoInvade(IUser user = null)
        {
            if (await IsGMLevel(4))
            {
                if (user == null)
                    user = Context.User;
                Player player = Player.Load(user.Id);
                if (player.Area.type == AreaType.Stronghold)
                {
                    player.Area.sandbox.Captured(user.Id);
                    await player.Area.UploadToDatabase();
                    await ReplyAsync("Completed");
                }
                else await ReplyAsync("Area is not a Stronghold");
            }
        }
        [Command("AddChildArea")]
        public async Task AddChildArea_CMD(string childType, string childName, string arguments = null)
        {
            if(await IsGMLevel(4))
                await ReplyAsync(AddChildArea(childType, childName, arguments));
        }
        string AddChildArea(string childType, string childName, string arguments)
        {
            Area parent = Player.Load(Context.BotUser, Player.IgnoreException.All).Area;
            Area child = null;
            switch (childType.ToLower())
            {
                case "tavern":  child = ChildrenArea.Tavern(parent, childName, true); break;
                case "arena":   child = ChildrenArea.Arena(parent, childName, arguments, true);break;
                case "petshop": child = ChildrenArea.PetShop(parent, childName, true);break;
                case "shrine":  child = ChildrenArea.Shrine(parent, childName, true); break;
                default:
                    return "Child Area Type Invalid" + Environment.NewLine
                        + "Tavern" + Environment.NewLine
                        + "Arena" + Environment.NewLine
                        + "PetShop" + Environment.NewLine
                        ;
                    
            }
            return $"Added {child} {child.type} in {parent}";
        }

        [Command("Add Junction")]
        public async Task AddJunction(string fromId, string toId, int floor = 0, int returnFloor = 0)
        {
            if (await IsGMLevel(4))
            {
                Area from = Area.LoadFromName(fromId);
                Area to = Area.LoadFromName(toId);

                from.junctions = from.junctions ?? new List<NeitsilliaEngine.Junction>();
                int index = from.junctions.FindIndex(NeitsilliaEngine.Junction.FindName(to.name));
                if (index == -1)
                    from.junctions.Add(new NeitsilliaEngine.Junction(to, floor, returnFloor));
                else
                {
                    from.junctions[index].floorRequirement = floor;
                    from.junctions[index].returnfloor = returnFloor;
                }
                await from.UploadToDatabase();
                await ReplyAsync("Area Junction Added");
            }
        }

        [Command("SpawnNest")]
        public async Task InitSpawnNest()
        {
            if (!await IsGMLevel(4)) return;
            await Neitsillia.Areas.Nests.Nest.SpawnNest();
        }
        [Command("VerifyNests")]
        public async Task VerifyExistingNests()
        {
            if (!await IsGMLevel(4)) return;

            await Neitsillia.Areas.Nests.Nest.VerifyNests();
        }
        #endregion

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

      

        #region Message Manipulation
        [Command("Send Message")]
        public async Task SendMessage(ulong guildID, ulong channelID, string message)
        {
            if (IsGMLevel(4).Result)
                await Program.clientCopy.GetGuild(guildID).GetTextChannel(channelID).SendMessageAsync(message);
            await DUtils.DeleteContextMessageAsync(Context);
        }
        [Command("Notify", true)]
        public async Task Notify(string notification)
        {
            if (IsGMLevel(4).Result)
            {
                string content = Context.Message.Content.Substring(Context.Prefix.Length + notification.Length + 6);
                if (content.Length == 0) throw NeitsilliaError.ReplyError("There is no content to write in this notification");

                EmbedBuilder noti;
                switch (notification)
                {
                    case "patch-notes":
                    case "patch-note":
                    case "patchnote":
                    case "patchnotes":
                    case "patch note":
                    case "patch notes":
                            noti = OtherCommands.UpdateLog();
                        break;

                    default: noti = DUtils.BuildEmbed(notification, content); break;
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
                ulong guildID = Context.Channel.Id;
                string[] copms = messageURL.Replace("https://discordapp.com/channels/", "").Split('/');
                ulong.TryParse(copms[0], out guildID);
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
