using AMI.Module;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using static AMI.Neitsillia.User.UserInterface.EUI;
using AMI.Methods;
using System;
using AMI.Neitsillia.Items;
using AMI.Neitsillia.Encounters;
using AMI.Neitsillia.Collections;
using AMYPrototype;
using AMI.Neitsillia.Combat;
using AMI.Neitsillia.NeitsilliaCommands.Social;
using AMI.Neitsillia.Areas.Arenas;
using AMYPrototype.Commands;
using AMI.Neitsillia.Items.Quests;
using AMI.Neitsillia.NPCSystems.Companions;
using AMI.Neitsillia.NeitsilliaCommands;
using AMI.Neitsillia.Gambling.Games;
using System.Linq;
using AMI.AMIData;
using AMI.Neitsillia.User.PlayerPartials;
using AMI.Neitsillia.Items.Perks.PerkLoad;
using AMI.Neitsillia.Areas.InteractiveAreas;

namespace AMI.Neitsillia.User.UserInterface
{
    public partial class UI
    {
        private static ReflectionCache<UI> reflectionCache = new ReflectionCache<UI>();

        public ulong channelID;
        public ulong msgId;
        public List<string> options;
        public MsgType type;
        public string data;

        private IUserMessage message;
        private Player player;
        private IMessageChannel _channel;
        internal IMessageChannel Channel => _channel ?? message?.Channel;

        //
        [JsonConstructor]
        public UI(bool json = true)
        { }
        public UI(IUserMessage argMsg, MsgType argType, Player argplayer, string argData = null, bool loadReaction = true)
        {
            message = argMsg;
            msgId = argMsg.Id;
            channelID = argMsg.Channel.Id;
            type = argType;
            player = argplayer;
            data = argData;

            InitialiseOption();
            VerifyOptions();

            if (loadReaction) LoadOptions(argMsg);

            else options = player.ui?.options;
        }

        public async Task<UI> Edit(Player player, string content, Embed embed, MsgType type, string data, bool editEmotes = true)
        {
            this.type = type;
            this.player = player;
            this.data = data;

            //Edit Message
            IUserMessage msg = await GetUiMessage();
            await msg.ModifyAsync(x =>
            {
                x.Content = content;
                x.Embed = embed;
            });


            if (editEmotes)
            {
                List<string> oldops = options;
                InitialiseOption();
                VerifyOptions();

                oldops.RemoveAll(e => options.Contains(e));
                _ = msg.RemoveReactionsAsync(Program.clientCopy.CurrentUser, oldops.Select(x => EUI.ToEmote(x)).ToArray());

                LoadOptions(msg);
            }

            return this;
        }

        public UI(IUserMessage argMsg, List<string> argoptions)
        {
            msgId = argMsg.Id;
            channelID = argMsg.Channel.Id;
            type = MsgType.Other;
            options = argoptions;
            LoadOptions(argMsg);
        }

        partial void InitialiseOption();

        private void VerifyOptions()
        {
            if (options != null && player != null)
            {
                if (player.IsResting)
                {
                    options.Remove(explore);
                    options.Remove(loot);
                    options.Remove(trade);
                    options.Remove(skills);
                }
                else if (type == MsgType.Main || type == MsgType.Inventory || type == MsgType.Main
                    || type == MsgType.Schems || type == MsgType.Sheet || type == MsgType.Stats
                    || type == MsgType.Skills || type == MsgType.XP)
                {
                    if (player.Encounter != null
                     && (player.Encounter.Name == Encounter.Names.Floor
                     || player.Encounter.Name == Encounter.Names.Dungeon))
                        options.Add(enterFloor);
                }
                if (options.Contains(schem) && (player.schematics == null
                    || player.schematics.Count < 1))
                    options.Remove(schem);
                if (options.Contains(inv) && (player.inventory == null
                    || player.inventory.Count < 1))
                    options.Remove(inv);
                if (options.Contains(loot) && (player.Encounter == null
                    || player.Encounter.loot == null || player.Encounter.loot.Count < 1))
                    options.Remove(loot);
            }
            if (options != null && !options.Contains(help))
                options.Add(help);
        }

        public void LoadOptions(IUserMessage reply)
        {
            if (options != null)
            {
                try
                {
                    _ = reply.AddReactionsAsync(options.Select(s => EUI.ToEmote(s)).ToArray());
                }
                catch (Exception e)
                {
                    _ = Handlers.UniqueChannels.Instance.SendToLog(e);
                    Log.LogS(e);
                }
            }
        }
        
        #region Message Methods
        internal async Task<IUserMessage> GetUiMessage()
        {
            try {
                return message ??= (IUserMessage)await ((ISocketMessageChannel)Program.clientCopy.GetChannel(channelID)).GetMessageAsync(msgId);

            } catch (Exception) { return null; }
        }
        internal async Task TryDeleteMessage()
        {
            try
            {
                var msg = await GetUiMessage();
                _ = msg?.DeleteAsync();
            }
            catch (Exception) { }
            channelID = 0; msgId = 0; type = MsgType.Main;
        }

        internal async Task<IUserMessage> EditMessage(string content = null, Embed embed = null, IMessageChannel chan = null,
            bool sendIfFail = true, bool removeReactions = true)
        {
            try {
                IUserMessage msg = await GetUiMessage();
                if (msg != null)
                {
                    await msg.ModifyAsync(x =>
                    {
                        x.Content = content;
                        x.Embed = embed;
                    });
                    if (removeReactions)
                    {
                        try
                        { await msg.RemoveAllReactionsAsync(); }
                        catch (Exception)
                        { _ = RemoveReactions(msg); }
                    }
                    return msg;
                }
            } catch (Exception)
            {
                await TryDeleteMessage();
                if (sendIfFail)
                {
                    if (chan == null)
                        chan = ((IMessageChannel)Program.clientCopy.GetChannel(channelID));
                    return
                        await chan.SendMessageAsync(content, false, embed);
                }
            }
            return null;
        }

