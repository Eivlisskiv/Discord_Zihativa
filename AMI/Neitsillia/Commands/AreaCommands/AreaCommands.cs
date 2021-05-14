using AMI.Methods;
using AMI.Neitsillia.Areas;
using AMI.Neitsillia.Areas.Strongholds;
using AMI.Neitsillia.Encounters;
using AMI.Neitsillia.User;
using AMYPrototype;
using AMYPrototype.Commands;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using AMI.Neitsillia.Items.ItemPartials;
using NeitsilliaEngine;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AMI.Neitsillia.Items.Quests;
using AMI.Neitsillia.Campaigns;
using AMI.Neitsillia.User.PlayerPartials;
using AMI.Neitsillia.User.UserInterface;
using AMI.Neitsillia.Areas.Arenas;
using AMI.Module;
using AMI.Neitsillia.Areas.AreaPartials;
using AMI.Neitsillia.Areas.InteractiveAreas;

namespace AMI.Neitsillia.Commands
{
    public partial class Areas : ModuleBase<AMI.Commands.CustomCommandContext>
    {
        public static string LocationRestriction(string locationTarget, string userLocation)
        {
            string message = " Location: " + locationTarget + " is out of your reach, you must relocate from current location: " + userLocation + ".";
            if (locationTarget == userLocation)
                message = " You may not relocate to where you already are.";
            return message;
        }
        [Command("Map", true)]
        [Summary("Shows a rough map of the area.")]
        public async Task ShowMap()
        {
            Player player = Context.Player;
            (string url, string name) = PremadeMap.Load(player.Area);
            if (url != null)
            {
                await ReplyAsync(embed: DUtils.BuildEmbed(name + " Map", "Use the `Travel Post` command to travel to areas.",
                    color: Color.DarkGreen).WithImageUrl(url).Build());
            }
            else await ReplyAsync("There are no maps for this area");
        }

        #region Entering
        [Command("Enter")]
        [Summary("Enter the specified area if available.")]
        public async Task Enter(params string[] args)
        {
            if (args.Length > 0)
            {
                string areaName = StringM.UpperAt(ArrayM.ToString(args, " "));
                Player player = Context.Player;
                await Enter(player, areaName, Context.Channel);
                await DUtils.DeleteContextMessageAsync(Context);
            }
            else await DUtils.Replydb(Context, "To travel, one must have a destination. `~Enter 'Area'` or use the `~tp` interface.");
        }
        internal static async Task Enter(Player player, string areaName, ISocketMessageChannel chan)
        {
            //Player is in combat
            if (player.IsEncounter("Combat"))
            {
                DUtils.DeleteMessage(await chan.SendMessageAsync($"You may not enter another area while in combat"));
                return;
            }

            //player is not leader
            if (player.Party != null && player.Party.GetLeaderID() != player.userid)
            {
                await chan.SendMessageAsync($"{player.name} may not lead the party in a new area.");
                return;
            }

            //Player is currently in dungeon
            if (player.Area.type == AreaType.Dungeon && player.Area.arena == null && areaName != "Floor")
            {
                DUtils.DeleteMessage(await chan.SendMessageAsync($"You may not leave this dungeon so easily, the only way is forward."));
                return;
            }

            string error = await TryEnter(player, areaName, chan);

            if(error == null)
            {
                if (player.Party != null)
                    await player.Party.SyncArea(player.AreaInfo);
                return;
            }

            DUtils.DeleteMessage(await chan.SendMessageAsync(
                $"{player.name}, {error} {Environment.NewLine} Use `~Travel Post` for accessible areas."));
        }

