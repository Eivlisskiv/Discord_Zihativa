using AMI.AMIData.Events;
using AMI.Commands;
using AMI.Methods;
using AMI.Methods.Graphs;
using AMI.Neitsillia;
using AMI.Neitsillia.Areas.AreaPartials;
using AMI.Neitsillia.Crafting;
using AMI.Neitsillia.Items;
using AMI.Neitsillia.Items.Perks.PerkLoad;
using AMI.Neitsillia.Items.Quests;
using AMI.Neitsillia.Religion;
using AMI.Neitsillia.User;
using AMI.Neitsillia.User.PlayerPartials;
using AMI.Neitsillia.User.UserInterface;
using AMYPrototype;
using AMYPrototype.Commands;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Neitsillia.Items.Item;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AMI.Module
{
    public class GameCommands : ModuleBase<CustomSocketCommandContext>
    {
        //23.20..8.22.19.4.8.26..2.22.16.15.4.26.13.
        ////////////Neitsillia Discord RPG Game 

        [Command("Crate"), Alias("Crates", "OpenCrates")]
        public async Task ViewCrates() => await Context.BotUser.CratesListUI(Context.Channel);
        
        [Command("How To Start")]
        [Alias("start")]
        async Task HowToStart() => await ReplyAsync("To begin, create a character using the following command ``~Create Character 'Enter Character Name'``");
        //Characters Commands
        
        private static string NoCharacterMessage()
        {
            return " does not have a character." + Environment.NewLine +
                "New Character command: ``~New Character ''Character Name'' |> Attempts to creates a new character, if no name is entered: name = user-name, if ''~rng'' = random name.``";
        }//*/

        #region Stats

        static EmbedBuilder StatsStart(Player player)
        {
            return DUtils.BuildEmbed(player.name,
                (OngoingEvent.Ongoing != null ? $"{EUI.eventQuest}**Event**{EUI.eventQuest}: __{OngoingEvent.Ongoing.name}__ {Environment.NewLine}" : null)
                + $"Location: {player.areaPath.name} {Environment.NewLine}"
                + (player.areaPath.floor > 0 ? $"Floor: {player.areaPath.floor} {Environment.NewLine}" : null)
                + (player.Encounter != null ? $"Encounter: {player.Encounter.Name} {Environment.NewLine}" : null)
                ,null, player.userSettings.Color());
        }

        [Command("Stats")]
        [Alias("ls", "profile")]
        public async Task StatsDisplay()
        {
            await StatsDisplay(Player.Load(Context.User.Id, Player.IgnoreException.Resting), Context.Channel);
            await DUtils.DeleteContextMessageAsync(Context);
        }
        internal static async Task StatsDisplay(Player player, IMessageChannel chan)
        {
            ///
            EmbedBuilder nChar = StatsStart(player);
            string extraStats = Stats_Advanced(player) + player.GetInfo_DmgRes();
            string equipements = player.GearList(null);
            string generalstats = Stats_General(player);

            nChar.AddField("Info", generalstats + Environment.NewLine +
                           "--Stats--" + Environment.NewLine + 
                           extraStats, true);
            nChar.AddField("Skills", Stats_Skills(player), true);

            nChar.AddField("Equipment: ", equipements);
            IUserMessage reply = await chan.SendMessageAsync(embed: nChar.Build());
            await player.NewUI(reply, MsgType.Stats);
        }
        internal static string Stats_General(Player player, bool longInfo = true)
        {
            string result = null;
            result += "**Level:** " + player.level + Environment.NewLine;
            string health = player.health + "/" + player.Health();
            player.HealthStatus(out string status, true);
            health += $" [{status}]";
            if (health.Length < 21)
                result += "**Health:** " + health + Environment.NewLine;
            else
                result += "**Health:** " + Environment.NewLine + health + Environment.NewLine;
            string stamina = $"{player.stamina}/{player.Stamina()}";
            if (stamina.Length < 21)
                result += "**Stamina:** " + stamina + Environment.NewLine;
            else
                result += "**Stamina:** " + Environment.NewLine + stamina + Environment.NewLine;
            if (player.KCoins.ToString().Length < 6)
                result += "**Kutsyei Coins:** " + player.KCoins + Environment.NewLine;
            else
                result += "**Kutsyei Coins:** " + Environment.NewLine + player.KCoins + Environment.NewLine;

            result += "**Rank:** " + player.Rank();

            result += player.Specialization != null ? Environment.NewLine + "**Specialization:** " + player.Specialization.specType.ToString() : null;
                        
            return result;
        }
        internal static string Stats_Advanced(Player player)
        {
            return "**Agility:** " + player.Agility() + Environment.NewLine +
                        "**Crit Chance:** " + Math.Round(player.CritChance(), 2) + '%' + Environment.NewLine +
                        "**Crit Damage:** " + Math.Round(player.CritMult(), 2) + '%' + Environment.NewLine;
        }
        internal static string Stats_Skills(Player player)
        {
            var ps = player.stats;
            string st = $"Endurance: {ps.endurance}";
            st += SkillBuff(0, ps, Environment.NewLine);
            st += $"Intelligence: {player.stats.intelligence}";
            st += SkillBuff(1, ps, Environment.NewLine);
            st += $"Strength: {player.stats.strength}";
            st += SkillBuff(2, ps, Environment.NewLine);
            st += $"Charisma: {player.stats.charisma}";
            st += SkillBuff(3, ps, Environment.NewLine);
            st += $"Dexterity: {player.stats.dexterity}";
            st += SkillBuff(4, ps, Environment.NewLine);
            st += $"Perception: {player.stats.perception}";
            st += SkillBuff(5, ps, Environment.NewLine);
            return st;
        }
        internal static string SkillBuff(int i, Neitsillia.Collections.Stats ps, string end)
        {
            if (ps.baseBuffs[i] > 0) return $" [+{ps.baseBuffs[i]}]{end}";
            else if (ps.baseBuffs[i] < 0) return $" [{ps.baseBuffs[i]}]{end}";
            return end;
        }

        [Command("Short Stats")]
        [Alias("ss", "s", "balance")]
        public async Task CmdShortStatsDisplay()
        {
            await ShortStatsDisplay(Player.Load(Context.User.Id, Player.IgnoreException.Resting)
                , Context.Channel);
        }
        internal static async Task ShortStatsDisplay(Player player, ISocketMessageChannel chan)
        {
            EmbedBuilder nChar = StatsStart(player);
            nChar.Description += Stats_General(player, false);
            nChar.WithFooter("~stats  OR  ~ls  for more detailed stats sheet.");
            await player.NewUI(await chan.SendMessageAsync(embed: nChar.Build()), MsgType.Stats);
        }
        [Command("equipment", true)][Alias("eq")]
        public async Task EquipmentDisplay()
        {
            await EquipmentDisplay(Player.Load(Context.User.Id, Player.IgnoreException.Resting), Context.Channel);
            await DUtils.DeleteContextMessageAsync(Context);
        }
        internal static async Task EquipmentDisplay(Player player, ISocketMessageChannel chan)
        {
            ///
            EmbedBuilder nChar = new EmbedBuilder();
            nChar = player.UserEmbedColor(nChar);
            nChar.WithTitle(player.name);
            nChar.WithDescription("Location: " + player.Area.name);

            string equipements = player.GearList(null);

            nChar.AddField("Equipment: ", equipements);
            IUserMessage reply = await chan.SendMessageAsync(embed: nChar.Build());
            await player.NewUI(reply, MsgType.Stats);
        }
        //XP
        [Command("xp"), Alias("level")]
        public async Task ViewXP()
        {
            await ViewXP(Player.Load(Context.User.Id, Player.IgnoreException.Resting), Context.Channel);
        }
        internal static async Task ViewXP(Player player, ISocketMessageChannel chan)
        {
                EmbedBuilder nXP = new EmbedBuilder();
                nXP = player.UserEmbedColor(nXP);
                nXP.WithTitle(player.name);
                string xpPoints = player.experience.ToString() + "/" + Quadratic.XPCalc(player.level + 1).ToString();
                if (xpPoints.Length > 50)
                    xpPoints = Environment.NewLine + xpPoints;
                string details = "Level: " + player.level + Environment.NewLine + player.DetailedXP();
            nXP.AddField("Experience", details);
            //Next level reward
            string nextLevelRewards = null;
            if (player.IsGainSkillPoint(player.level + 1))
                nextLevelRewards += "1 SkillPoint " + Environment.NewLine;
            if (player.level == 19)
                nextLevelRewards += "Specialization " + Environment.NewLine;
            if (nextLevelRewards != null)
                nXP.AddField("**Next Level Rewards**", nextLevelRewards);

                await player.NewUI(await chan.SendMessageAsync("Experience Info", embed: nXP.Build()),
                    MsgType.XP);
        }
        internal static async Task SkillUpgradePage(Player player, ISocketMessageChannel chan)
        {
            EmbedBuilder em = new EmbedBuilder();
            em.WithTitle(player.name);
            em.WithFooter($"Skill Points: {player.skillPoints}{Environment.NewLine}");
            em.WithDescription($"Use the reactions to select on which stat to spend your skill point."
                + $"{Environment.NewLine}{EUI.GetLetter(4)} {ReferenceData.EnduranceInfo()}"
                + $"{Environment.NewLine}{EUI.GetLetter(8)} {ReferenceData.IntelligenceInfo()}"
                + $"{Environment.NewLine}{EUI.GetLetter(18)} {ReferenceData.StrengthInfo()}"
                + $"{Environment.NewLine}{EUI.GetLetter(2)} {ReferenceData.CharismaInfo()}"
                + $"{Environment.NewLine}{EUI.GetLetter(3)} {ReferenceData.DexterityInfo()}"
                + $"{Environment.NewLine}{EUI.GetLetter(15)} {ReferenceData.PerceptionInfo()}"
                + "");
            await player.NewUI(await chan.SendMessageAsync(embed: em.Build()),
                MsgType.Skills);
        }
        /////////////Sheet
        [Command("CharacterSheet")]
        [Alias("CharSheet", "Sheet")]
        public async Task SheetDisplay(IUser muser = null)
        {
            IUser user = Context.User;
            if (muser != null)
                user = muser;
            await SheetDisplay(Player.Load(user.Id, Player.IgnoreException.Resting), Context.Channel);
            await DUtils.DeleteContextMessageAsync(Context);
        }
        internal static async Task SheetDisplay(Player player, ISocketMessageChannel chan)
        {
                EmbedBuilder sheetm = new EmbedBuilder();
                sheetm = player.UserEmbedColor(sheetm);
                sheetm.WithTitle(player.name);
                sheetm.AddField("Appearance", player.userSheet.appearance);
                sheetm.AddField("Personality", player.userSheet.personality);
                sheetm.AddField("Other Info",
                    "**Age:** " + player.userSheet.age + " | - | " + "**Race:** " + player.race.ToString() + " | - | " + "**Gender:** " + player.userSheet.gender);
                sheetm.AddField("Lore", player.userSheet.lore);

                IUserMessage reply = await chan.SendMessageAsync(player.name + "'s Character Sheet", embed: sheetm.Build());
                await player.NewUI(reply, MsgType.Sheet);
        }
        [Command("Mod-Sheet")]
        [Alias("msheet", "mod sheet")]
        public async Task ModSheet(string property, params string[] value)
        {
            const int maxChar = 350;
            Player player = Player.Load(Context.User.Id, Player.IgnoreException.Resting);
            string text = ArrayM.ToString(value, " ");
            if (player.name == null)
                await DUtils.Replydb(Context, Context.User.Mention + NoCharacterMessage());
            else
            {
                if (text.Length > maxChar)
                    await ReplyAsync($"Text must be less than {maxChar} | {text.Length}/{maxChar}");
                else if (Verify.IsInArray(property.ToLower(), Sheet.properties))
                {

                    if (player.userSheet.ModifyProperty(property.ToLower(), text))
                        await DUtils.Replydb(Context, "Modifications Completed", lifetime: 2);
                    player.SaveFileMongo();
                }
                else
                    await ReplyAsync("Indicated property unavailable.");
            }
        }

        [Command("Specter")]
        public async Task SpecterInfo()
        {
            Player player = Context.GetPlayer(Player.IgnoreException.Resting);
            if (player.specter == null) await ReplyAsync("You aren't binded to a specter");
            else await ReplyAsync(embed: player.specter.Info(player).Build());
        }
        #endregion

        #region Collections
        [Command("Inventory"), Alias("Inv"), Summary("Displays the inventory page `display page` with the filter (item type).")]
        public async Task DisplayInventory(int displayPage = 1, string filter = "none")
        {
            Player player = Player.Load(Context.User.Id, Player.IgnoreException.Resting);

            await DisplayInventory(player, Context.Channel, displayPage-1, filter);
        }
        internal static async Task DisplayInventory(Player player, IMessageChannel chan, int displayPage, string filter = "none", bool isEdit = false)
        {
            if (player.inventory.inv.Count > 0)
            {
                EmbedBuilder inventory = player.UserEmbedColor(
                    player.inventory.ToEmbed(ref displayPage, ref filter,
                    $"{player.name}'s Inventory", player.InventorySize(),player.equipment));
                //
                if (isEdit)
                    await player.EditUI(null, inventory.Build(), chan, MsgType.Inventory, displayPage.ToString());
                else
                    await player.NewUI(await chan.SendMessageAsync(embed: inventory.Build()), MsgType.Inventory, displayPage.ToString());
            }
            else
                await chan.SendMessageAsync("Inventory Empty");
        }
        [Command("Schematics")]
        [Alias("Schems")]
        public async Task ViewSchems(int displayPage = 1)
            => await ViewSchems(Player.Load(Context.User.Id, Player.IgnoreException.Resting), Context.Channel, displayPage);

        internal static async Task ViewSchems(Player player, ISocketMessageChannel chan, int displayPage = 1)
        {
            int count = player.schematics.Count;
            if (count == 0) throw NeitsilliaError.ReplyError("No known schematics.");

            const int itemPerPage = 15;
            int pages = NumbersM.CeilParse<int>(count / (double)itemPerPage);
            displayPage = Verify.MinMax(displayPage, pages, 1);

            int start = (displayPage - 1) * itemPerPage;
            string desc = string.Join(Environment.NewLine, player.schematics.GetRange(
                start, start + itemPerPage > count ? count - start : itemPerPage));

            EmbedBuilder schems = DUtils.BuildEmbed($"{player.name}'s Schematics", desc, $"Page {displayPage}/{pages}", player.userSettings.Color());

            await player.NewUI(await chan.SendMessageAsync(embed: schems.Build()), MsgType.Schems, displayPage.ToString());
        }
        //Abilities
        [Command("Abilities")][Alias("ab", "ability", "ainfo", "abilityinfo")]
        [Summary("Displays the character's abilities. Enter a ability name to view more details on a specific ability")]
        public async Task Abilities(params string[] abilityName)
        => await AbilityInfo(abilityName);

        public async Task AbilityInfo(params string[] arg)
        {
            Player player = Context.GetPlayer(Player.IgnoreException.Resting);
            if (arg.Length < 1) await Abilities(player, Context.Channel);
            else
            {
                string name = ArrayM.ToString(arg);
                if (player.HasAbility(name, out int index))
                {
                    EmbedBuilder e = player.UserEmbedColor(new EmbedBuilder());
                    e.WithTitle($"{player.name}'s ");
                    Ability ability = player.GetAbility(index);
                    var message = await ReplyAsync(embed: ability.InfoPage(e, true).Build());
                    //
                    if (ability.level >= ability.maxLevel) await player.NewUI(message, MsgType.AbilityLevel, index.ToString());
                }
                else
                {
                    await ReplyAsync($"{player.name} has no ability named {name}.");
                    await Abilities(player, Context.Channel);
                }
            }
        }

        internal static async Task Abilities(Player player, ISocketMessageChannel chan)
        {
            EmbedBuilder abs = new EmbedBuilder();
            string list = null;
            foreach (var a in player.abilities)
            {
                if (a.maxLevel > 0)
                {
                    list += $"{a}";
                    if(a.level < a.maxLevel)
                        list += $" {a.DetailedXP()} XP";
                }
                else list += a.name;
                list += Environment.NewLine;
            }

            if(player.specter != null)
                list += player.specter.essence?.name;

            abs.AddField($"{player.name}'s Abilities", list);
            abs.WithFooter("`ability {abilityname}` to view specific information for an ability." );
            abs = player.UserEmbedColor(abs);
            await player.NewUI(await chan.SendMessageAsync(embed: abs.Build()), MsgType.Main);
        }

        [Command("View Perks")]
        [Alias("Perks", "Perk")]
        public async Task View_Perks()
        {
            Player player = Context.Player;
            string playerPerkList = null;
            foreach (Perk perk in player.perks)
                playerPerkList += $"**{perk}** | {Environment.NewLine} ``{perk.desc}`` {Environment.NewLine} ";
            for(int i = 0; i < Neitsillia.Collections.Equipment.gearCount; i++)
            {
                Item gear = player.equipment.GetGear(i);
                if(gear != null && gear.perk != null)
                    playerPerkList += $"**{gear.perk}** (*{gear.name}*) {Environment.NewLine} ``{gear.perk.desc}`` {Environment.NewLine}";
            }
            EmbedBuilder embed = player.UserEmbedColor(new EmbedBuilder());
            embed.WithTitle(player.name + " Perks");
            embed.WithDescription(playerPerkList);
            await DUtils.Replydb(Context, embed: embed.Build());
        }

        [Command("Quest"), Alias("Quests")]
        public async Task ViewQuests(int page = 1)
        {
            Player player = Player.Load(Context.BotUser, Player.IgnoreException.Resting);
            await Quest.QuestList(player, page - 1, Context.Channel);
        }
        #endregion

        [Command("User Color")]
        public async Task ModUColor(params string[] args)
        {
            int r = 0, g = 0, b = 0;
            bool ok = false;
            if(args.Length == 1 && args[0][0] == '#' && args[0].Length == 7)//hexa color
            {
                r = int.Parse(args[0].Substring(1, 2), System.Globalization.NumberStyles.HexNumber);
                g = int.Parse(args[0].Substring(3, 2), System.Globalization.NumberStyles.HexNumber);
                b = int.Parse(args[0].Substring(5, 2), System.Globalization.NumberStyles.HexNumber);
                ok = true;
            }
            else if (args.Length == 3 && int.TryParse(args[0], out r)
                && int.TryParse(args[1], out g) && int.TryParse(args[2], out b))
            {
                ok = true;
            }
            if(ok)
            {
                r = Verify.MinMax(r, 255, 0);
                g = Verify.MinMax(g, 255, 0);
                b = Verify.MinMax(b, 255, 0);
                ulong iD = Context.User.Id;
                Player user = Player.Load(iD);
                if (user != null)
                {
                    string[] values = new string[] { r.ToString(), g.ToString(), b.ToString() };
                    user.userSettings.ModifyMultiValue("color", values);
                    await DUtils.Replydb(Context, "Modifications completed.");
                    user.SaveFileMongo();
                }
                else await DUtils.Replydb(Context, "No user file.");
            }
            else await DUtils.Replydb(Context, "Color format not recognized, supported formats: R G B , #000000");

        }
        
        [Command("Give Coins")]
        public async Task GiveCoins(IUser muser, int coinsgift)
        {
            var user = Context.User;
            if (user.Id != muser.Id)
            {
                ulong muserID = muser.Id;
                Player uplayer = Context.Player;
                Player mplayer = Player.Load(muserID, Player.IgnoreException.All);
                if (uplayer == null)
                    await DUtils.Replydb(Context, Context.User.Mention + NoCharacterMessage());
                if (mplayer == null)
                    await DUtils.Replydb(Context, muser.Mention + NoCharacterMessage());
                else if (uplayer.KCoins >= coinsgift)
                {
                    uplayer.KCoins -= coinsgift;
                    mplayer.KCoins += coinsgift;
                    uplayer.SaveFileMongo();
                    mplayer.SaveFileMongo();
                    await DUtils.Replydb(Context, muser.Mention + " has received " + coinsgift + " Kutsyei Coins from " + user.Username);
                }
                else
                    await DUtils.Replydb(Context, Context.User.Username + " does not have the required Kutsyei coins for this transaction.");
            }else await DUtils.Replydb(Context, Context.User.Username + ", you may not give yourself coins.");
        }
        ////////
        [Command("Fix File")]
        [Alias("fixf")]
        public async Task Fix_File()
        { await FixFile(Player.Load(Context.User.Id, Player.IgnoreException.All), Context.Channel); }
        internal static async Task FixFile(Player player, ISocketMessageChannel chan)
        {
            string message = null;
            int errorsFixed = 0;
            //Character Info
            if (player.userSheet == null)
            {
                player.userSheet = new Sheet();
                errorsFixed++;
            }
            if (player.userSheet.appearance == null)
            { player.userSheet.appearance = "Unknown"; errorsFixed++; }
            if (player.userSheet.gender == null)
            { player.userSheet.gender = "Unknown"; errorsFixed++; }
            if (player.userSheet.lore == null)
            { player.userSheet.lore = "Unknown"; errorsFixed++; }
            if (player.userSheet.personality == null)
            { player.userSheet.personality = "Unknown"; errorsFixed++; }
            //End Character
            //Inventory
            if (player.inventory == null)
            { player.inventory = new Neitsillia.Collections.Inventory(); errorsFixed++; }
            //
            if (player.schematics == null)
            { player.schematics = new List<Schematic>(); errorsFixed++; }
            player.schematics.Sort((x, y) => x.name.CompareTo(y.name));
            for(int i = 0; i < player.schematics.Count;)
            {
                if (player.schematics[i].path != player.schematics[i].name)
                { player.schematics[i].path = player.schematics[i].name; errorsFixed++; }
                if (i > 0 && player.schematics[i].name == player.schematics[i - 1].name)
                {
                    player.schematics.RemoveAt(i);
                    errorsFixed++;
                }
                else i++;
            }
            //
            foreach(Perk perk in player.perks)
            {
                Perk update = PerkLoad.Load(perk.name);
                if (perk.desc != update.desc)
                {
                    errorsFixed++;
                    perk.desc = update.desc;
                }
                if (perk.trigger != update.trigger)
                {
                    errorsFixed++;
                    perk.trigger = update.trigger;
                }
                if (perk.end != update.end)
                {
                    errorsFixed++;
                    perk.end = update.end;
                }
            }
            for(int i = 0; i < Neitsillia.Collections.Equipment.gearCount; i++)
            {
                Item gear = player.equipment.GetGear(i);
                if(gear != null && gear.perk != null)
                {
                    if (gear.perk.name == "-Random")
                        gear.LoadPerk();
                    Perk update = PerkLoad.Load(gear.perk.name);
                    if(gear.perk.desc != update.desc)
                    {
                        errorsFixed++;
                        gear.perk.desc = update.desc;
                    }
                    if (gear.perk.trigger != update.trigger)
                    {
                        errorsFixed++;
                        gear.perk.trigger = update.trigger;
                    }
                    if (gear.perk.end != update.end)
                    {
                        errorsFixed++;
                        gear.perk.end = update.end;
                    }
                }
            }
            //Abilities
            foreach(Ability a in player.abilities)
            {
                Ability update = Ability.Load(a.name, a.level);
                if(a.staminaDrain != update.staminaDrain)
                {
                    a.staminaDrain = update.staminaDrain;
                    errorsFixed++;
                }
                if (a.maxLevel != update.maxLevel)
                {
                    a.maxLevel = update.maxLevel;
                    errorsFixed++;
                }
                if (a.tier != update.tier)
                {
                    a.tier = update.tier;
                    errorsFixed++;
                }
                if (a.description != update.description)
                {
                    a.description = update.description;
                    errorsFixed++;
                }
                if (a.evolves != update.evolves || ((a.evolves != null && update.evolves != null) && !a.evolves.SequenceEqual(update.evolves)))
                {
                    a.evolves = update.evolves;
                    errorsFixed++;
                }
            }
            //Area
            if (player.areaPath == null)
                await player.SetArea(Area.LoadFromName("Atsauka")); errorsFixed++; message += "You were brought back to Atsauka | ";
            //End Area
            player.SaveFileMongo();
            message = errorsFixed + " Error(s) found and fixed";
            if (errorsFixed == 0)
                message += " If you are still encountering issues please contact an Administrator or GM";
            await chan.SendMessageAsync($"{player.name}({player.userid}) : {message}");
        }
        //
        [Command("Event")]
        public async Task EventDisplay(string action = null)
        {
            if (AMIData.Events.OngoingEvent.Ongoing == null)
                await DUtils.DeleteBothMsg(Context, await ReplyAsync("No active event."));
            else
            {
                AMIData.Events.OngoingEvent ongoing = AMIData.Events.OngoingEvent.Ongoing;
                switch (action?.ToLower())
                {
                    case "shop":
                        await ongoing.OpenShop(Context.Player, Context.Channel);
                        break;
                    default:
                        await Context.Player.NewUI(await Context.Channel.SendMessageAsync(
                    embed: AMIData.Events.OngoingEvent.Ongoing.EmbedInfo()), MsgType.Event); break;
                }
                
            }
        }

        [Command("Encounter")]
        public async Task DisplayEncounterInfo()
        {
            Player player = Context.GetPlayer(Player.IgnoreException.All);
            IUserMessage sent = player.Encounter == null ? await ReplyAsync("No Encounter") : await ReplyAsync(embed: player.Encounter.GetEmbed().Build());
            switch(player.Encounter?.Name)
            {
                case Neitsillia.Encounters.Encounter.Names.Bounty:
                case Neitsillia.Encounters.Encounter.Names.Mob:
                    if(player.IsSolo) await player.NewUI(sent, MsgType.Combat);
                    break;
                case Neitsillia.Encounters.Encounter.Names.Puzzle:
                    if(player.IsSolo) await player.NewUI(sent, MsgType.Puzzle);
                    else
                    {
                        foreach(var m in player.Party.members)
                        {
                            Player p = m.id == player.userid ? player : m.LoadPlayer();
                            await p.NewUI(sent, MsgType.Puzzle);
                        }
                    }
                    break;

            }
        }

        [Command("Pray")]
        async Task PrayCommand()
        {
            Player player = Context.Player;

            if (player.IsEncounter("Combat")) await ReplyAsync("It is not the time for this, my child.");
            else
            {
                int index = -1; //jewelry index of a god's sigil
                for(int i = 0; i < player.equipment.jewelry.Length; i++)
                {
                    Item jewel = player.equipment.jewelry[i];
                    if (jewel != null && jewel.isUnique && Faith.Sigils.ContainsKey(jewel.name))
                    {
                        if (index > -1) throw NeitsilliaError.ReplyError("A divided faith is a useless faith");
                        index = i;
                    } 
                }

                if (index <= -1) await ReplyAsync("You show no faith to pray to.");
                else await ReplyAsync(player.Faith.Pray(player,
                    player.equipment.jewelry[index].name));
            }
        }
    }
}