        async Task RemoveReactions(IUserMessage msg)
        {
            msg = msg ?? message ?? await GetUiMessage();

            var bot = Program.clientCopy.CurrentUser;

            _ = msg.RemoveReactionsAsync(bot, options.Select(x => EUI.ToEmote(x)).ToArray());
        }
        async Task TryMSGDel(IUserMessage msg)
        {
            try { await msg.DeleteAsync(); } catch (Exception) { }
        }
        internal async Task RemoveReaction(IEmote e, IUserMessage msg = null,
            SocketSelfUser bot = null)
        {
            options.Remove(e.ToString());
            msg = msg ?? await GetUiMessage();
            _ = msg.RemoveReactionAsync(e,
                  bot ?? Program.clientCopy.CurrentUser);
        }
        internal Task RemoveReaction(string e, IUserMessage msg = null,
            SocketSelfUser bot = null) => RemoveReaction(EUI.ToEmote(e), msg, bot);
        #endregion

        #region Clicks
        public async Task Click(SocketReaction reaction, IUserMessage msg, Player argplayer)
        {
            this.player = argplayer;
            this._channel = reaction.Channel;
            this.message = msg;

            switch (reaction.Emote.ToString())
            {
                case inv:
                    await GameCommands.DisplayInventory(player, reaction.Channel, 0);
                    break;
                case sheet:
                    await GameCommands.SheetDisplay(player, reaction.Channel);
                    break;
                case xp:
                    await GameCommands.ViewXP(player, reaction.Channel);
                    break;
                case explore:
                    await Commands.Areas.Exploration(player, reaction.Channel);
                    break;
                case tpost:
                    await Commands.Areas.ViewJunctions(player, reaction.Channel, 0);
                    break;
                case ability:
                    await GameCommands.Abilities(player, reaction.Channel);
                    break;
                case enterFloor:
                    await EnterFloor(null, reaction.Channel);
                    break;
                case loot:
                    {
                        if (type == MsgType.Loot)
                            await Loot(reaction, msg);
                        else
                        {
                            int.TryParse(data, out int page);
                            await InventoryCommands.Inventory.ViewLoot(player, reaction.Channel, page);
                        }
                    } break;
                case schem:
                    await GameCommands.ViewSchems(player, reaction.Channel);
                    break;
                case stats:
                    await GameCommands.ShortStatsDisplay(player, reaction.Channel);
                    break;
                default:
                    await Click2(reaction, msg);
                    break;
            }
        }
        public async Task Click2(SocketReaction reaction, IUserMessage msg)
        {
            string emote = reaction.Emote.ToString();
            if (type != MsgType.SpecSelection && ArrayM.IsInArray(emote, specs))
                await player.Specialization.MainMenu(player, reaction.Channel);
            else if (emote == help)
                await AllHelp(reaction, msg);
            else if (options.Contains(emote))
            {
                string function = type.ToString();
                try
                {
                    await reflectionCache.Run<Task>(function, this, reaction, msg);

                    //await Utils.RunMethod<Task>(function, this, reaction, msg);
                }
                catch (Exception e) { if(!await NeitsilliaError.SpecialExceptions(e, reaction.Channel, player)) Log.LogS(e); }
            }
            //else await TryMSGDel(msg);
        }

        public async Task AllHelp(SocketReaction reaction, IUserMessage msg)
        {
            string reactionsInfo = null;
            if (options != null)
            {
                reactionsInfo = "Emote actions [Newer UI may be missing information]: " + Environment.NewLine;
                foreach (var s in options)
                {
                    string d = null;
                    if (s != help && (d = GetReactionDescription(s, type)) != null)
                        reactionsInfo += $"{s} **{d}** {Environment.NewLine}";
                }
            }

            string helpType = null;
            switch (type)
            {
                //Same name as type
                default:
                    helpType = AMIData.HelpPages.Help.GetName(type.ToString());
                    break;
            }

            reactionsInfo += (helpType == null ? "This interface does not have any help page from the `Help` command. " +
                Environment.NewLine + "Request one via the support server or the `suggest` command in the bot's DM." :
                $"Use the `Help {helpType}` command for related commands.");

            await reaction.Channel.SendMessageAsync(reactionsInfo);

        }
        #endregion

        #region Click Effect

        public async Task AbilityLevel(SocketReaction reaction, IUserMessage msg)
        {
            int i = int.Parse(data);
            int evovleIndex = -1;
            switch (reaction.Emote.ToString())
            {
                case zero: evovleIndex = 0; break;
                case one: evovleIndex = 1; break;
                case two: evovleIndex = 2; break;
                case three: evovleIndex = 3; break;
                case four: evovleIndex = 4; break;
                case five: evovleIndex = 5; break;
            }
            if (evovleIndex > -1)
            {
                Ability evolve = Ability.Load(player.abilities[i].evolves[evovleIndex]);

                await EditMessage($"{player.name}'s {player.abilities[i].name} evolved to {evolve}",
                    embed: evolve.InfoPage(player.UserEmbedColor(), true).Build());

                player.abilities[i] = evolve;
                player.SaveFileMongo();
            }
        }
        public async Task AutoNewCharacter(SocketReaction reaction, IUserMessage msg)
        {
            switch (reaction.Emote.ToString())
            {
                case ok:
                    await CharacterCommands.AutoCharacter(player, reaction.Channel, false);
                    break;
                case next:
                    await CharacterCommands.SetSkills(player, reaction.Channel, 0, null, new[] { false, false, false, false, false, false });
                    break;
                case info:
                    EmbedBuilder em = DUtils.BuildEmbed("Character Creation",
                    $"{EUI.ok} - Randomize {Environment.NewLine} {EUI.next} - Manual (Advanced)", null, default,
                    DUtils.NewField("Use the reactions to make your choice", "Making a character can be complicated for new users, use this to skip this step and start playing."
                    + Environment.NewLine + "You can always make another new character once you are more comfortable with the system."));
                    await EditMessage(null, em.Build(), removeReactions: false);
                    break;
            }
        }