        static async Task<string> TryEnter(Player player, string areaName, ISocketMessageChannel chan)
        {
            switch(areaName)
            {
                case "Floor":
                    {
                        if (player.IsEncounter(Encounter.Names.Floor))
                        {
                            await EnterFLoor(player, chan);
                            return null;
                        }
                        return "There are currently no accessible new floors.";
                    }
                case "Dungeon":
                    {
                        if(player.IsEncounter(Encounter.Names.Dungeon))
                        {
                            await EnterDungeon(player, chan);
                            return null;
                        }
                        return "There are no dungeons currently available";
                    }
                default:
                    {
                        Junction j = player.Area.junctions.Find(Junction.FindName(areaName));
                        if (j != null)
                        {
                            if (player.Area.arena != null)
                                await player.Area.arena.EndChallenge(player.Encounter, player.Area, player.areaPath.floor);

                            await EnterJunction(player, j, chan);

                            if(player.Area.AreaId == "Neitsillia\\Casdam Ilse\\Central Casdam\\Serene Cliffside\\Ruined Shrine" 
                                && player.quests.Find(q => q.Equals(0, 2, 2) || q.Equals(0, 2, 3) || q.Equals(0, 2, 4)) != null
                                && (player.Party == null || player.Party.MemberCount == 1))
                            {
                                player.Quest_Trigger(Quest.QuestTrigger.QuestLine, "Whispers Of The Wind III");
                                player.NewEncounter(new Encounter(Encounter.Names.Puzzle, player,
                                    $"Disk;2;Untamed Specter"));
                                await player.NewUI(await chan.SendMessageAsync(embed: player.Encounter.GetEmbed().Build()), MsgType.Puzzle);
                            }

                            return null;
                        }
                        return "Area is non existent or inaccessible.";
                    }
            }
        }

        internal static async Task EnterFLoor(Player player, ISocketMessageChannel chan)
        {
            EmbedBuilder result;
            string message = null;
            MsgType uiType = MsgType.Main;
            player.EndEncounter();

            player.EggPocketTrigger(NPCSystems.Companions.Egg.EggChallenge.Exploration);

            if (AMIData.Events.OngoingEvent.Ongoing != null)
                AMIData.Events.OngoingEvent.Ongoing.EventBounty(player.Area, player.AreaInfo.floor);

            if (player.Area.type == AreaType.Dungeon && player.AreaInfo.floor + 1 >= player.Area.floors)
            {
                player.AreaInfo.floor++;
                NPCSystems.NPC mob = Dungeons.GetBoss(player.Area);
                player.NewEncounter(new Encounter("Mob", player)
                { mobs = new NPCSystems.NPC[] { mob } });

                result = DUtils.BuildEmbed("Boss Battle", "You've encountered a " + mob.name,
                    null, player.userSettings.Color,
                DUtils.NewField(mob.displayName,
                    "Level: " + mob.level + Environment.NewLine +
                    "Rank: " + mob.Rank() + Environment.NewLine +
                    $"Health: {mob.health}/{mob.Health()}" + Environment.NewLine
                    ));
                uiType = MsgType.Combat;
            }
            else
            {
                // Calc the amount of floors to go:
                //           (if lower than area level, only ever 1 [else] difference of player's level and area's lvl capped at 6) capped at available floors
                int floors = FloorIncrease(player);
                player.AreaInfo.floor += floors;
                result = player.Area.AreaInfo(player.AreaInfo.floor).WithColor(player.userSettings.Color);
                message = $"You've advanced {floors} floors " + player.Area.name;
            }

            player.QuestTrigger(Quest.QuestTrigger.EnterFloor,
                $"{player.AreaInfo.path};{player.AreaInfo.floor}");

            await player.NewUI(await chan.SendMessageAsync(message,
                embed: result.Build()), uiType);
        }

        private static int FloorIncrease(Player player)
            => player.Area.type == AreaType.Dungeon || player.Area.type == AreaType.Arena ? 1 :
            Math.Min(player.level < player.Area.level ? 1 : Math.Min((player.level - player.Area.level) / 5, 6), player.Area.floors - player.AreaInfo.floor);

        internal static async Task EnterJunction(Player player, Junction junction, ISocketMessageChannel chan)
        {
            await junction.PassJunction(player);

            if (AMIData.Events.OngoingEvent.Ongoing != null)
                AMIData.Events.OngoingEvent.Ongoing.EventBounty(player.Area, player.AreaInfo.floor);

            player.QuestTrigger(Neitsillia.Items.Quests.Quest.QuestTrigger.Enter, player.AreaInfo.path);

            EmbedBuilder areaInfo = player.UserEmbedColor(player.Area.AreaInfo(player.AreaInfo.floor));
            await player.NewUI(await chan.SendMessageAsync("You've entered " + player.Area.name, embed: areaInfo.Build())
            , MsgType.Main);
        }
        internal static async Task EnterDungeon(Player player, IMessageChannel chan)
        {
            player.EndEncounter();
            Area dungeon = Dungeons.Generate(player.AreaInfo.floor, player.Area);
            await player.SetArea(dungeon, player.AreaInfo.floor);

            player.QuestTrigger(Quest.QuestTrigger.Enter, "Dungeon");

            player.EggPocketTrigger(NPCSystems.Companions.Egg.EggChallenge.Exploration);

            EmbedBuilder areaInfo = player.UserEmbedColor(player.Area.AreaInfo(player.AreaInfo.floor));
            await player.NewUI(await chan.SendMessageAsync("You've entered " + player.Area.name,
            embed: areaInfo.Build()), MsgType.Main);
        }
        internal static async Task EnterArena(Player player, ISocketMessageChannel chan)
        {
            if (!player.IsPartyLeader())
                throw NeitsilliaError.ReplyError("You must be party leader to start a Arena Challenge.");
            await Arena.SelectMode(player, 0, chan);
        }
        #endregion

