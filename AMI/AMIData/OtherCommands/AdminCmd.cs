using AMI.AMIData.Servers;
using AMI.Methods;
using AMI.Module;
using AMI.Neitsillia;
using AMI.Neitsillia.Areas.AreaPartials;
using AMI.Neitsillia.Campaigns;
using AMI.Neitsillia.Items.Quests;
using AMI.Neitsillia.NPCSystems;
using AMI.Neitsillia.User;
using AMI.Neitsillia.User.PlayerPartials;
using AMYPrototype;
using AMYPrototype.Commands;
using Discord;
using Discord.Commands;
using Neitsillia.Items.Item;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace AMI.AMIData.OtherCommands
{
    public class AdminCmd : ModuleBase<Commands.CustomSocketCommandContext>
    {
        internal static MongoDatabase Database => AMYPrototype.Program.data.database;

        internal bool AdminCheck()
        {
            if (Context.User.Id != 201875246091993088) throw NeitsilliaError.ReplyError("You may not use this command");
            return true;
        }
        //Program
        [Command("ProgramExit")]
        public async Task ProgramExit()
        {
            AdminCheck();
            Program.SetState(Program.State.Exiting);
            await Program.Exit("Shutting down...");
        }

        [Command("ProgramUpdate")]
        public async Task UpdateProgram(int wait = 120000)
        {
            AdminCheck();

            if (Context.Message.Attachments.Count == 0) throw NeitsilliaError.ReplyError("No update file attached");

            var file = Enumerable.ToArray(Context.Message.Attachments)[0];

            if(file.Filename != "AMI.exe") throw NeitsilliaError.ReplyError("Incorrect file name");

            string script = null;
            string args = null;

            if (Program.tokens.platform == Tokens.Platforms.Windows)
            {
                List<string> downloads = await DownloadDiscordFiles("./Temp/", "f");
                string[] path = downloads[0].Split('/');

                script = "./UpdateProgram.exe";
                args = $"{downloads[0]} .\\{path[path.Length - 1]}";
            }
            else
            {
                script = "~/Neitsillia/Update.bash";
                args = file.Url;
            }

            new Thread(async () =>
            {
                Program.SetState(Program.State.Updating);
                Thread.Sleep(wait);

                await Program.Exit("Downloading update...");

                object[] values = Utils.RunExecutable(script, args);

            }).Start();
        }

        [Command("Bot Status")]
        public void BotEnable(int s)
        {
            AdminCheck();

            Program.SetState((Program.State)s);
        }

        [Command("Upload")]
        public async Task UploadAt(string type, params string[] path)
        {
            AdminCheck();
                try
                {
                    await DownloadDiscordFiles(ArrayM.ToString(path), type);
                }
                catch (Exception e)
                { Log.LogS(e); await ReplyAsync("Muo, something went wrong!"); }

        }
        async Task<List<string>> DownloadDiscordFiles(string path, string type)
        {
            List<string> download = new List<string>();
            path = (Program.tokens.platform == Tokens.Platforms.Windows ? "./" : 
                "~/" + (Program.isDev ? "neits_dev/" : "Neitsillia/"))
                + path;

            Log.LogS(Environment.NewLine + "Download path: " + path + Environment.NewLine);

            if (!type.StartsWith("f") && path.EndsWith("/") && !Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                await ReplyAsync("Directory not found, Directory created. Retrying...");
            }
            using (var client = new WebClient())
            {
                foreach (var att in Context.Message.Attachments)
                {
                    string npath = path;
                    if (path.EndsWith("/"))
                        npath += att.Filename;
                    string url = att.Url;
                    client.DownloadFile(url, npath);
                    download.Add(npath);
                }
            }
            await ReplyAsync("File(s) uploaded successfully.");
            return download;
        }

        [Command("Mod Health")]
        [Alias("mhp")]
        public async Task Mod_Health(IUser user, int amount)
        {
            if (Context.AdminCheck())
            {
                Player p = Player.Load(user.Id, Player.IgnoreException.All);
                p.health += amount;
                if (p.health <= 0)
                {
                    await ReplyAsync($"{p.name} Has Died and was returned to Atsauka");
                    p.health = p.Health();
                    await p.Respawn(true, true);
                }
                p.SaveFileMongo();
            }
            await DUtils.DeleteContextMessageAsync(Context);
        }
        [Command("Mod Stamina")]
        [Alias("msp")]
        public async Task Mod_Stamina(IUser user, int amount)
        {
            if (Context.AdminCheck())
            {
                Player p = Player.Load(user.Id, Player.IgnoreException.All);
                p.stamina += amount;
                if (p.stamina < 0)
                {
                    p.stamina = 0;
                    await ReplyAsync(p.name + " is exhausted.");
                }
                p.SaveFileMongo();
                await DUtils.DeleteContextMessageAsync(Context);
            }
        }
        [Command("ResetJumpFloorCooldown"), Alias("resetjumpf")]
        public async Task REsetJumpFloorCooldown(IUser user)
        {
            if (Context.AdminCheck())
            {
                Player player = Player.Load(user.Id, Player.IgnoreException.All);
                player.userTimers.floorJumpCooldown = default;
                player.SaveFileMongo();
                await DUtils.DeleteContextMessageAsync(Context);
            }
        }

        [Command("ResetPlayerSpecialization"), Alias("ResetPSpec")]
        public async Task ResetPlayerSpecialization(IUser pinged)
        {
            if (Context.AdminCheck())
            {
                Player player = Player.Load(BotUser.Load(pinged.Id), Player.IgnoreException.All);
                Neitsillia.User.Specialization.Specialization.ResetSpec(player);
                await ReplyAsync("Spec Reseted");
            }
        }

        #region Mongo Database
        [Command("Database Upload")]
        [Summary("Admin only. Upload game data to the database.")]
        public async Task UploadToDatabase(string fileType, string argument = null)
        {
            if (AdminCheck())
            {
                string path = null;
                using (var client = new WebClient())
                {
                    foreach (var att in Context.Message.Attachments)
                    {
                        path = $"./Temp/{att.Filename.Replace('_', ' ')}";
                        string url = att.Url;
                        client.DownloadFile(url, path);
                        //
                        if (path == null)
                            await ReplyAsync("No file found and downloaded");
                        else
                            switch (fileType.ToLower())
                            {
                                case "area":
                                    {
                                        Area temp = FileReading.LoadJSON<Area>(path);
                                        temp.AreaId = temp.GeneratePath();

                                        await temp.UploadToDatabase();
                                        await ReplyAsync($"{temp} Uploaded/Updated");
                                    }
                                    break;
                                case "item":
                                    {
                                        Item temp = FileReading.LoadJSON<Item>(path);
                                        temp.VerifyItem(true);
                                        if (argument == null)
                                            await temp.SaveItemSync();
                                        else
                                            switch (argument.ToLower())
                                            {
                                                case "item":
                                                    await temp.SaveItemSync();
                                                    await ReplyAsync($"{temp} Uploaded/Updated");
                                                    break;
                                                case "skavi":
                                                    await temp.SaveItemSync("Skavi");
                                                    await ReplyAsync($"{temp} Uploaded/Updated");
                                                    break;
                                                case "event":
                                                    await temp.SaveItemSync("Event Items");
                                                    await ReplyAsync($"{temp} Uploaded/Updated");
                                                    break;
                                                default: throw NeitsilliaError.ReplyError("Database Table Name is invalid: Item OR Skavi (Unique Items are automatic if unique == true)");
                                            }

                                    }
                                    break;
                                case "skavi":
                                    {
                                        Item temp = FileReading.LoadJSON<Item>(path);
                                        temp.VerifyItem(true);
                                        await temp.SaveItemSync("Skavi");
                                    }
                                    break;
                                case "creature":
                                case "mob":
                                case "character":
                                    {
                                        NPC temp = FileReading.LoadJSON<NPC>(path);
                                        await Database.UpdateRecordAsync<NPC>(
                                            "Creature", AMIData.MongoDatabase.FilterEqual<NPC, string>("_id", temp.name), temp);
                                        await ReplyAsync($"{temp} Uploaded/Updated");
                                    }
                                    break;
                                case "player":
                                    {
                                        Player player = FileReading.LoadJSON<Player>(path);
                                        await Database.UpdateRecordAsync("Character",
                                            MongoDatabase.FilterEqual<Player, string>("_id", player._id), player);
                                        await ReplyAsync($"{player} Uploaded/Updated");
                                    }
                                    break;
                                default: await ReplyAsync("Type entered is incompatible"); break;
                            }
                    }
                }

            }
        }
        [Command("Database Query")]
        [Summary("Make a querie in the database using json formats.")]
        public async Task DatabaseQuery(string tableName, string filter = "{}", string sort = "{}")
        {
            if (AdminCheck())
            {
                try
                {
                    EmbedBuilder embed = null;
                    switch (tableName.ToLower())
                    {
                        case "item":
                        case "unique":
                        case "skavi":
                        case "eventitem":
                            embed = DatabaseQuery<Item>(tableName, filter, sort);
                            break;
                        case "area":
                            embed = DatabaseQuery<Area>(tableName, filter, sort);
                            break;
                        case "player":
                            embed = DatabaseQuery<Player>(tableName, filter, sort);
                            break;
                        default:
                            embed = DatabaseQuery<object>(tableName, filter, sort);
                            break;
                    }
                    await ReplyAsync(embed: embed.Build());

                }
                catch (Exception e)
                {
                    await ReplyAsync(e.Message);
                }
            }
        }
        EmbedBuilder DatabaseQuery<T>(string table, string filter, string sort)
        {
            table = StringM.UpperAt(table);
            List<T> list = Database.LoadSortRecords<T>(table, filter, sort);

            EmbedBuilder embed = new EmbedBuilder();
            embed.WithTitle(table);
            embed.WithDescription($"Filter {filter} {Environment.NewLine} Sort {sort}");
            string content = "";
            int i = 0;
            for (; i < list.Count && content.Length < 800; i++)
            {
                if (typeof(T) == typeof(object) && ((System.Dynamic.ExpandoObject)(object)list[i]).ToDictionary(k => k.Key, k => k.Value).TryGetValue("_id", out object o)) content += $"{o} {Environment.NewLine}";
                else content += $"{list[i]} {Environment.NewLine}";
            }
            if (i < list.Count)
                content += (list.Count - i) + " More Items";
            embed.AddField("Result", content);
            return embed;
        }
        [Command("Database Update Field Name")]
        public async Task DatabaseUpdateFieldName(string tableKey, string filter, string oldName, string newName)
        {
            if (AdminCheck())
            {
                await Database.database.GetCollection<object>(tableKey).UpdateManyAsync(filter, "{ $rename: { '" + oldName + "': '" + newName + "' } }");
                await ReplyAsync("Done");
            }
        }

        async Task UploadAreaToDatabase(params string[] args)
        {
            if (AdminCheck())
            {
                string areaName = StringM.UpperAt(ArrayM.ToString(args));
                Area area = Area.LoadArea(areaName, null);
                await area.UploadToDatabase();
                await ReplyAsync($"{area} Loaded from file and saved into database");
            }
        }

        [Command("Database All Items")]
        [Summary("Uploads all item files located in bot files into the database.")]
        public async Task DatabaseAllItems()
        {
            if (AdminCheck())
            {
                DirectoryInfo[] types = new DirectoryInfo(ReferenceData.olditemsPath).GetDirectories();
                foreach (var tdir in types)
                {
                    if (tdir.Name == "Unique")
                    {
                        FileInfo[] uniques = tdir.GetFiles();
                        foreach (var unique in uniques)
                        {
                            try
                            {
                                Item item = FileReading.LoadJSON<Item>(unique.FullName);
                                item.VerifyItem(true);
                                await item.SaveItemSync();
                            }
                            catch (Exception) { Console.WriteLine("Failed to save Unique item: " + unique.FullName); }
                        }
                    }
                    else if (tdir.Name == "Skavi")
                    {
                        DirectoryInfo[] tiers = tdir.GetDirectories();
                        foreach (var tier in tiers)
                        {
                            FileInfo[] items = tier.GetFiles();
                            foreach (var file in items)
                            {
                                try
                                {
                                    Item item = FileReading.LoadJSON<Item>(file.FullName);
                                    item.VerifyItem(true);
                                    await item.SaveItemSync("Skavi");
                                }
                                catch (Exception) { Console.WriteLine("Failed to save item: " + file.FullName); }
                            }
                        }
                    }
                    else
                    {
                        DirectoryInfo[] tiers = tdir.GetDirectories();
                        foreach (var tier in tiers)
                        {
                            FileInfo[] items = tier.GetFiles();
                            foreach (var file in items)
                            {
                                try
                                {
                                    Item item = FileReading.LoadJSON<Item>(file.FullName);
                                    item.VerifyItem(true);
                                    await item.SaveItemSync();
                                }
                                catch (Exception) { Console.WriteLine("Failed to save item: " + file.FullName); }
                            }
                        }
                    }
                }
                await ReplyAsync("Done");
            }
        }
        [Command("Database All Creatures")]
        [Summary("Uploads all mob files located in bot files into the database.")]
        public async Task DatabaseAllMobs()
        {
            if (AdminCheck())
            {
                DirectoryInfo[] dirs = new DirectoryInfo(ReferenceData.oldmobPath).GetDirectories();
                foreach (var a in dirs)
                {
                    var races = a.GetDirectories();
                    foreach (var b in races)
                    {
                        FileInfo[] files = b.GetFiles();
                        foreach (var f in files)
                        {
                            NPC mob = FileReading.LoadJSON<NPC>(f.FullName);
                            mob.displayName = mob.name;
                            await Database.UpdateRecordAsync("Creature",
                                MongoDatabase.FilterEqual<NPC, string>("_id", mob.displayName),
                                mob);
                            Console.WriteLine($"{mob.displayName}");
                        }
                    }
                }
                await ReplyAsync("Done");
            }
        }

        [Command("Database Rename")]
        [Summary("Renames an entry from the database for all important instances.")]
        public async Task DatabaseRename(string table, string id, string newName)
        {
            AdminCheck();
            switch (table.ToString())
            {
                case "item":
                    {
                        await DatabaseRenameItem(id, newName);

                        await UpdateAllEntires("Creature", new Func<NPC, (bool, string)>(x =>
                        {
                            bool changed = false;
                            foreach(var l in x.drops)
                            {
                                int index = l.FindIndex(s => s == id);
                                if(index > -1)
                                {
                                    l[index] = newName;
                                    changed = true;
                                }
                            }

                            return (changed, x.displayName);

                        }));

                        await UpdateAllEntires("Area", new Func<Area, (bool, string)>(x =>
                        {
                            bool changed = false;
                            foreach (var l in x.loot)
                            {
                                int index = l.FindIndex(s => s == id);
                                if (index > -1)
                                {
                                    l[index] = newName;
                                    changed = true;
                                }
                            }

                            return (changed, x.AreaId);
                        }));
                    }
                    break;
            }
        }
        static async Task DatabaseRenameItem(string id, string newName)
        {
            Item entry = Item.LoadItem(id);
            await Database.DeleteRecord<Item>("Item", id);
            Console.WriteLine($"{id}'s entry was deleted");

            entry.name = newName;
            entry.originalName = newName;
            await entry.SaveItemSync();
            Console.WriteLine($"{newName}'s entry was saved");
        }
        static async Task UpdateAllEntires<T, I>(string table, Func<T, (bool, I)> func)
        {
            var list = await Database.LoadRecordsAsync<T>(table);
            list.ForEach(async x => 
            {
                (bool changed, I id) = func(x);
                
                if (changed)
                {
                    await Database.UpdateRecordAsync("Creature",
                    MongoDatabase.FilterEqual<T, I>("_id", id), x);
                }
            });
        }

        [Command("AddGifEmote")][Alias("agemote")]
        [Summary("Adds a gif or a message to a gif emote. Creates it if the category does not exist")]
        public async Task AddGifEmote(string category, params string[] args)
        {
            AdminCheck();
            int i = 0;
            GifEmote loaded = GifEmote.Load(category);
            if (loaded == null)
            {
                loaded = new GifEmote(category, args[i]);
                i++;
            }
            string reply = null;
            for (; i < args.Length; i++)
            {
                reply += loaded.isNew ? $"{loaded.name} was created with gif `{args[i]}`" :

                    loaded.IsGif(args[i]) ? loaded.AddGif(args[i]) : loaded.AddMessage(args[i])

                    + Environment.NewLine;
            }
            await ReplyAsync(reply);
        }
        [Command("AddAreaMap")]
        public async Task AddAreaMap(string name, string url)
        {
            Context.AdminCheck();

            PremadeMap.Save(new PremadeMap(name, url));

            await ReplyAsync("Map url uploaded");
        }

        [Command("ResetGuildActivity")]
        public async Task ResetGuildActivity()
        {
            Context.AdminCheck();

            Program.SetState(Program.State.Paused);

            new Task(async () =>
            {
                var collection = Program.data.database.database.GetCollection<Servers.GuildSettings>("Guilds");
                collection.UpdateMany("{}", "{ $set: { activityScore: 0 } }");
                await Task.Delay(5000);
                Program.SetState(Program.State.Ready);
            }).Start();
        }
        #endregion

        #region Cheats
        [Command("GiveCurrency")]
        public async Task GiveCurrency(IUser user, string name, int amount)
        {
            AdminCheck();

            Player player = Player.Load(user.Id);
            Events.PlayerCurrency.Load(player._id).Mod(name, amount);
        }

        [Command("Cheat_Enrich_NPC")]
        [Alias("chenpc")]
        public async Task EnrichNPC(int koins)
        {
            if (AdminCheck())
            {
                Player p = Context.Player;
                if (p.Encounter != null && p.Encounter.npc != null)
                {
                    p.Encounter.npc.KCoins += koins;
                    p.SaveFileMongo();

                    await DUtils.DeleteContextMessageAsync(Context);
               
                }
            }
        }
        [Command("Cheat_GiveItem_NPC")]
        [Alias("chitemnpc")]
        public async Task GiveNPCItme(params string[] args)
        {
            if (AdminCheck())
            {
                Item item = Item.LoadItem(StringM.UpperAt(ArrayM.ToString(args, " ")));
                if (item != null)
                {
                    Player p = Context.Player;
                    if (p.Encounter != null && p.Encounter.npc != null)
                    {
                        p.Encounter.npc.AddItemToInv(item, 1, true);
                        p.SaveFileMongo();
                        try
                        {
                            await Context.Message.DeleteAsync();
                        }
                        catch (Exception) { }
                    }
                }
                else await ReplyAsync("Item not found");
            }
        }

        [Command("SimVote")]
        public async Task SimVote(IUser user)
        {
            AdminCheck();
            BotUser bu = BotUser.Load(user.Id);
            await bu.NewVote();
        }
        [Command("StartQuest")]
        public void StartQuest(IUser user, int a, int b, int c)
        {
            AdminCheck();
            Player player = Player.Load(user.Id, Player.IgnoreException.All);
            player.quests.Add(Quest.Load(a, b, c));
            player.SaveFileMongo();
        }
        #endregion

        #region Files
        [Command("Download File")]
        public async Task DownloadFile(params string[] arg)
        {
            if (AdminCheck())
            {
                string path = ArrayM.ToString(arg);
                if (!path.EndsWith("\\") && File.Exists(path))
                    await Context.Channel.SendFileAsync(path);
                else await ReplyAsync("File not found");
            }
        }
        
        [Command("dir")]
        public async Task ShowPathDirectories(params string[] arg)
        {
            if (AdminCheck())
            {
                string path = @"./" + ArrayM.ToString(arg);
                DirectoryInfo[] dirs = new DirectoryInfo(path).GetDirectories();
                string result = null;
                foreach (var d in dirs)
                {
                    result += d.Name + Environment.NewLine;
                }
                if (result == null)
                    result = "No Directories Found.";
                await ReplyAsync(result);
            }
        }
        [Command("dirf")]
        public async Task ShowPathFiles(params string[] arg)
        {
            if (AdminCheck())
            {
                string path = @"./" + ArrayM.ToString(arg);
                FileInfo[] dirs = new DirectoryInfo(path).GetFiles();
                string result = null;
                foreach (var d in dirs)
                {
                    result += d.Name + Environment.NewLine;
                }
                if (result == null)
                    result = "No Files Found.";
                await ReplyAsync(result);
            }
        }
        #endregion

        [Command("LeaveServer")]
        public async Task LeaveServer(ulong id)
        {
            if (Context.AdminCheck())
            {
                var guild = Program.clientCopy.GetGuild(id);
                if (guild == null)
                {
                    await ReplyAsync("Guild not found");
                    return;
                }

                await guild.LeaveAsync();
                await ReplyAsync($"Left {guild.Name}");
            }
        }
        [Command("LeaveInactiveServers")]
        public async Task LeaveInactiveServers()
        {
            if (Context.AdminCheck())
            {
                var guilds = Program.clientCopy.Guilds;
                foreach (var guild in guilds)
                {
                    GuildSettings gs = GuildSettings.Load(guild);
                    if (gs == null || gs.activityScore < 1)
                    {
                        await guild.LeaveAsync();
                        await ReplyAsync($"Left {guild.Name}");
                    }
                }
            }
        }
    }
}