        #region Crates
        public async Task ResourceCrateList(SocketReaction reaction, IUserMessage msg)
        {
            int i = GetNum(reaction.Emote.ToString());
            if (i >= 0)
            {
                BotUser buser = BotUser.Load(reaction.UserId);
                if (buser.ResourceCrates[i] > 0)
                    await buser.CharListForCrate(reaction.Channel, i);
                else
                    await reaction.Channel.SendMessageAsync("You do not own any of this crate type.");
            }
        }
        public async Task ResourceCrateOpening(SocketReaction reaction, IUserMessage msg)
        {
            int crateType = int.Parse(data.Split(';')[0]);
            int i = GetNum(reaction.Emote.ToString());
            if (i >= 0)
            {
                BotUser buser = BotUser.Load(reaction.UserId);
                if (buser.ResourceCrates[crateType] > 0)
                {
                    string r = buser.OpenCrate(crateType, i, out string givenTo);

                    if (givenTo != null)
                    {
                        string crate = $"{(ReferenceData.ResourceCrate)crateType} Crate";
                        EmbedBuilder newEmbed = DUtils.GetMessageEmbed(msg, false);
                        EmbedFieldBuilder newField = DUtils.NewField(
                        $"Contents of {crate} given to {givenTo}",
                        r, false);
                        newEmbed.AddField(newField);
                        newEmbed.WithFooter($"{buser.ResourceCrates[crateType]}x {crate} Left");

                        await msg.ModifyAsync(m =>
                        {
                            m.Embed = newEmbed.Build();
                        });
                    }
                    else
                        await reaction.Channel.SendMessageAsync(r);
                }
                else
                {
                    await reaction.Channel.SendMessageAsync("You do not own any of this crate type.");
                    await buser.CratesListUI(reaction.Channel);
                }
            }
        }
        #endregion

        #region Character Creation
        public async Task ChooseRace(SocketReaction reaction, IUserMessage msg)
        {
            bool selected = true;
            switch (reaction.Emote.ToString())
            {
                case "🇭":
                    {
                        player.race = ReferenceData.HumanoidRaces.Human;
                        player.perks.Add(PerkLoad.Load("Human Adaptation"));
                    }
                    break;
                case "🇹":
                    {
                        player.race = ReferenceData.HumanoidRaces.Tsiun;
                        player.perks.Add(PerkLoad.Load("Tsiun Trickery"));
                    }
                    break;
                case "🇺":
                    {
                        player.race = ReferenceData.HumanoidRaces.Uskavian;
                        player.perks.Add(PerkLoad.Load("Uskavian Learning"));
                    }
                    break;
                case "🇲":
                    {
                        player.race = ReferenceData.HumanoidRaces.Miganan;
                        player.perks.Add(PerkLoad.Load("Migana Skin"));
                    }
                    break;
                case "🇮":
                    {
                        player.race = ReferenceData.HumanoidRaces.Ireskian;
                        player.perks.Add(PerkLoad.Load("Ireskian Talent"));
                    }
                    break;
                default: selected = false; break;
            }
            if (selected)
            {
                player.SaveFileMongo();
                await TryMSGDel(msg);
                await CharacterCommands.StarterAbilities(player, reaction.Channel, 0);
            }
        }
        public async Task SetSkill(SocketReaction reaction, IUserMessage msg)
        {
            int result = GetLetter(reaction.Emote.ToString()) + 1;

            string[] arrays = data.Split(';');
            bool[] rused = Utils.JSON<bool[]>(arrays[1]);
            if (!rused[result - 1])
                await CharacterCommands.SetSkills(player, reaction.Channel, result,
                    Utils.JSON<int[]>(arrays[0]), rused, true);
        }
        public async Task StarterAbilities(SocketReaction reaction, IUserMessage msg)
        {
            string[] split = data.Split('/');
            int p = int.Parse(split[0]);
            int x = int.Parse(split[1]);
            int z = int.Parse(split[2]);
            int index = GetNum(reaction.Emote.ToString());
            if (index > -1)
            {
                Ability a = Ability.Load(LoadAbility.Starters[p, index]);
                if (player.abilities.Count < 3)
                    player.abilities.Add(a);
                if (x > -1)
                {
                    string prefix = CommandHandler.GetPrefix(reaction.Channel);
                    player.level = 0;
                    await EditMessage("Character creation completed");
                    await GameCommands.StatsDisplay(player, reaction.Channel);
                    await reaction.Channel.SendMessageAsync("Welcome to Neitsillia, Traveler. To guide you, you've been given the \"Tutorial\" Quest line."
                        + Environment.NewLine + "Use the `Quest` command to view your quest list and inspect the quest using the assigned emote. Follow the Tutorial quest to learn to play.");
                }
                else
                    await CharacterCommands.StarterAbilities(player, reaction.Channel, p);
                player.SaveFileMongo();
            }
            else if (reaction.Emote.ToString() == next)
                await CharacterCommands.StarterAbilities(player, reaction.Channel, p + 1, x, z);
            else if (reaction.Emote.ToString() == prev)
                await CharacterCommands.StarterAbilities(player, reaction.Channel, p - 1, x, z);
        }
        #endregion