        [Command("Travel Post")][Alias("tp", "travel")]
        [Summary("View accessible areas from your location.")]
        public async Task ViewJunctions(int page = 1)
            => await ViewJunctions(Context.Player, Context.Channel, page - 1, false);

        internal static async Task ViewJunctions(Player player, ISocketMessageChannel chan, int page, bool edit = true)
        {
            string juncList = null;
            Area pArea = player.Area;

            if (pArea.type == AreaType.Dungeon) throw NeitsilliaError.ReplyError("You can not leave a dungeon so easily." +
                " Fight or cower your way out, but whichever you choose, you must reach the end of these dark floors or leave them your life.");

            List<string> juncIds = new List<string>();
            int n = 0;
            if (pArea.junctions != null)
                for (int i = page * 5; i < pArea.junctions.Count && i < (page + 1) * 5; i++)
                {
                    if (pArea.junctions[i] != null &&
                            player.AreaInfo.floor >= pArea.junctions[i].floorRequirement)
                    {
                        juncList += $"{EUI.GetNum(n)} {pArea.junctions[i]} {Environment.NewLine}";
                        juncIds.Add(pArea.junctions[i].destination);
                        n++;
                    }
                }
            if (player.Encounter != null && player.Encounter.Name == Encounter.Names.Floor)
                juncList += "Floor";
            EmbedBuilder junctions = new EmbedBuilder();
            junctions = player.UserEmbedColor(junctions);
            junctions.WithTitle(pArea.name + " Junctions");
            junctions.WithDescription(juncList);
            junctions.WithFooter($"Use reactions to enter Areas");

            await player.EnUI(edit, null, junctions.Build(), chan, MsgType.Junctions,
                $"{page};{(pArea.junctions?.Count ?? 0)};{JsonConvert.SerializeObject(juncIds)}");
        }

        [Command("Explore", true)][Alias("ep")]
        [Summary("Explore your current location.")]
        public async Task Exploration()
            => await Exploration(Context.Player, Context.Channel);

        internal static async Task Exploration(Player player, ISocketMessageChannel chan)
        {
            string message = "```You take a look around but fail to find anything of interest, there is nothing to explore here.```";
            EmbedBuilder embed = null;

            if (player.IsEncounter("Combat"))
            {
                await chan.SendMessageAsync(embed: player.Encounter.GetEmbed().Build());
                return;
            }
            else if (!player.IsLeader)
            {
                await chan.SendMessageAsync($"{player.name} is not party leader");
                return;
            }
            else if (player.Area.type == AreaType.ArenaLobby || player.Area.type == AreaType.Tavern)
            {
                await chan.SendMessageAsync($"There is nothing to explore here. " +
                   $"Instead use the ``Service`` command to participate in the arena.");
                return;
            }

            Area area = player.Area;

            if (area.IsExplorable)
            {
                try
                {
                    embed = await area.ExploreArea(player);
                    message = $"{player.name} exploring {area.name}";
                }
                catch (Exception exploring)
                {
                    await Handlers.UniqueChannels.Instance.SendToLog(exploring, $"{player.userid} Exploring floor {player.AreaInfo.floor} of {player.Area.name}", chan);
                    throw NeitsilliaError.ReplyError("Apologies, but we've encountered an error. It has been logged and sent to the support channel.");
                }

                player.QuestTrigger(Quest.QuestTrigger.QuestLine, "TIII");
            }

            if (embed != null)
            {
                embed.WithColor(player.userSettings.Color);
                MsgType menuType = MsgType.Main;
                if (player.Encounter != null)
                {
                    if (player.Encounter.IsCombatEncounter())
                        menuType = MsgType.Combat;
                    else
                    switch (player.Encounter.Name)
                    {
                        case Encounter.Names.Loot:
                            {
                                menuType = MsgType.Loot;
                                var party = player.Party;
                                if (party != null && party.NPCMembers.Count > 0)
                                {
                                    NPCSystems.NPC npc =
                                        party.NPCMembers[Program.rng.Next(party.NPCMembers.Count)];
                                    npc.TrimInventory(5);
                                }
                            }
                            break;
                        case Encounter.Names.NPC: menuType = MsgType.NPC; break;
                        case Encounter.Names.Puzzle: menuType = MsgType.Puzzle; break;
                    }
                }
                var msg = await chan.SendMessageAsync(message, embed: embed?.Build());
                if (!player.IsSolo && player.IsEncounter("partyshared"))
                    player.Party.UpdateUI(player, msg, menuType, menuType.ToString());
                else await player.NewUI(msg, menuType, menuType.ToString());
            }
            else DUtils.DeleteMessage(await chan.SendMessageAsync(message));
        }

