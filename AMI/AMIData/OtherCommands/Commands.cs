using AMI.AMIData.HelpPages;
using AMI.AMIData.Servers;
using AMI.Commands;
using AMI.Methods;
using AMI.Module;
using AMI.Neitsillia;
using AMI.Neitsillia.Items;
using AMI.Neitsillia.User.PlayerPartials;
using AMYPrototype;
using AMYPrototype.Commands;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AMI.AMIData.OtherCommands
{
    public class OtherCommands : ModuleBase<CustomSocketCommandContext>
    {
        //TODO using Lookup on Discord.CommandInfo and commandInfoEmbed
        [Command("Modules")] [Alias("Module")]
        public async Task Help2(string moduleName = null, params string[] commandName)
        {
            var cservice = CommandHandler.CommandService;
            if (moduleName == null)
            {
                var modulelist = cservice.Modules.Select(mi => mi.Name).ToList();
                modulelist.Sort();

                await ReplyAsync("Please enter command category", embed:
                    DUtils.BuildEmbed("Command Categories", string.Join(Environment.NewLine, modulelist), null, Color.DarkRed,
                    DUtils.NewField("Example", $"{Context.Prefix}module {Utils.RandomElement(cservice.Modules).Name}")).Build());
            }
            else
            {
                var module = cservice.Modules.First(m => m.Name.ToLower() == moduleName?.ToLower());
                if (module == null) await ReplyAsync("Module was not found");
                else if (module.Commands.Count == 0)
                    await ReplyAsync($"Module {module.Name} is empty");
                else if (commandName.Length == 0)
                {
                    await ReplyAsync("Please enter command name", embed:
                    DUtils.BuildEmbed("Commands", string.Join(Environment.NewLine, module.Commands.Select(c => c.Name)), null, Color.DarkRed,
                    DUtils.NewField("Example", $"{Context.Prefix}modules {module.Name} " +
                    $"{module.Commands[Program.rng.Next(module.Commands.Count)].Name}")).Build());
                }
                else
                {
                    string name = string.Join(" ", commandName).ToLower();
                    CommandInfo ci = module.Commands.First(c => c.Name.ToLower() == name);
                    if (ci == null) await ReplyAsync($"Command {name} not found");
                    else await ReplyAsync(embed: new CommandsHandler.CommandInfoEmbed(ci).Embed);
                }
            }
        }

        [Command("help")]
        public async Task Help(string category = "main")
        {
            Help help = new Help(Context.Prefix, category, true);
            await ReplyAsync(embed: help.embed);
        }

        [Command("chelp")]
        public async Task  CommandHelp(params string[] commandName)
        {
            if (commandName.Length == 0) await ReplyAsync("No command name was given");
            else
            {
                var cservice = CommandHandler.CommandService;
                string name = string.Join(" ", commandName).ToLower();
                var search = cservice.Search(name);
                if (!search.IsSuccess) await ReplyAsync(search.ErrorReason);
                else
                {
                    var ci = search.Commands.First().Command;
                    await ReplyAsync(embed: new CommandsHandler.CommandInfoEmbed(ci).Embed);
                }
            }
        }

        [Command("Support Server")][Alias("Support", "support invite")]
        public async Task Createinvite()
        { await Context.Channel.SendMessageAsync("https://discord.gg/eGK72Aw"); }
        [Command("Roll")]
        public async Task Roll(params string[] message)
        {
            EmbedBuilder em = new EmbedBuilder
            { Title = Context.User.Username + "'s Roll" };
            string abilityName = StringM.UpperAt(ArrayM.ToString(message));
            Player player = Player.Load(Context.User.Id, Player.IgnoreException.All);
            if (player.HasAbility(abilityName, out int index))
                em = AbilityRoll(index, player, player.UserEmbedColor(em));
            else
                em = DiceRolls(message, em);
            await DUtils.Replydb(Context, embed: em.Build());
        }
        EmbedBuilder AbilityRoll(int abilityindex, Player player, EmbedBuilder em)
        {
            Random rng = new Random();
            for (int i = 5; i < 5; i++)
                rng.Next(101);
            Ability ability = player.abilities[abilityindex];
            int hitchance = Verify.Min(rng.Next(ability.Agility(player.Agility(), player.stats.Efficiency()) + 1), 25);
            string result = $"Hit Chance: {hitchance}{Environment.NewLine}";
            double damageMult = 1;
            double critChance = ability.CritChance(player.CritChance(), player.stats.GetINT());
            double critDamage = ability.CritChance(player.CritMult(), player.stats.GetINT());
            int x = rng.Next(101);
            while (critChance > 0 && critDamage > 0 && (critChance > 100 || x < critChance))
            {
                damageMult += (critDamage / 100.00);
                critChance -= 100.00;
                critDamage -= 50;
                x = rng.Next(100);
            }
            if (damageMult > 1)
                result += $"Critical Multiplier: {damageMult}{Environment.NewLine}";
            for (int i = 0; i < ReferenceData.DmgType.Length; i++)
            {
                long dmg = ability.Damage(i, player.Damage(i), player.stats.GetINT());
                if (dmg > 0)
                    result += $"{ReferenceData.DmgType[i]} DMG: {dmg}{Environment.NewLine}";
            }
            result += ability.description;
            return em.AddField(ability.name + " Roll", result);
        }
        EmbedBuilder DiceRolls(string[] message, EmbedBuilder em)
        {
            Random r = new Random();
            for (int i = 5; i < 5; i++)
                r.Next(101);
            foreach (string roll in message)
            {
                int maxRoll = 20;
                int numRolls = 1;
                bool error = false;
                string[] values = null;
                if (roll != null)
                {
                    values = roll.Split('d', 'D');
                    if (values.Length == 1)
                    {
                        if (!(int.TryParse(values[0], out maxRoll)))
                            error = true;
                    }
                    else
                    {
                        if (!(values.Length > 0 && int.TryParse(values[0], out numRolls)))
                            error = true;
                        if (values.Length > 1 && values[1] != "" && !int.TryParse(values[1], out maxRoll))
                            error = true;
                    }
                }
                //
                if (!error)
                {
                    string results = null;

                    int[] rolls = new int[numRolls];
                    for (int i = 0; i < numRolls && i < 16; i++)
                    {
                        int rl = r.Next(1, maxRoll + 1);
                        rolls[i] = rl;
                        results += $" [{rl}] ";
                    }
                    em.AddField($"{numRolls}D{maxRoll} Rolls", results);
                }
                else em.AddField("Errors", "Some Roll(s) could not be read.");
            }
            return em;
        }
        //[Command("user info")]
        public async Task UserInfoAsync(ulong id)
            => await UserInfoAsync(Program.clientCopy.GetUser(id));
        [Command("user info")]
        public async Task UserInfoAsync(IUser user = null)
        {
            user = user ?? Context.User;
            EmbedBuilder em = new EmbedBuilder();
            em.WithTitle(user.ToString());
            em.AddField("General Info",
                $"Status: {user.Status} " + Environment.NewLine
                + $"Username: {user.Username} " + Environment.NewLine
                + $"ID: {user.Id} " + Environment.NewLine
                + $"Created: {user.CreatedAt.Date.ToLongDateString()} " + Environment.NewLine
                , true);
            em.WithThumbnailUrl(user.GetAvatarUrl());
            em.WithColor(Color.DarkRed);
            if (Context.Guild != null)
                try
                {
                    string roleList = null;
                    SocketGuildUser guser = ((SocketGuildUser)user);

                    var roles = guser.Roles.ToList();
                    roles.Sort((y, x) => x.Position.CompareTo(y.Position));

                    roles.RemoveAt(roles.Count - 1);
                    if (roles.Count == 0) roleList = "No roles";
                    else foreach (var role in roles)
                            roleList += $"<@&{role.Id}>" + Environment.NewLine;

                    em.WithColor(roles[0].Color);
                    em.AddField("Roles", roleList, true);
                }
                catch (Exception) { }
            List<Player> characters = AMYPrototype.Program.data.database.LoadRecords(
                "Character", MongoDatabase.FilterEqual<Player, ulong>("userid", user.Id));
            if (characters.Count > 0)
            {
                string charsInfo = $"{characters.Count} Characters Owned" + Environment.NewLine;
                foreach (var c in characters)
                    charsInfo += $"{c.name} | Level {c.level}" + Environment.NewLine;
                em.AddField("Neitsillia Characters", charsInfo);
            }
            await DUtils.Replydb(Context, embed: em.Build());
        }
        [Command("server info")]
        public async Task ServerInfo(ulong? id = null)
        {
            if (id == null || Context.Guild?.Id == id)
            {
                if (Context.Guild != null)
                    await ReplyAsync(embed: Context.guildSettings.GetInfo(Context.Guild).Build());
                else
                    await ReplyAsync("Current channel is not in a guild.");
            }
            else
            {
                var gs = GuildSettings.Load(id ?? 0);
                if (gs?.Guild == null)
                {
                    //Bot is not in that server, delete from database.
                    await Program.data.database.database.GetCollection<GuildSettings>("Guilds").DeleteOneAsync($"{{_id:{gs.guildID}}}");
                    await ReplyAsync("Guild not found");
                }
                else await ReplyAsync(embed: gs.GetInfo().Build());
            }
        }
        [Command("Patch Notes")]
        public async Task SendUpdateLog()
        {
            EmbedBuilder e = UpdateLog();
            await ReplyAsync("Current Patch Notes", embed: e.Build());
        }
        public static EmbedBuilder UpdateLog()
        {
            string version = ReferenceData.Version();
            EmbedBuilder patch = new EmbedBuilder();
            patch.WithTitle("Version " + version + " Patch Notes");
            //
            string[] PatchNotesMethods = new string[]
            { "General", "Stats","Items","Abilities","Combat",
            "World", "Mobs","Social","Crafting", "NPC", "Quest"};
            //
            foreach (string s in PatchNotesMethods)
            {
                string r = Utils.RunMethod<string>(s + "PatchNotes", typeof(PatchNote));
                if (r != null && r.Trim().Length > 0)
                    patch.AddField(s, r);
            }
            return patch;
        }

        [Command("suggest", true)][Alias("sug")]
        public async Task SendSuggestion(string suggestion)
        {
            string content = Context.Message.Content;
            content = content.Remove(0, content.IndexOf(' '));
            if (content.Trim().Length < 1)
                await DUtils.Replydb(Context, "Your suggestion is empty, please enter your suggestion after the command:" +
                    $" ex: ``{Context.Prefix}suggest More content pls``", lifetime: 2);
            else
            {
                string message = "Suggestion sent" + Environment.NewLine;
                Embed embed = null;
                if (Context.guildSettings == null || Context.guildSettings.suggestionChannel == null)
                {
                    (string url, Embed e) = await Program.data.SendSuggestion(content, Context.User);
                    message += "To support server " + url;
                    embed = e;
                }
                else
                {
                    (string url, Embed e) = await Context.guildSettings.SendSuggestion(content, Context.User, Context.Guild);
                    message += "To support server " + url;
                    embed = e;
                }

                DUtils.DeleteMessage(await ReplyAsync(message, embed: embed));
            } 
        }

        [Command("BugReport", true)]
        public async Task BugReport(string report)
        {
            string content = Context.Message.Content;
            content = content.Remove(0, content.IndexOf(' '));
            if (content.Trim().Length < 1)
                await DUtils.Replydb(Context, "Your report is empty, please enter your report following the command:" +
                    $" ex: ``{Context.Prefix}BugReport I get stuck after going in x area while being y``", lifetime: 2);
            else
            {
                string message = "Report sent" + Environment.NewLine;
                (string url, Embed embed) = await Program.data.SendBugReport(content, Context.User);
                message += "To support server " + url;

                DUtils.DeleteMessage(await ReplyAsync(message, embed: embed));
            }
        }

        [Command("DiscordBotList")]
        [Alias("dblw", "vote")]
        [Summary("Get the top.gg's url for this bot")]
        public async Task GetDBLURL()
        {
            string msg = Program.dblAPI?.WebsiteUrl;
            if (msg != null) msg += Environment.NewLine + $"Use `{Context.Prefix}claim crate` after voting to claim your crate(s)";
            await ReplyAsync(msg ?? "Website unavailable.");
        }

        [Command("Emote")][Alias("e")]
        [Summary("Sends gif emotes")]
        public async Task SendGifEmote(string action = null, IUser target = null, params string[] args)
        {
            
            if(target?.Id == Program.clientCopy.CurrentUser.Id && Context.User.Id != 201875246091993088)
            {
                throw NeitsilliaError.ReplyError(Utils.RandomElement("Blasphemy!", "I will not allow this.", "Cease this.",
                    "I do not condone this behavior.", "Your desires are of no concern to me.", "A pitiful wish, it will remain denied.",
                    "Keep me out of your quarrels."));
            }
            else if (target?.Id == 201875246091993088  && Context.User.Id != 212631292469051392)
            {
                throw NeitsilliaError.ReplyError(Utils.RandomElement("Blasphemy!", "I will not allow this.", "Cease this.",
                    "I do not condone this behavior.", "A pitiful wish, it will remain denied.", "Such is unacceptable behavior."
                    ));
            }

            action = action?.ToLower();

            if(action == "kiss")
            {
                switch(target?.Id)
                {
                    case 535952364356632606:
                    case 670352819844546561:
                        throw NeitsilliaError.ReplyError(Utils.RandomElement("Back off simp", "This one is out of reach."));
                }
            }


            var guild = Context.Guild;
            var _ = await (GifEmote.Load(action)?.Send(Context.Channel,
                guild?.GetUser(Context.User.Id).Nickname ?? Context.User.Username,

                target != null ? 
                guild?.GetUser(target.Id).Nickname ?? target.Username : null)

                ?? ReplyAsync(action != null ? $"{action} was not found as a valid option. Try these: `{GifEmote.GetAll()}`"
                : $"Here are the available actions: `{GifEmote.GetAll()}`"));
        }

        [Command("Connectdbl")]
        public async Task ConnectDBL()
        {
            await Program.dblAPI?.Connect();
            Program.dblAPI?.UpdateServerCount(Program.clientCopy);
            await ReplyAsync(Program.dblAPI?.connected ?? false ? "Connected to top.gg API" : "Could not connect to top.gg API");
        }

        [Command("Ping")]
        public async Task PingCommand()
        {
            //Quick discord
            DateTime then = DateTime.UtcNow;
            string reply = $"Handler: {Context.watch.ElapsedMilliseconds}ms";
            var msg = await ReplyAsync(reply);

            reply += Environment.NewLine + "Bot/Discord: " + (DateTime.UtcNow - then).TotalMilliseconds + " ms";

            then = DateTime.UtcNow;
            _ = Program.data.database.LoadRecords<Neitsillia.Areas.AreaPartials.Area>("Area");
            reply += Environment.NewLine + "Database: " + (DateTime.UtcNow - then).TotalMilliseconds + " ms";

            await msg.ModifyAsync(m => { m.Content = reply; });
        }
    }
}