        #region Gambling
        public async Task GamblingGames(SocketReaction reaction, IUserMessage msg)
        {
            switch (data)
            {
                case "Tavern":
                    {
                        string s = reaction.Emote.ToString();
                        int i = Dice(s);
                        if (i > -1)
                        {
                            switch (i)
                            {
                                case 1: await GamblingCommands.DiceGame_EvenOdd(player, reaction.Channel); break;
                            }
                        }
                        else
                        {
                            i = GetNum(s);
                            string[] games =
                            {
                                "Blackjack",
                            };
                            if (i > games.Length) return;

                            await GamblingGame.SelectInitialBet(player, reaction.Channel, games[i]); break;
                        }

                    }break;
            }
        }
        public async Task DiceGame(SocketReaction reaction, IUserMessage msg)
        {
            string[] d = data.Split(';');
            //$"{coins};{streak}
            int.TryParse(d[0], out int coins);
            int.TryParse(d[1], out int streak);

            int i = Dice(reaction.Emote.ToString());
            if (i != -1)
                await GamblingCommands.DiceGame_EvenOdd(player, reaction.Channel, i, coins, streak);
            switch (reaction.Emote.ToString())
            {
                case cancel: await GameCommands.ShortStatsDisplay(player, reaction.Channel); break;

                case prev: await GamblingCommands.DiceGame_EvenOdd(player, reaction.Channel, -1, Math.Max(coins - 10, 10), streak); break;
                case next: await GamblingCommands.DiceGame_EvenOdd(player, reaction.Channel, -1, coins + 10, streak); break;

                case two: await GamblingCommands.DiceGame_EvenOdd(player, reaction.Channel, -1, coins * 2, streak); break;
                case five: await GamblingCommands.DiceGame_EvenOdd(player, reaction.Channel, -1, coins * 5, streak); break;
                case zero: await GamblingCommands.DiceGame_EvenOdd(player, reaction.Channel, -1, coins * 10, streak); break;
            }
        }
        public async Task CardGame(SocketReaction reaction, IUserMessage msg)
        {
            string[] d = data.Split(';');
            if (d.Length > 1)
            {
                int bet = int.Parse(d[1]);
                switch (reaction.Emote.ToString())
                {
                    case prev: bet = Math.Max(bet - 10, 10); break;
                    case next: bet += 10; break;

                    case two: bet *= 2; break;
                    case five: bet *= 5;  break;
                    case zero: bet *= 10; break;

                    case ok:
                        await GamblingGame.Initialise(
                            player, d[0], bet, reaction.Channel);
                        return;
                }
                await GamblingGame.SelectInitialBet(player, reaction.Channel, d[0], bet);
            }
            else
            {
                if(reaction.Emote.ToString() == cancel)
                {
                    var deck = Gambling.Cards.Deck.Load(player);
                    if (deck?.house != null)
                    {
                        deck.house.bet += player.GamblingHand.bet;
                        deck.Save();
                    }
                    await player.GamblingHandKey.Delete();
                    player.SaveFileMongo();
                    await reaction.Channel.SendMessageAsync("You quit the game");
                    return;
                }

                if (player.GamblingHand.turn != null)
                {
                    await reaction.Channel.SendMessageAsync($"You've already played your turn for this round: {player.GamblingHand.turn}");
                    return;
                }

                Type type = GamblingGame.GetGameType(data);
                Dictionary<string, string> actions = Utils.GetVar<Dictionary<string, string>>(type, "Actions", true);

                if (actions == null) return;

                actions.TryGetValue(reaction.Emote.ToString(), out string action);
                if (action != null)
                {
                    IGamblingGame game = GamblingGame.CreateInstance(type, player);
                    game.Action(action);

                    player.GamblingHand.turn = action;
                    player.GamblingHandKey.Save();

                    await game.EndTurn();
                }
                
            }
        }
        #endregion

        public async Task Puzzle(SocketReaction reaction, IUserMessage msg)
        {
            Encounter enc = player.Encounter;
            if (enc?.Name == Encounter.Names.Puzzle)
            {
                Puzzle puz = enc.puzzle;
                puz.partyName = player.Party?.partyName ?? player.name;
                bool solved = puz.Solve_Puzzle(reaction.Emote.ToString(), enc.turn, out EmbedBuilder embed);
                await EditMessage(null, embed.Build(), removeReactions: solved);
               
                if(solved)
                {
                    player.QuestTrigger(Quest.QuestTrigger.Puzzle, $"{puz.name};{(int)puz.rewardType};{puz.reward}");
                    puz.Solved_Puzzle(enc);
                    await reaction.Channel.SendMessageAsync(embed: enc.GetEmbed().Build());
                }
                else enc.turn++;

                enc.Save();
            }
        }

        #region Strongholds
        public async Task AcceptBuilding(SocketReaction reaction, IUserMessage msg)
        {
            switch (reaction.Emote.ToString())
            {
                case EUI.ok:
                    string[] bdata = data.Split(';');
                    await Commands.Areas.BuildBuilding(player.Area, bdata[0],
                        int.Parse(bdata[1]), reaction.Channel);
                    break;
            }
            await TryMSGDel(msg);
        }
        public async Task AcceptBuildingUpgrade(SocketReaction reaction, IUserMessage msg)
        {
            switch (reaction.Emote.ToString())
            {
                case EUI.ok:
                    string[] bdata = data.Split(';');
                    await Commands.Areas.UpgradeBuilding(player.Area, int.Parse(bdata[0]),
                        int.Parse(bdata[1]), reaction.Channel);
                    break;
            }
            await TryMSGDel(msg);
        }
        #endregion

        public async Task BountyBoard(SocketReaction reaction, IUserMessage msg)
        {
            switch (reaction.Emote.ToString())
            {
                case EUI.prev:
                    if (player.Area.type == Areas.AreaType.Tavern)
                        await TavernInteractive.GenerateBountyFile(player, player.Area, int.Parse(data) - 1, reaction.Channel);
                    break;
                case EUI.next:
                    if (player.Area.type == Areas.AreaType.Tavern)
                        await TavernInteractive.GenerateBountyFile(player, player.Area, int.Parse(data) + 1, reaction.Channel);
                    break;
            }
        }