        [Command("jump floor")][Alias("floor jump")]
        [Summary("Advances floors faster by defeating stronger opponents. 2h Cooldown.")]
        public async Task JumpFloor(int floors)
        {
            Player player = Context.Player;
            floors = Verify.Max(floors, player.Area.floors - player.AreaInfo.floor);
            if (player.Area.type == AreaType.Dungeon || player.Area.type == AreaType.Arena)
                await ReplyAsync($"You may not jump floors in this {player.Area.type}. ");
            else if (!player.userTimers.CanFloorJump())
                await ReplyAsync($"This action is currently on cooldown. {Timers.CoolDownToString(player.userTimers.floorJumpCooldown)} until cooldown end. ");
            else if (player.AreaInfo.floor >= player.Area.floors)
                await ReplyAsync($"{player.name} has already reached the maximum floor.");
            else if (player.Party != null && player.Party.GetLeaderID() != player.userid)
                await ReplyAsync($"Only the leader may lead the party.");
            else
            {
                floors = Verify.Min(floors, 1);
                NPCSystems.NPC[] mobs = new NPCSystems.NPC[1];
                if (player.Party != null)
                    mobs = new NPCSystems.NPC[player.Party.MemberCount];
                Random rng = new Random();
                for (int i = 0; i < mobs.Length; i++)
                {
                    mobs[i] = player.Area.GetAMob(rng, floors);
                    mobs[i].Evolve(2, false);
                    mobs[i].inventory.inv.Clear();
                }
                player.EndEncounter();
                player.NewEncounter(new Encounter(Encounter.Names.FloorJump, player, floors + ""));
                player.Encounter.mobs = mobs;

                player.userTimers.floorJumpCooldown = DateTime.UtcNow.AddHours(2);

                player.EggPocketTrigger(NPCSystems.Companions.Egg.EggChallenge.Exploration);

                await player.NewUI(null, player.Encounter.GetEmbed().Build(), Context.Channel, MsgType.Combat);
            }
        }

        [Command("Service"), Alias("Services"), Summary("Checks the available services for the current area.")]
        public async Task CheckServices()
        => await CheckServices(Context.Player, Context.Channel);
        [Command("Deliver"), Alias("Delivery"), Summary("Attempts to complete a delivery quest in this area.")]
        public async Task DeliveryCommand()
        => await Quest.CheckDeliveries(Context.Player, Context.Channel);

        internal static async Task CheckServices(Player player, ISocketMessageChannel chan)
        {
            switch(player.Area.type)
            {
                
                case AreaType.ArenaLobby:
                    await Arena.Service(player, chan);
                    break;
                    
                case AreaType.BeastMasterShop:
                    await PetShopInteractive.PetShopUi(player, chan);
                    break;

                case AreaType.Tavern:
                    await TavernInteractive.TavernUI(player, chan);
                    break;
                default:
                    await chan.SendMessageAsync("No extra features available in this area.");
                    break;
            }
        }

