using AMI.Commands;
using AMI.Methods;
using AMI.Module;
using AMI.Neitsillia.Items.Abilities;
using AMI.Neitsillia.Items.Abilities.Load;
using AMI.Neitsillia.Items.Perks.PerkLoad;
using AMI.Neitsillia.User;
using AMI.Neitsillia.User.PlayerPartials;
using AMI.Neitsillia.User.UserInterface;
using AMYPrototype;
using AMYPrototype.Commands;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Neitsillia.Methods;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AMI.Neitsillia.NeitsilliaCommands
{
    public class CharacterCommands : ModuleBase<CustomCommandContext>
    {
        static readonly List<string> IllegalNames = new List<string>()
        {
            "zihativa", "kemired"
        };

        [Command("New Character")]
        [Alias("Create Character", "New Char", "Create Char")]
        public async Task StartGame(
            [Summary("The name for your character. If empty, will take your discord username.")]
            params string[] paramName)
        {
            string charname = null;
            ulong playerID = Context.User.Id;
            if (paramName.Length == 0)
                charname = Context.User.Username;
            else if (paramName[0] == "~rng")
            {
                charname = RandomName.ARandomName();
                if (Program.rng.Next(3) == 0)
                    charname += " " + RandomName.ARandomName();
                if (Program.rng.Next(5) == 0)
                    charname += " " + RandomName.ARandomName();
            }
            else charname = ArrayM.ToString(paramName, " ");

            if (Context.User.Id != 201875246091993088 && IllegalNames.Contains(charname.ToLower())) throw NeitsilliaError.ReplyError($"The name {charname} is not allowed, you fool.");

            List<Player> characters = BotUser.GetCharFiles(playerID);

            Match nameResult = Regex.Match(charname, @"^([a-zA-Z]|'|-|’|\s)+$");
            int maxChar = ReferenceData.maxCharacterCount + Context.BotUser.membershipLevel;
            if (characters.Count >= maxChar)
                DUtils.DeleteMessage(await ReplyAsync($"You may not have more than {maxChar} characters"), 1);
            else if (!nameResult.Success)
                DUtils.DeleteMessage(await ReplyAsync("Name must only contain A to Z, (-), ('), (’) and spaces"), 1);
            else if (charname.Length < 2 || charname.Length > 28)
                DUtils.DeleteMessage(await ReplyAsync("Name must be 2 to 28 characters"), 1);
            else if (characters.Find((x) => x.name.ToLower().Equals(charname.ToLower())) != null) //charname, StringComparer.OrdinalIgnoreCase))
                DUtils.DeleteMessage(await ReplyAsync("Character name already used."), 1);
            else
            {
                if (Context.BotUser.dateInscription.Year == 1)
                {
                    Context.BotUser.dateInscription = DateTime.UtcNow;
                    Context.BotUser.Save();
                }

                Player player = new Player(Context.BotUser, charname);


                player.userSettings.RGB = GetRoleColor(Context.User, Context.Guild);
                player.SaveFileMongo();
                await DUtils.DeleteContextMessageAsync(Context);

                await AutoCharacter(player, Context.Channel, true);
            }
        }

        int[] GetRoleColor(IUser user, IGuild guild)
        {
            Color c = DUtils.RandomColor();

            if (guild != null)
                try
                {
                    var gu = (SocketGuildUser)user;
                    List<SocketRole> roles = gu.Roles.ToList();

                    roles.Sort((y, x) => x.Position.CompareTo(y.Position));
                    if (roles[0].Name != "@everyone")
                        c = roles[0].Color;
                }
                catch (Exception) { }
            return new int[] { c.R, c.G, c.B };
        }

        static int[] GetRolls()
        {
            Random rng = new Random();
            int[] rolls = new int[6];
            int total = 0;
            int i = 0;
            for (; i < 2; i++)
                total += rolls[i] = rng.Next(1, ReferenceData.maxSkillRoll + 1);

            total += rolls[i] = rng.Next(1, ReferenceData.maxSkillRoll);
            i++;
            int ibase = Verify.MinMax(ReferenceData.maxSkillRoll - (total / (i + 1)), 6, 3);
            total += rolls[i] = rng.Next(ibase - 2, ibase + 3);

            int leftovers = 30 - total;
            total += rolls[i + 1] = leftovers / 2;
            rolls[i + 2] = 30 - total;

            return rolls;
        }

        internal static async Task AutoCharacter(Player player, IMessageChannel channel, bool ask)
        {
            if (ask)
            {
                EmbedBuilder em = DUtils.BuildEmbed("Character Creation",
                    $"{EUI.ok} - Randomize {Environment.NewLine} {EUI.next} - Manual (Advanced)"
                    + Environment.NewLine + Environment.NewLine + $"{EUI.info} - More Info", null, default);
                await player.NewUI(null, em.Build(), channel, MsgType.AutoNewCharacter);
            }
            else
            {
                int[] rolls = GetRolls();
                var s = player.stats;
                s.endurance = rolls[0];
                s.intelligence = rolls[1];
                s.strength = rolls[2];
                s.charisma = rolls[3];
                s.dexterity = rolls[4];
                s.perception = rolls[5];
                //
                player.abilities.Add(Ability.Load("Strike"));
                player.abilities.Add(Ability.Load("Shelter"));
                //
                player.race = Utils.RandomElement<ReferenceData.HumanoidRaces>();
                switch (player.race)
                {
                    case ReferenceData.HumanoidRaces.Human:
                            player.perks.Add(PerkLoad.Load("Human Adaptation"));
                        break;
                    case ReferenceData.HumanoidRaces.Tsiun:
                            player.perks.Add(PerkLoad.Load("Tsiun Trickery"));
                        break;
                    case ReferenceData.HumanoidRaces.Uskavian:
                            player.perks.Add(PerkLoad.Load("Uskavian Learning"));
                        break;
                    case ReferenceData.HumanoidRaces.Miganan:
                            player.perks.Add(PerkLoad.Load("Migana Skin"));
                        break;
                    case ReferenceData.HumanoidRaces.Ireskian:
                            player.perks.Add(PerkLoad.Load("Ireskian Talent"));
                        break;
                }
                //
                player.level = 0;
                player.health = player.Health();
                player.stamina = player.Stamina();
                player.SaveFileMongo();
                await GameCommands.StatsDisplay(player, channel);
                await channel.SendMessageAsync("Welcome to Neitsillia, Traveler. To guide you, you've been given the \"Tutorial\" Quest line."
                        + Environment.NewLine + "Use the `Quest` command to view your quest list and inspect the quest using the assigned emote. Follow the Tutorial quest to learn to play.");
            }
        }

        internal static async Task SetSkills(Player player, IMessageChannel channel,
            int e, int[] rolls, bool[] rused, bool edit = false)
        {
            rolls ??= GetRolls();

            if (e > 0) SetASkill(player, e - 1, rolls, rused);

            EmbedBuilder em = DUtils.BuildEmbed(player.name, "Select a number from the rolls list using reactions to set that roll as the following stat", 
                null, player.userSettings.Color);

            MsgType type = MsgType.SetSkill;

            EmbedFieldBuilder statField = new EmbedFieldBuilder()
            { Name = "**Stat To Set**", IsInline = true };
            if (player.stats.endurance == 0)
                statField.Value = ReferenceData.EnduranceInfo();
            else if (player.stats.intelligence == 0)
                statField.Value = ReferenceData.IntelligenceInfo();
            else if (player.stats.strength == 0)
                statField.Value = ReferenceData.StrengthInfo();
            else if (player.stats.charisma == 0)
                statField.Value = ReferenceData.CharismaInfo();
            else if (player.stats.dexterity == 0)
                statField.Value = ReferenceData.DexterityInfo();
            else if (player.stats.perception == 0)
                statField.Value = ReferenceData.PerceptionInfo();
            else
            {
                em.WithDescription("Please confirm your stats," +
                    " these may not be re rolled later." +
                    $" Click {EUI.cancel} to reassign your rolls." + 
                    $" Click {EUI.ok} to proceed with these stats.");
                em.AddField("Stats", ""
                    + $"Endurance: {player.stats.endurance}{Environment.NewLine}"
                    + $"Intelligence: {player.stats.intelligence}{Environment.NewLine}"
                    + $"Strength: {player.stats.strength}{Environment.NewLine}"
                    + $"Charisma: {player.stats.charisma}{Environment.NewLine}"
                    + $"Dexterity: {player.stats.dexterity}{Environment.NewLine}"
                    + $"Perception: {player.stats.perception}{Environment.NewLine}"
                    );
                type = MsgType.ConfirmSkills;
                edit = false;
            }

            if (type == MsgType.SetSkill)
            {
                string[] scEmote = { EUI.GetLetter(0), EUI.GetLetter(1), EUI.GetLetter(2),
                EUI.GetLetter(3), EUI.GetLetter(4), EUI.GetLetter(5)};
                string rollsLeft = null;
                for (int r = 0; r < 6; r++)
                {
                    rollsLeft += scEmote[r];
                    if (rused[r])
                        rollsLeft += $"~~[{rolls[r]}]~~";
                    else
                        rollsLeft += $" = [{rolls[r]}]";
                    rollsLeft += Environment.NewLine;
                }
                em.AddField("**Rolls**", rollsLeft, true);
                em.AddField(statField);
                int[] stats = { player.stats.endurance, player.stats.intelligence, player.stats.strength,
                                player.stats.charisma, player.stats.dexterity, player.stats.perception};
                string[] titles = { "Endurance", "Intelligence", "Strength", "Charisma", "Dexterity", "Perception" };
                string currStats = null;
                for(int i = 0; i < stats.Length; i++)
                {
                    if(stats[i] > 0)
                        currStats += $" {titles[i]}: {stats[i]}";
                    else if (i == 0 || stats[i-1] > 0)
                        currStats += $" {titles[i]}:__";
                    else
                        currStats += $" {titles[i]}: ??";
                    currStats += Environment.NewLine;
                }
                em.AddField("**Current Stats**", currStats, true);
            }
            IUserMessage msgEdit = null;
            if (edit && (msgEdit = await player.ui.GetUiMessage()) != null)
            {
                await msgEdit.ModifyAsync(x =>
                {
                    x.Embed = em.Build();
                });
                await msgEdit.RemoveReactionAsync(new Emoji(EUI.GetLetter(e - 1)),
                    Program.clientCopy.CurrentUser);
                player.ui.data = $"{Utils.JSON(rolls)};{Utils.JSON(rused)}";
                player.SaveFileMongo();
            }
            else
            {
                if(player.ui != null)
                await player.ui.TryDeleteMessage();
                await player.NewUI($"For help on how to use interfaces, click the {EUI.help} emote",
                    em.Build(), channel, type, $"{Utils.JSON(rolls)};{Utils.JSON(rused)}");
            }
        }

        static void SetASkill(Player player, int i, int[] rolls, bool[] rused)
        {
            if (player.stats.endurance == 0)
                player.stats.endurance = rolls[i];
            else if (player.stats.intelligence == 0)
                player.stats.intelligence = rolls[i];
            else if (player.stats.strength == 0)
                player.stats.strength = rolls[i];
            else if (player.stats.charisma == 0)
                player.stats.charisma = rolls[i];
            else if (player.stats.dexterity == 0)
                player.stats.dexterity = rolls[i];
            else if (player.stats.perception == 0)
                player.stats.perception = rolls[i];
            rused[i] = true;
        }

        internal static async Task ChooseRace(Player player, IMessageChannel channel)
        {
            EmbedBuilder race = new EmbedBuilder();
            race.WithTitle($"{player.name}'s Race");
            race.WithDescription("Select a race using the reactions, each has a unique perk. You may not change this option later.");
            //
            race.AddField($"{EUI.GetLetter(7)} | **Human** {Environment.NewLine}",
            $"Basic Humans{Environment.NewLine}" +
                $"Perk: {PerkLoad.Load("Human Adaptation").desc}{Environment.NewLine}");
            //
            race.AddField($"{EUI.GetLetter(19)} | **Tsiun** {Environment.NewLine}",
                $"Black or dark color skinned, thin, pointy ears, large eyes and pupils which fill " +
                $"the entire eye, prefer darker areas, learn well by observing others{Environment.NewLine}" +
                $"Perk: {PerkLoad.Load("Tsiun Trickery").desc}{Environment.NewLine}");
            //
            race.AddField($"{EUI.GetLetter(20)} | **Uskavian** {Environment.NewLine}",
                $"Large creatures, not known as a smart race, not very creative and usually does" +
                $" what they know works, brown or darker skin color with some patterns sometimes{Environment.NewLine}" +
            $"Perk: {PerkLoad.Load("Uskavian Learning").desc}{Environment.NewLine}");
            //
            race.AddField($"{EUI.GetLetter(12)} | **Miganan** {Environment.NewLine}",
                $"Similar to humans but with only pale skin variants, mostly white up to grey-ish" +
                $" with a touch of slight beige, very strong skin, grows no hair{Environment.NewLine}" +
            $"Perk: {PerkLoad.Load("Migana Skin").desc}{Environment.NewLine}");
            //
            race.AddField($"{EUI.GetLetter(8)} | **Ireskian** {Environment.NewLine}",
                $"Usually around a feet or two shorter than humans, like to craft and creating things," +
                $" artistic folks, longingly keep their young appearance{Environment.NewLine}" +
            $"Perk: {PerkLoad.Load("Ireskian Talent").desc}{Environment.NewLine}");
            await player.NewUI(null, race.Build(), channel, MsgType.ChooseRace);
        }
        
        internal static async Task<bool> Set_Race(IMessageChannel channel, Player p)
        {
            bool selected = false;

            switch(p.race)
            {
                case ReferenceData.HumanoidRaces.Human:
                    if (p.HasPerk("Human Adaptation") > -1)
                        selected = true;break;
                case ReferenceData.HumanoidRaces.Tsiun:
                    if (p.HasPerk("Tsiun Trickery") > -1)
                        selected = true; break;
                case ReferenceData.HumanoidRaces.Uskavian:
                    if (p.HasPerk("Uskavian Learning") > -1)
                        selected = true; break;
                case ReferenceData.HumanoidRaces.Miganan:
                    if (p.HasPerk("Migana Skin") > -1)
                        selected = true; break;
                case ReferenceData.HumanoidRaces.Ireskian:
                    if (p.HasPerk("Ireskian Talent") > -1)
                        selected = true; break;
            }
            if (!selected)
            { await ChooseRace(p, channel); return true; }
            return false;
        }

        internal static async Task StarterAbilities(Player player, IMessageChannel chan, int page, int firstPick = -1, int firstpickpage = -1)
        {
            EmbedBuilder embed = new EmbedBuilder();
            embed.WithDescription("**You may select 2 out of these 9 abilities**");
            player.UserEmbedColor(embed);
            switch (page)
            {
                case 0:embed.Title = "Simple Abilities";break;
                case 1:embed.Title = "Medium Abilities";break;
                case 2:embed.Title = "Complex Abilities"; break;
            }
            for(int i = 0; i < 3; i++)
            {
                Ability a = Ability.Load(LoadAbility.Starters[page, i]);
                if (player.HasAbility(a.name, out _))
                {
                    firstPick = i;
                    firstpickpage = page;
                }
                else
                    embed.AddField($"{EUI.GetNum(i)} {a.name}", a.type.ToString() + Environment.NewLine +
                        a.description + Environment.NewLine + a.GetStats());
            }
            embed.AddField("Commands",
                "Use ``~ability level #level 'ability name'`` to get the stats for a specific level." +
                " ex: ``~ability level 5 Shelter`` displays the stats of Shelter at level 5");
            embed.WithFooter($"Cycle through the ability type pages using {EUI.prev} {EUI.next}" );
            await player.NewUI(null, embed.Build(), chan,
                MsgType.StarterAbilities, $"{page}/{firstPick}/{firstpickpage}");
        }

        [Command("List Characters")][Alias("List Char", "List Chars", "Characters", "Chars")]
        [Summary("List all of your characters.")]
        public async Task ListCharacters() => await ListCharacters(Context.User, Context.Channel, Context.Prefix);

        public static async Task ListCharacters(IUser user, IMessageChannel chan, string prefix = null)
        {
            var chars = BotUser.GetCharFiles(user.Id);
            if (chars.Count < 1)
                await chan.SendMessageAsync($"No characters found, use ``{prefix}New Character characternamehere`` to create characters");
            else
            {
                string list = ArrayM.ToString(chars, Environment.NewLine);
                EmbedBuilder em = DUtils.BuildEmbed(user.Username + "'s Characters",
                list, null, default, DUtils.NewField("Commands", "`Load charactername` & `Delete charactername`"));
                await chan.SendMessageAsync(embed: em.Build());
            }
        }

        [Command("DeleteCharacter")]
        [Alias("delchar", "delete", "deletechar")]
        public async Task DeleteChar(
            [Summary("The name of the character to delete. If empty, delete current character")]
            params string[] args)
        {
            string charName = ArrayM.ToString(args) ?? Context.BotUser.loaded ?? throw NeitsilliaError.ReplyError("No character name entered.");
            List<Player> list = BotUser.GetCharFiles(Context.User.Id);
            string found = list.Find(x => x.name.ToLower().Equals(charName.ToLower()))?.name ?? null;
            if (found != null)
                Context.BotUser.NewUI(await ReplyAsync($"Are you sure you want to delete this character: " +
                    $"{found}"), MsgType.ConfirmCharDel, found);
            else
                await DUtils.Replydb(Context, $"Character {charName} not found.");
            await DUtils.DeleteContextMessageAsync(Context);
        }

        [Command("LoadCharacter")]
        [Alias("LoadChar", "Load")]
        public async Task LoadCharacter(
            [Summary("Name of the character to load")]
            params string[] charname)
        {
            string res = Context.BotUser.ChangeCharacter(ArrayM.ToString(charname) ?? throw NeitsilliaError.ReplyError("No character name entered."));
            await DUtils.Replydb(Context, res);
        }
    }
    
}