        public async Task Combat(SocketReaction reaction, IUserMessage msg)
        {
            switch (reaction.Emote.ToString())
            {
                case brawl:
                    {
                        if (player.Encounter.Name == Encounter.Names.PVP)
                            await CombatCommands.PVPTurn(player, data, reaction.Channel);
                        else
                            await CombatCommands.AutoBrawl(player, reaction.Channel);
                    }
                    break;
                case run:
                    {
                        await CombatCommands.Run(player, reaction.Channel, true);
                        await TryMSGDel(msg); break;
                    }
            }
        }
        public async Task ConfirmCharDel(SocketReaction reaction, IUserMessage msg)
        {
            if (reaction.Emote.ToString() == ok)
            {
                string id = reaction.UserId + "\\" + data;
                Player p = Player.Load(id, Player.IgnoreException.All);
                await p.DeleteFileMongo();

                if (p.Area != null && p.Area.name != "Moceoy's Basement")
                {
                    NPCSystems.NPC revival = NPCSystems.NPC.GenerateNPC(p.level, "Choichoith");
                    if (revival != null)
                    {
                        revival.inventory.inv.AddRange(p.inventory.inv);
                        NPCSystems.PopulationHandler.Add(p.Area, revival);
                    }
                }
                await TryMSGDel(msg);
                await CharacterCommands.ListCharacters(reaction.User.Value, reaction.Channel);
            }
            else if (reaction.Emote.ToString() == cancel)
                await TryMSGDel(msg);
        }
        public async Task ConfirmOffer(SocketReaction reaction, IUserMessage msg)
        {
            if (reaction.Emote.ToString() == ok)
            {
                //{o.i.index, o.i.amount, o.i.cost, t.id, o.note}
                string[] args = JsonConvert.DeserializeObject<string[]>(data);
                Player target = Player.Load(ulong.Parse(args[3]), Player.IgnoreException.All, true);

                int amount = int.Parse(args[1]);
                int index = int.Parse(args[0]);
                ItemOffer offer = new ItemOffer(reaction.UserId, target.userid, new StackedItems(player.inventory.GetItem(index), amount),
                    long.Parse(args[2]), args[4]);
                //Place the offer out of the inventory
                player.inventory.Remove(index, amount);
                player.SaveFileMongo();

                IMessageChannel chan = reaction.Channel is IGuildChannel gChan
                    && (await gChan.Guild.GetUserAsync(target.userid)) != null ? reaction.Channel :
                    (IMessageChannel)(await Program.clientCopy.GetUser(target.userid).GetOrCreateDMChannelAsync());

                await chan.SendMessageAsync($"<@{target.userid}>, You've received a new offer. View all offers using " +
                    $"`Received Offers`");
                await offer.InspectOffer(target, chan);
            }
            await TryMSGDel(msg);
        }
        public async Task ConfirmSkills(SocketReaction reaction, IUserMessage msg)
        {
            if (reaction.Emote.ToString() == ok)
            {
                player.health = player.Health();
                player.stamina = player.Stamina();
                player.SaveFileMongo();
                await TryMSGDel(msg);
                await CharacterCommands.ChooseRace(player, reaction.Channel);
            }
            else if (reaction.Emote.ToString() == cancel)
            {
                int[] rolls = Utils.JSON<int[]>(data.Split(';')[0]);
                player.stats.endurance = 0;
                player.stats.intelligence = 0;
                player.stats.strength = 0;
                player.stats.charisma = 0;
                player.stats.dexterity = 0;
                player.stats.perception = 0;
                player.SaveFileMongo();
                await TryMSGDel(msg);
                await CharacterCommands.SetSkills(player, reaction.Channel, 0, rolls, new bool[6]);
            }
        }

        public async Task ConfirmUpgrade(SocketReaction reaction, IUserMessage msg)
        {
            await TryMSGDel(msg);
            if (reaction.Emote.ToString() == ok)
                await Commands.InventoryCommands.Crafting.ProceedItemUpgrade(player,
                    reaction.Channel, data);
        }

        public async Task DuelOffer(SocketReaction reaction, IUserMessage msg)
        {
            if (reaction.Emote.ToString() == ok)
            {
                Player enemy = Player.Load(data);
                if (player.IsEncounter("Combat"))
                    await EditMessage($"{player.name} may not accept a duel while in combat");
                else if (enemy.IsEncounter("Combat"))
                    await EditMessage($"{enemy.name} may not start a duel while in combat");
                else
                {
                    Encounter.NewDuel(player, enemy);
                    await EditMessage($"<@{enemy.userid}>, {player.name} Has accepted the duel!");
                }
            }
        }

        #region Eggs
        public async Task EggPocket(SocketReaction reaction, IUserMessage msg)
        {
            switch (reaction.Emote.ToString())
            {
                case cancel:
                    await player.NewUI(await reaction.Channel.SendMessageAsync("Are you sure you want to discard this egg?"), MsgType.ConfirmEggDiscard);
                    break;
                case pets:
                    await CompanionCommands.ViewPets(player, reaction.Channel);
                    break;
            }
        }

        public async Task ConfirmEggDiscard(SocketReaction reaction, IUserMessage msg)
        {
            switch (reaction.Emote.ToString())
            {
                case ok:
                    player.EggPocket.egg = null;
                    await CompanionCommands.PocketUi(player, reaction.Channel);
                    break;
                case cancel:
                    await CompanionCommands.PocketUi(player, reaction.Channel);
                    break;
            }
        }

        #endregion

        public async Task EndRest(SocketReaction reaction, IUserMessage msg)
        {
            await TryMSGDel(msg);
            if (reaction.Emote.ToString() == ok)
                await Commands.Areas.EndRest(player, reaction.Channel);
        }
        public async Task EnterFloor(SocketReaction setToNull, ISocketMessageChannel channel)
        {
            await Commands.Areas.Enter(player, player.Encounter.Name.ToString(), channel);
        }