        internal static EmbedField AvailableQuests(Player player, ref string data, 
            params (int a, int b, int c)[] questGiven)
        {
            int availableQuests = 0;
            foreach (var (a, b, c) in questGiven)
            {
                int[] id = { a, b, c };
                if (player.IsQuestAvailable(id))
                {
                    availableQuests++;
                    data += string.Join(",", id) + ';';
                }
            }
            return DUtils.NewField("Quests " + EUI.mainQuest, availableQuests == 0 ? "No available quests." : $"{availableQuests} Available Quests").Build();
        }

        //
        [Command("Adventure")][Alias("Advt")]
        [Summary("Start an automatic adventure in your current location.")]
        public async Task AdventureStat()
        {
            await DUtils.DeleteContextMessageAsync(Context);
            await AdventureStat(Context.GetPlayer(Player.IgnoreException.Adventuring), Context.Channel);
        }
        internal static async Task AdventureStat(Player player, IMessageChannel chan)
        {
            if (player.level < 0) throw NeitsilliaError.ReplyError("You must first complete your character");
            if (player.Encounter != null && player.Encounter.IsCombatEncounter())
                DUtils.DeleteMessage(await chan.SendMessageAsync("You may not Adventure while in combat"));
            else if (player.Party != null)
                DUtils.DeleteMessage(await chan.SendMessageAsync("You may not start an automatic adventure while in a party."));
            else if (player.Area.type == AreaType.Dungeon || player.Area.type == AreaType.Arena)
                DUtils.DeleteMessage(await chan.SendMessageAsync($"You may not freely Adventure in this {player.Area.type}."));
            else if (player.Area.IsNonHostileArea())
                DUtils.DeleteMessage(await chan.SendMessageAsync($"There are no Adventures in this {player.Area.type}."));
            else if (player.IsInAdventure) await player.Adventure.Display(player, chan, false);
            else await Adventures.Adventure.SelectType(player, chan);
        }

        
        //
        [Command("Area Info")][Alias("areainfo")]
        [Summary("View basic information on your current area.")]
        public async Task Area_Info()
        {
            Player player = Player.Load(Context.User.Id, Player.IgnoreException.Resting);
            await ReplyAsync(embed: player.Area.AreaInfo(player.AreaInfo.floor).Build());
        }

        [Command("NestInfo")][Alias("nesti")]
        [Summary("View information on active nests")]
        public async Task NestInfoDisplay()
        {
            await Neitsillia.Areas.Nests.Nest.NestInfos(Context.Channel);
        }

        #region StrongHolds
        [Command("Create Stronghold")]
        [Summary("Create a new stronghold near your current area. Your current area and floor level will become requirements to access the area.")]
        public async Task Request_Stronghold(int size, params string[] areaCustomName)
        {
            Player player = Context.Player;
            int junctedStrongholds = 0;
            long cost = 0;
            string strongholdName = StringM.UpperAt(ArrayM.ToString(areaCustomName));
            size = Verify.MinMax(size, 5, 1);
            if (Context.User.Id != 201875246091993088)
                await DUtils.Replydb(Context, $"This WIP content is currently unavailable.");
            else if (strongholdName.Length < 5 || strongholdName.Length > 30)
                await DUtils.Replydb(Context, $"Stronghold name must be between 5 and 30 characters long.");
            else if (!Regex.Match(strongholdName, @"^([a-zA-Z]|'|-|’|\s)+$").Success)
                await DUtils.Replydb(Context, $"Name must only contain A to Z, (-), ('), (’) and spaces.");
            else
            {
                bool alreadyExistant = false;
                foreach (var j in player.Area.junctions)
                {
                    if (j.destination.Equals(strongholdName))
                        alreadyExistant = true;
                    if (Area.LoadArea(j.filePath, null).type == AreaType.Stronghold)
                        junctedStrongholds++;
                }
                if (alreadyExistant)
                    await DUtils.Replydb(Context, $"You may not name this area {strongholdName}");
                else if (junctedStrongholds >= 5)
                    await DUtils.Replydb(Context, "There are no free plots of land for you to buy near your current area.");
                else if ((cost = ReferenceData.strongholdCostperSize * size) > player.KCoins)
                    await DUtils.Replydb(Context, $"{player.name} is missing {cost - player.KCoins} Kutsyei Coins for this transaction.");
                else
                    await player.NewUI(await ReplyAsync($"Please confirm the construction of size {size} Stronghold " +
                        $"{strongholdName} conjuncted with floor {player.AreaInfo.floor} of {player.Area.name} for the " +
                        $"cost of {cost} Kutsyei Coins.")
                        , MsgType.NewStronghold, $"{strongholdName}&{size}");
            }
        }
        internal static async Task BuildStronghold(Player player, string name, int size, ISocketMessageChannel chan)
        {
            Area area = player.Area;
            Area stronghold = await Area.NewStronghold(name, size, area, player);
            await player.SetArea(stronghold);
            player.KCoins -= ReferenceData.strongholdCostperSize * size;
            player.SaveFileMongo();
            await chan.SendMessageAsync(embed: stronghold.AreaInfo(0).Build());
        }