        public async Task InspectOffer(SocketReaction reaction, IUserMessage msg)
        {
            ItemOffer offer = ItemOffer.IdLoad(JsonConvert.DeserializeObject<Guid>(data));
            switch (reaction.Emote.ToString())
            {
                case ok:
                    {
                        if (offer.receiver == reaction.UserId)
                        {
                            if (player.KCoins < offer.pricePer)
                            { await EditMessage($"{player.name} does not have enough Kutsyei Coins"); }
                            else
                            {
                                Player offerer = Player.Load(offer.sender, Player.IgnoreException.None, true);
                                if (!player.CollectItem(offer.offer, true))
                                    await EditMessage($"{player.name} does not have inventory storage");
                                else
                                {
                                    player.KCoins -= offer.pricePer;
                                    offerer.KCoins += offer.pricePer;
                                    player.SaveFileMongo();
                                    offerer.SaveFileMongo();
                                    await offer.DeleteAsync();
                                    await reaction.Channel.SendMessageAsync("Trade completed!");
                                    await ItemOffer.GetOffers(player, 0, ItemOffer.OfferQuery.Receiver, reaction.Channel);
                                }
                            }
                        }
                        else await TryMSGDel(msg);
                    }
                    break;
                case cancel:
                    {
                        if (offer.receiver == reaction.UserId)
                        {
                            EmbedBuilder em = new EmbedBuilder();
                            em.WithTitle($"{player.name} denied the following trade.");
                            em.WithDescription(offer.ToInfo(false));
                            //
                            Player sender = Player.Load(offer.sender, Player.IgnoreException.All, true);
                            sender.inventory.Add(offer.offer, -1);
                            sender.SaveFileMongo();
                            await sender.SendMessageToDM("Offer Items returned to inventory.", em, reaction.Channel);
                            await offer.DeleteAsync();
                            await reaction.Channel.SendMessageAsync("Trade Denied!");
                            await NeitsilliaCommands.Social.ItemOffer.GetOffers(player, 0, ItemOffer.OfferQuery.Receiver, reaction.Channel);
                        }
                        else if (offer.sender == reaction.UserId)
                        {
                            EmbedBuilder em = new EmbedBuilder();
                            em.WithTitle($"<@{player.userid}> canceled the following trade.");
                            em.WithDescription(offer.ToInfo(false));
                            //
                            Player receiver = Player.Load(offer.receiver, Player.IgnoreException.All, true);
                            player.inventory.Add(offer.offer, -1);
                            player.SaveFileMongo();
                            await receiver.SendMessageToDM(null, em, reaction.Channel);
                            await offer.DeleteAsync();
                            await reaction.Channel.SendMessageAsync("Trade Canceled, Items retrieved!");
                            await ItemOffer.GetOffers(player, 0, ItemOffer.OfferQuery.Sender, reaction.Channel);
                        }
                    }
                    break;

            }
        }
        public async Task Inventory(SocketReaction reaction, IUserMessage msg)
        {
            int.TryParse(data, out int i);
            switch (reaction.Emote.ToString())
            {
                case prev:
                    await GameCommands.DisplayInventory(player, reaction.Channel, i - 1, "none", true);
                    break;
                case next:
                    await GameCommands.DisplayInventory(player, reaction.Channel, i + 1, "none", true);
                    break;
            }
        }

        public async Task Loot(SocketReaction reaction, IUserMessage msg)
        {
            int p = 0;
            switch (reaction.Emote.ToString())
            {
                case loot:
                    await InventoryCommands.Inventory.CollectLoot(player, reaction.Channel, "all");
                    await TryMSGDel(msg);
                    break;
                case next:
                    int.TryParse(data, out p);
                    await TryMSGDel(msg);
                    await InventoryCommands.Inventory.ViewLoot(player, reaction.Channel, p + 1); break;
                case prev:
                    int.TryParse(data, out p);
                    await TryMSGDel(msg);
                    await InventoryCommands.Inventory.ViewLoot(player, reaction.Channel, p - 1); break;
            }
        }

        #region Pets
        public async Task PetList(SocketReaction reaction, IUserMessage msg)
        {
            int i = GetNum(reaction.Emote.ToString());
            if (i > -1)
            {
                if (player.PetList[i].status == NPCSystems.Companions.Pets.Pet.PetStatus.InParty)
                    await reaction.Channel.SendMessageAsync("Use the `Follower` command to inspect pets in your party");
                else
                    await player.PetList[i].GetInfo(player, reaction.Channel, i);
            }
        }
        public async Task InspectPet(SocketReaction reaction, IUserMessage msg)
        {
            string[] split = data.Split(';');
            int i = int.Parse(split[0]);
            switch (reaction.Emote.ToString())
            {
                case summon:
                    {
                        if (player.IsEncounter("Combat"))
                            throw NeitsilliaError.ReplyError("Cannot summon a pet while in combat.");
                        else if (player.Party == null)
                            throw NeitsilliaError.ReplyError("You must be in a party to summon a pet");
                        else if (player.Party.MemberCount >= player.Party.maxPartySize)
                            throw NeitsilliaError.ReplyError("Party can't hold more members.");

                        var petslot = player.PetList[i];
                        switch (petslot.status)
                        {
                            case NPCSystems.Companions.Pets.Pet.PetStatus.InParty:
                                await reaction.Channel.SendMessageAsync($"{petslot.pet.displayName} is already in a party.");
                                return;

                            case NPCSystems.Companions.Pets.Pet.PetStatus.Idle:
                                petslot.status = NPCSystems.Companions.Pets.Pet.PetStatus.InParty;
                                player.Party.NPCMembers.Add(petslot.pet);
                                await player.Party.SaveData();
                                player.PetList.Save();
                                await reaction.Channel.SendMessageAsync($"{petslot.pet.displayName} has joined {player.Party.partyName}");
                                return;
                        }
                    } break;
                case skills:
                    {
                        var petslot = player.PetList[i];
                        await petslot.UpgradeOptionsUI(player, reaction.Channel, i);
                    }
                    break;
                case whistle: await player.PetList[i].ToggleFetching(player, reaction.Channel, i); break;
                case pickSpec: await player.PetList[i].ViewEvolves(player, reaction.Channel, i); break;
            }
        }
        public async Task PetUpgrade(SocketReaction reaction, IUserMessage msg)
        {
            string e = reaction.Emote.ToString();
            int n = -1;

            string[] split = data.Split(';');
            int i = int.Parse(split[0]);
            var petslot = player.PetList[i];

            switch (e)
            {
                case uturn:
                    {
                        Func<Player, IMessageChannel, int, bool, Task> func = (split.Length == 2 ? (Func<Player, IMessageChannel, int, bool, Task>)petslot.UpgradeOptionsUI : petslot.GetInfo);
                        await func(player, reaction.Channel, i, true);
                    }
                    break;

                case greaterthan: n = 0; break;
                default:
                    n = split.Length > 1 ? (int)GetElement(e) : GetNum(e);
                    break;
            }
            if (n < 0) return;

            if (split.Length > 1 && int.TryParse(split[1], out int k))
            {
                if (petslot.UpgradePet((NPCSystems.Companions.Pets.PetUpgrades.Upgrade)k, n))
                {
                    player.PetList.Save();
                    await petslot.UpgradeStatUI(player, reaction.Channel, i, k);
                }
                else await reaction.Channel.SendMessageAsync("Not enough points for this training.");

            }
            else
            {
                await petslot.UpgradeStatUI(player, reaction.Channel, i, n);
            }
        }
        public async Task PetEvolve(SocketReaction reaction, IUserMessage msg)
        {
            int k = GetNum(reaction.Emote.ToString());
            if (k == -1) return;

            string[] d = data.Split(';');
            int i = int.Parse(d[0]);
            NPCSystems.Companions.Pets.Pet pet = player.PetList[i];
            (int level, string name) = NPCSystems.Companions.Pets.Evolves.GetOptions(pet.pet.race, pet.pet.name)[k];
            if (pet.pet.level < level)
                await reaction.Channel.SendMessageAsync($"{pet.pet.displayName} must be level {level} to evolve into a {name}");
            else
            {
                await reaction.Channel.SendMessageAsync(NPCSystems.Companions.Pets.Evolves.Evolve(pet, name));
                await pet.GetInfo(player, reaction.Channel, i);
            }
        }
        #endregion

        public async Task Schems(SocketReaction reaction, IUserMessage msg)
        {
            int.TryParse(data, out int page);
            switch(reaction.Emote.ToString())
            {
                case next:
                    await GameCommands.ViewSchems(player, reaction.Channel, page + 1);
                    break;
                case prev:
                    await GameCommands.ViewSchems(player, reaction.Channel, page - 1);
                    break;
            }
        }

        public async Task Skills(SocketReaction reaction, IUserMessage msg)
        {
            if (player.skillPoints > 0)
            {
                bool changes = true;
                switch (reaction.Emote.ToString())
                {
                    case "🇪":
                        player.stats.endurance++;
                        player.skillPoints--;
                        break;
                    case "🇮":
                        player.stats.intelligence++;
                        player.skillPoints--;
                        break;
                    case "🇸":
                        player.stats.strength++;
                        player.skillPoints--;
                        break;
                    case "🇨":
                        player.stats.charisma++;
                        player.skillPoints--;
                        break;
                    case "🇩":
                        player.stats.dexterity++;
                        player.skillPoints--;
                        break;
                    case "🇵":
                        player.stats.perception++;
                        player.skillPoints--;
                        break;
                    default:
                        changes = false;
                        break;
                }
                if (changes)
                {
                    player.SaveFileMongo();
                    await TryMSGDel(msg);
                    if (player.skillPoints == 0)
                        await GameCommands.ViewXP(player, reaction.Channel);
                    else await GameCommands.SkillUpgradePage(player, reaction.Channel);
                }
            }
        }

        #region Specs
        public async Task SpecMain(SocketReaction reaction, IUserMessage msg)
        {
            switch(reaction.Emote.ToString())
            {
                case EUI.classAbility:
                    await player.Specialization.ShowAbilityList(player, reaction.Channel);
                    break;
                case EUI.classPerk:
                    await player.Specialization.ShowPerkList(player, reaction.Channel);
                    break;
            }
        }
        public async Task SpecSelection(SocketReaction reaction, IUserMessage msg)
        {
            string emote = reaction.Emote.ToString();
            int j = -1;
            for (int i = 0; i < EUI.specs.Length; i++)
                if (emote.Equals(EUI.specs[i]))
                    j = i;
            if (j > -1)
                await Specialization.Specialization.LoadChosenSpec(player, j, reaction.Channel);
            await msg.DeleteAsync();
        }
        public async Task SpecPerks(SocketReaction reaction, IUserMessage msg)
        {
            if (reaction.Emote.ToString() == uturn)
            {
                await TryMSGDel(msg);
                await player.Specialization.MainMenu(player, reaction.Channel);
            }
            else
            {
                string result = player.Specialization.PurchasePerk(player, GetNum(reaction.Emote.ToString()));
                await msg.ModifyAsync(x =>
                {
                    x.Embed = player.Specialization.PerkListEmbed(player, out string s).Build();
                    x.Content = result;
                });
                await RemoveReaction(reaction.Emote, msg);
            }
        }
        public async Task SpecAbility(SocketReaction reaction, IUserMessage msg)
        {
            if (reaction.Emote.ToString() == uturn)
            {
                await TryMSGDel(msg);
                await player.Specialization.MainMenu(player, reaction.Channel);
            }
            else
            {
                string result = player.Specialization.PurchaseAbility(player, GetNum(reaction.Emote.ToString()));
                await msg.ModifyAsync(x =>
                {
                    x.Embed = player.Specialization.AbilityListEmbed(player, out string s).Build();
                    x.Content = result;
                });
                await RemoveReaction(reaction.Emote, msg);
            }

        }
        #endregion