        [Command("Stronghold")][Alias("sh")]
        [Summary("View stronghold menu and execute related actions such as building, using stronghold storage and more.")]
        public async Task StrongholdInfo(string action = "info", string target = null, string argument = null)
        {
            Player player = Player.Load(Context.BotUser, Player.IgnoreException.Resting);
            if (player.Area.type != AreaType.Stronghold)
                await DUtils.DeleteBothMsg(Context, await ReplyAsync("Current area is not a Stronghold"));
            else if (player.Area.sandbox != null && player.Area.sandbox.leader != player.userid)
                await DUtils.DeleteBothMsg(Context, await ReplyAsync($"{player.name} does not own this Stronghold"));
            else
            {
                bool delContext = true;
                string message = null;
                EmbedBuilder em = null;
                await DUtils.DeleteContextMessageAsync(Context);
                switch (action.ToLower())
                {
                    case "deposit":
                        switch (target.ToLower())
                        {
                            case "coins":
                            case "coin":
                            case "koin":
                            case "koins":
                            case "kutsyei":
                                {
                                    long amount = 0;
                                    if (argument == null)
                                        message = "No amount entered: ``~sh deposit coins amount#``";
                                    else if (long.TryParse(argument, out amount))
                                        message = await SHDepositCoins(player, amount);
                                    else message = $"{argument} could not be parsed into int64, make sure you enter a number.";
                                }
                                break;
                            case "item":
                                {
                                    var (index, amount) = Verify.IndexXAmount(argument);
                                    message = await SHDepositItem(player, index-1, amount);
                                }
                                break;
                            //case "schematic":
                            //case "schem":
                            //case "blueprint":
                            //    {

                            //    }break;
                            default: message = "Action target unrecognized." + Environment.NewLine
                                    + "~sh deposit coins #amount" + Environment.NewLine
                                    + "~sh deposit item #InventorySlot*#Amount" + Environment.NewLine
                                    ; break;
                        }
                        break;
                    case "withdraw":
                        switch (target.ToLower())
                        {
                            case "coins":
                                {
                                    long amount = 0;
                                    if (argument == null)
                                        message = "No amount entered: ``~sh withdraw coins amount#``";
                                    else if (long.TryParse(argument, out amount))
                                        message = await SHWithdrawCoins(player, amount);
                                    else message = $"{argument} could not be parsed into int64, make sure you enter a number.";
                                }
                                break;
                            case "item":
                                {
                                    var (index, amount) = Verify.IndexXAmount(argument);
                                    message = await SHWithdrawItem(player, index-1, amount);
                                }
                                break;
                            default:
                                message = "Action target unrecognized." + Environment.NewLine
                           + "~sh withdraw coins amount#" + Environment.NewLine
                           + "~sh withdraw item #InventorySlot*#Amount" + Environment.NewLine
                           ; break;
                        }
                        break;
                    
                    case "info":
                        {
                            message = "Information";
                            em = player.Area.sandbox.GetEmbedInfo(player.Area);
                        }
                        break;
                    case "inventory":
                    case "inv":
                    case "stock":
                    case "storage":
                        {
                            int.TryParse(target, out int page);
                            await player.NewUI(await ReplyAsync("Stronghold Stock",
                                embed: player.Area.sandbox.stock.ToEmbed(ref page, $"{player.Area.name} Stock", player.Area.sandbox.stats.StorageSpace).Build()),
                                MsgType.SandboxInventory, page + "");
                        }
                        return;
                        //Buildings
                    case "build":
                        {
                            int schemIndex = -1;
                            var sb = player.Area.sandbox;
                            string bn = StringM.UpperAt(target != null ?  
                                (argument != null ? target + " " + argument : target)
                                : null);
                            if (bn == null)
                            {
                                message = "List of stronghold building schematic.";
                                em = sb.SchematicsList();
                            }
                            else if (!sb.buildingBlueprints.Contains(bn))
                            {
                                message = $"{player.Area.name} does not contain a building blueprint for {bn}.";
                                em = sb.SchematicsList();
                            }
                            else
                            {
                                await StrongholdBuild(player, bn, 0, MsgType.AcceptBuilding);
                                return;
                            }
                        } break;
                    case "upgrade":
                        if (target == null)
                            message = "Action target unrecognized." + Environment.NewLine
                           + "~sh upgrade building slot#" + Environment.NewLine
                           + "~sh upgrade stronghold" + Environment.NewLine;
                        else
                            switch (target.ToLower())
                            {
                                case "building":
                                    {
                                        if (!int.TryParse(argument, out int index))
                                            index = -1;

                                        OldSandBox sb = player.Area.sandbox;
                                        if (index <= -1 || index >= sb.buildings.Count)
                                            em = sb.BuildingList();
                                        else if (sb.buildings[index].Tier >= sb.buildings[index].MaxTier)
                                            message = "Selected building is already at maximum tier";
                                        else
                                        {
                                            await StrongholdBuild(player, sb.buildings[index].Name, sb.buildings[index].Tier + 1
                                                , MsgType.AcceptBuildingUpgrade);
                                            return;
                                        }
                                    }
                                    break;
                                case "stronghold":
                                    message = "Feature unavailable";
                                    break;
                                default:
                                    message = "Action target unrecognized." + Environment.NewLine
                               + "~sh upgrade building slot#" + Environment.NewLine
                               + "~sh upgrade stronghold" + Environment.NewLine
                               ; break;

                            }
                        break;
                    case "destroy":
                        message = (target.ToLower()) switch
                        {
                            "building" => "Feature unavailable",
                            "stronghold" => "Feature unavailable",
                            _ => "Action target unrecognized." + Environment.NewLine
                                + "~sh destroy building slot#" + Environment.NewLine
                                + "~sh destroy stronghold" + Environment.NewLine,
                        };
                        break;
                    case "collect":
                        {
                            string[] results = player.Area.sandbox.Collect(player.Area);
                            em = new EmbedBuilder();
                            em.WithTitle($"{player.Area} Production Collecting");
                            em.AddField("Kutsyei Coins", results[0]);
                            em.AddField("Products", results[1] ?? "No products collected");
                        } break;
                        //Other
                    default:
                        {
                            message = "Stronghold action was not recognized, here are the available commands:" + Environment.NewLine
                             + "~sh deposit coins ``amount#``" + Environment.NewLine
                             + "~sh deposit item ``slot#``" + Environment.NewLine
                             + "~sh withdraw coins ``amount#``" + Environment.NewLine
                             + "~sh withdraw item ``slot#``" + Environment.NewLine
                             + "**Inventory**" + Environment.NewLine
                             + "~sh inventory" + Environment.NewLine
                             + "~sh collect" + Environment.NewLine
                             + "**Buildings**" + Environment.NewLine
                             + "~sh build" + Environment.NewLine
                             + "~sh upgrade" + Environment.NewLine
                             ;
                        }
                        break;
                }
                if (em != null)
                {
                    player.UserEmbedColor(em);
                    DUtils.DeleteMessage(await ReplyAsync(message, embed: em.Build()));
                }
                else if (message != null)
                    DUtils.DeleteMessage(await ReplyAsync(message));
                if (delContext)
                    await DUtils.DeleteContextMessageAsync(Context);
            }
        }
        async Task<string> SHDepositCoins(Player player, long amount)
        {
            OldSandBox sb = player.Area.sandbox;
            if (player.KCoins < amount)
                return $"{player.name} has insufficient funds for this transaction.";
            else if (amount < 1)
                return $"amount must be higher than 0.";
            sb.treasury += amount;
            player.KCoins -= amount;
            player.SaveFileMongo();
            await player.Area.UploadToDatabase();
            return $"Deposited {amount} Kutsyei Coins in {player.Area.name}'s Treasury.";
        }
        async Task<string> SHWithdrawCoins(Player player, long amount)
        {
            OldSandBox sb = player.Area.sandbox;
            if (sb.treasury < amount)
                return $"{player.Area.name}'s Treasury has insufficient funds for this transaction.";
            else if (amount < 1)
                return $"amount must be higher than 0.";
            sb.treasury -= amount;
            player.KCoins += amount;
            player.SaveFileMongo();
            await player.Area.UploadToDatabase();
            return $"Withdrew {amount} Kutsyei Coins from {player.Area.name}'s Treasury.";
        }
        async Task<string> SHDepositItem(Player player, int slot, int amount)
        {
            OldSandBox sb = player.Area.sandbox;
            Item it;
            if ((it = player.inventory.GetItem(slot)) == null)
                return $"No item binded to slot {slot}, please verify your inventory.";
            else if (player.inventory.GetCount(slot) < amount)
                return $"Missing {amount - player.inventory.GetCount(slot)} {it.name} in player's inventory" +
                    $" for this transaction.";
            else if (!sb.stock.CanContain(it, amount, sb.stats.StorageSpace))
                return $"{player.Area.name}'s storage cannot contain {amount}x {it.name}";
            sb.stock.Add(it, amount, sb.stats.StorageSpace);
            player.inventory.Remove(slot, amount);
            player.SaveFileMongo();
            await player.Area.UploadToDatabase();
            return $"Deposited {amount} {it.name} in {player.Area.name}'s Storage.";
        }
        async Task<string> SHWithdrawItem(Player player, int slot, int amount)
        {
            OldSandBox sb = player.Area.sandbox;
            Item it;
            if ((it = sb.stock.GetItem(slot)) == null)
                return $"No item binded to slot {slot}, please verify area stock.";
            else if (sb.stock.GetCount(slot) < amount)
                return $"Missing {amount - sb.stock.GetCount(slot)} {it.name} in {player.Area.name}'s stock" +
                    $" for this transaction.";
            else if (!player.inventory.CanContain(it, amount, sb.stats.StorageSpace))
                return $"{player.name}'s inventory cannot contain {amount}x {it.name}";
            player.inventory.Add(it, amount, sb.stats.StorageSpace);
            sb.stock.Remove(slot, amount);
            player.SaveFileMongo();
            await player.Area.UploadToDatabase();
            return $"Withdrew {amount} {it.name} from {player.Area.name}'s Storage.";
        }
        //Building
        async Task StrongholdBuild(Player player, string bn, int tier, MsgType type)
        {
            TileSchematic bs = TileSchematic.GetSchem(bn, tier);
            string result = bs.HasFunds(player.Area.sandbox);
            if (result == null)
                await player.NewUI(await ReplyAsync(embed: player.UserEmbedColor(bs.Embed(
                    bn, 0)).Build()),
                    type, $"{bn};{0}");
            else await DUtils.DeleteBothMsg(Context, await ReplyAsync(result + Environment.NewLine +
                "You must place materials and funds in your stronghold for them to be " +
                "used in constructions"));
        }
        internal static async Task BuildBuilding(Area stronghold, string BuildingName, int tier, 
            ISocketMessageChannel chan)
        {
            string result = TileSchematic.GetSchem(BuildingName, tier)
                .ConsumeSchematic(stronghold.sandbox);
            if (result == null)
            {
                    DUtils.DeleteMessage(await chan.SendMessageAsync(
                        stronghold.sandbox.Build(BuildingName, tier)));
                await stronghold.UploadToDatabase();
            }
            else DUtils.DeleteMessage(await chan.SendMessageAsync(result + Environment.NewLine +
                "You must place materials and funds in your stronghold for them to be " +
                "used in constructions"));
        }
        internal static async Task UpgradeBuilding(Area stronghold, int index, int tier, 
            ISocketMessageChannel chan)
        {
            string result = TileSchematic.GetSchem(stronghold.sandbox.buildings[index].Name, tier)
                .ConsumeSchematic(stronghold.sandbox);
            if (result == null)
            {
                    DUtils.DeleteMessage(await chan.SendMessageAsync(stronghold.sandbox.UpgradeBuilding(
                        index, tier)));
                await stronghold.UploadToDatabase();
            }
            else DUtils.DeleteMessage(await chan.SendMessageAsync(result + Environment.NewLine +
                "You must place materials and funds in your stronghold for them to be " +
                "used in constructions"));
        }

        #endregion
    }
}