        public async Task OfferList(SocketReaction reaction, IUserMessage msg)
        {
            //{page}.{x}.{JsonConvert.SerializeObject(guids)}
            string[] args = data.Split('.');
            int page = int.Parse(args[0]);
            int length = int.Parse(args[1]);
            ItemOffer.OfferQuery q = (ItemOffer.OfferQuery)Enum.Parse(typeof(ItemOffer.OfferQuery), args[2]);
            Guid[] guids = JsonConvert.DeserializeObject<Guid[]>(args[3]);
            switch(reaction.Emote.ToString())
            {
                case next: await ItemOffer.GetOffers(player, page + 1, q, reaction.Channel); break;
                case prev: await ItemOffer.GetOffers(player, page - 1, q, reaction.Channel); break;
                default:
                    {
                        int i = GetNum(reaction.Emote.ToString())-1;
                        if (i > -1 && i < guids.Length)
                        {
                            var v = ItemOffer.IdLoad(guids[i]);
                            await v.InspectOffer(player, reaction.Channel);
                        }
                    } break;
            }
            await TryMSGDel(msg);
        }
        public async Task Junctions(SocketReaction reaction, IUserMessage msg)
        {
            string[] args = data.Split(';');
            int page = int.Parse(args[0]);
            int total = int.Parse(args[1]);

            string[] juncIds = JsonConvert.DeserializeObject<string[]>(args[2]);
            switch (reaction.Emote.ToString())
            {
                case next: await Commands.Areas.ViewJunctions(player, reaction.Channel, page + 1); break;
                case prev: await Commands.Areas.ViewJunctions(player, reaction.Channel, page - 1); break;
                default:
                    {
                        int i = GetNum(reaction.Emote.ToString());
                        await Commands.Areas.Enter(player, juncIds[i], reaction.Channel);
                    }
                    break;
            }
            await TryMSGDel(msg);
        }
        
        public async Task Lottery(SocketReaction reaction, IUserMessage msg)
        {
            if (reaction.Emote.ToString() == ticket)
            {
                if(Program.data.lottery != null)
                    await EditMessage(Program.data.lottery.AddEntry(player), chan: reaction.Channel);
                else
                    await EditMessage("No Lottery currently available.", chan: reaction.Channel);
            }
        }
        public async Task NewStronghold(SocketReaction reaction, IUserMessage msg)
        {
            if (reaction.Emote.ToString() == ok)
            {
                string[] ds = data.Split('&');
                await Commands.Areas.BuildStronghold(player, ds[0], int.Parse(ds[1]), reaction.Channel);
            }
            await TryMSGDel(msg);
        }

        #region Quests
        public async Task QuestList(SocketReaction reaction, IUserMessage msg)
        {
            int.TryParse(data, out int page);
            int qIndex = GetNum(reaction.Emote.ToString());
            if (qIndex > -1)
                await Quest.QuestInfo(player, page, (page * Quest.PerPage) + qIndex, reaction.Channel);
            else
            switch (reaction.Emote.ToString())
            {
                case prev:
                    await Quest.QuestList(player, page - 1, reaction.Channel);
                    break;
                case next:
                    await Quest.QuestList(player, page + 1, reaction.Channel);
                    break;
            }

            
        }
        public async Task QuestInfo(SocketReaction reaction, IUserMessage msg)
        {
            int.TryParse(data, out int index);
            int page = index / Quest.PerPage;
            switch (reaction.Emote.ToString())
            {
                case uturn:
                    await Quest.QuestList(player, page, reaction.Channel);
                    break;
                case prev:
                    await Quest.QuestInfo(player, page, index - 1, reaction.Channel);
                    break;
                case next:
                    await Quest.QuestInfo(player, page, index + 1, reaction.Channel);
                    break;
            }
            
                
        }
        public async Task AcceptQuests(SocketReaction reaction, IUserMessage msg)
        {
            string[] idArrays = data.Split(';');
            int i = GetNum(reaction.Emote.ToString());
            if( i > -1 && i < idArrays.Length)
            {
                string[] sid = idArrays[i].Split(',');
                int[] id = new int[] 
                {
                    int.Parse(sid[0]),
                    int.Parse(sid[1]),
                    int.Parse(sid[2]),
                };
                Items.Quests.Quest q = Items.Quests.Quest.Load(id);
                player.quests.Insert(0, q);
                await reaction.Channel.SendMessageAsync(player.name + ", New Quest Accepted!", embed: q.AsEmbed().Build());
                player.SaveFileMongo();
            }
        }
        public async Task DailyQuestBoard(SocketReaction reaction, IUserMessage msg)
        {
            int i = GetNum(reaction.Emote.ToString());
            if(data.Contains(i.ToString()))
            {
                (int i, DailyQuestBoard.Cycle c) index = i <= 3 ? (i, User.DailyQuestBoard.Cycle.Daily) : (i - 3, User.DailyQuestBoard.Cycle.Weekly);

                DailyQuestBoard dq = User.DailyQuestBoard.Load(player._id);
                Quest quest = dq.AcceptQuest(index.i - 1, index.c) ??
                    throw NeitsilliaError.ReplyError("Quest invalid");
                player.quests.Add(quest);
                int p = player.quests.Count /Quest.PerPage;

                player.Quest_Trigger(Quest.QuestTrigger.QuestLine, "XI");
                await reaction.Channel.SendMessageAsync($"Quest {quest.title} accepted!");

                await msg.RemoveReactionAsync(reaction.Emote, Program.clientCopy.CurrentUser);

                await dq.ShowBoard(player, reaction.Channel);

            }
        }
        #endregion

        public async Task XP(SocketReaction reaction, IUserMessage msg)
        {
            switch(reaction.Emote.ToString())
            {
                case skills:
                    await GameCommands.SkillUpgradePage(player, reaction.Channel);
                    break;
                case pickSpec:
                    await Specialization.Specialization.SpecializationChoice(player, reaction.Channel);
                    break;
            }
        }

        #endregion
    }
}
