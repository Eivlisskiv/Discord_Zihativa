using AMI.Methods;
using AMI.Module;
using AMI.Neitsillia.Areas.AreaPartials;
using AMI.Neitsillia.Encounters;
using AMI.Neitsillia.Items;
using AMI.Neitsillia.Items.Abilities;
using AMI.Neitsillia.Items.Perks.PerkLoad;
using AMI.Neitsillia.NeitsilliaCommands;
using AMI.Neitsillia.NPCSystems;
using AMI.Neitsillia.User;
using AMI.Neitsillia.User.PlayerPartials;
using AMI.Neitsillia.User.UserInterface;
using AMYPrototype;
using AMYPrototype.Commands;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AMI.Neitsillia.Combat
{
    public class CombatCommands : ModuleBase<AMI.Commands.CustomCommandContext>
    {
        static Random Rng => Program.rng;

        [Command("Cast", true)]
        [Summary("Cast and ability on a target. Examples: \n"
            + "`cast` : uses brawl on the first live enemy \n"
            + "`cast {ability name}` : use ability with default target (first enemy for attacks, self for defense) \n"
            + "`cast {ability name} {target}` : uses attack ability on the target \n"
            )]
        public async Task Attack([Remainder] string arguments = null)
        {
            Player player = Context.Player;
            if (!player.IsEncounter("Combat") && !player.IsEncounter("NPC"))
                await DUtils.Replydb(Context, "There are no targets available.");
            else if (player.Party != null && player.IsEncounter("NPC") && player.Party.UpdateFollower(player.Encounter.npc))
                await DUtils.Replydb(Context, "There are no targets available.");
            else
            {
                string[] temp = GetAbilityAndTarget(player, arguments?.Split(' ') ?? new string[0]);
                if (player.duel != null && player.duel.abilityName != null
                    && player.duel.abilityName.StartsWith("~"))
                    throw NeitsilliaError.ReplyError($"{player.name} may not change their turn action from {player.duel.abilityName[1..]}");

                player.duel ??= new DuelData(null);
                player.duel.target = temp[1];
                player.duel.abilityName = temp[0];

                if(temp[0].ToLower() != "brawl")
                    player.QuestTrigger(Items.Quests.Quest.QuestTrigger.QuestLine, "TIV");
                if(temp[1] != "Default")
                    player.QuestTrigger(Items.Quests.Quest.QuestTrigger.QuestLine, "TV");

                player.SaveFileMongo();
                if (player.Encounter.IsNPC())
                {
                    await DUtils.Replydb(Context, "There are no targets available.");
                    return;

                    //player.Encounter.Name = Encounter.Names.Mob;
                    //player.Encounter.mobs = new NPC[1];
                    //player.Encounter.mobs[0] = player.Encounter.npc;
                }
                if (player.Encounter.Name == Encounter.Names.PVP)
                    await PVPTurn(player, temp[0], Context.Channel);
                else
                    await TurnCombat(player, temp[0], Context.Channel);
            }
        }

        internal static string[] GetAbilityAndTarget(Player player, string[] args)
        {
            if (player.duel == null)
                player.duel = new DuelData(null);
            string[] results = new[] { "Brawl", "Default"};
            switch(args.Length)
            {
                case 0: break;
                case 1:
                    {
                        if (args[0].Length > 2)
                            results = ParseAbility(player, args[0], results);
                        else
                            results = ParseTarget(args[0], results);
                    }break;
                default:
                    {
                        if(args[^1].Length > 2)
                            results = ParseAbility(player, ArrayM.ToString(args), results);
                        else
                        results = ParseAbility(player, ArrayM.ToKString(args, skipIndex: args.Length - 1), 
                            ParseTarget(args[^1], results));
                        
                    }
                    break;
            }
            
            return results;
        }

        static string[] ParseAbility(Player player, string name, string[] results)
        {
            name = name.Trim();
            if (player.HasAbility(name, out _))
            { results[0] = name; return results; }
            throw NeitsilliaError.ReplyError($"{player.name} does not have ability {name}. To view character's abilities, type `~abilities`.");
        }

        static string[] ParseTarget(string name, string[] results)
        {
            switch(name[0])
            {
                case '0':
                case 'M':
                case 'm':
                    results[1] = "0,";break;
                case '1':
                case 'P':
                case 'p':
                    results[1] = "1,"; break;
                default: throw NeitsilliaError.ReplyError($"target {name} invalid");
            }
            try
            {
                if (int.TryParse(name[1].ToString(), out int res))
                { results[1] += res.ToString(); return results; }
                throw NeitsilliaError.ReplyError($"target {name} invalid");
            }
            catch(Exception)
            {
                throw NeitsilliaError.ReplyError($"target {name} invalid");
            }
        }

        [Command("Run", true)]
        public async Task Run()
            => await Run(Context.Player, Context.Channel, false);

        internal static async Task Run(Player player, ISocketMessageChannel chan, bool edit)
        {
            if (!player.IsEncounter("Combat"))
                await chan.SendMessageAsync("You are not in combat");
            else
            {
                if(!player.IsSolo)
                {
                    player.duel ??= new DuelData(null);
                    await TurnCombat(player, "~Run", chan);
                    return;
                }
                NPC[] mob = player.Encounter.mobs;
                int accuracyMod = Verify.MinMax(player.Agility() - mob[Program.rng.Next(mob.Length)].Agility()
                    , ReferenceData.maximumAgilityDifference, ReferenceData.maximumAgilityDifference * -1);
                int runchance = Rng.Next(0, 101) + accuracyMod;
                if (runchance >= (110 - ReferenceData.hitChance)) //>=80
                {
                    player.EndEncounter();
                    EmbedBuilder fight = new EmbedBuilder
                    {
                        Title = "Combat Escape"
                    };
                    fight.AddField("Escape Successful", "You ran away from combat");
                    await player.NewUI(await chan.SendMessageAsync(embed: fight.Build()),
                         MsgType.Main);

                    PerkLoad.CheckPerks(player, Perk.Trigger.EndFight, player);

                    if (player.Area.IsDungeon && 
                        (player.Area.arena != null || player.AreaInfo.floor >= player.Area.floors))
                    {
                        if(player.Area.arena != null)
                            await EndArenaChallenge(player, chan);

                        await Program.data.database.DeleteRecord<Area>(
                            player.AreaInfo.table.ToString(), player.Area.AreaId, "AreaId");
                        await player.SetArea(Area.LoadArea(
                            player.Area.GeneratePath(false) + player.Area.parent, null),
                            player.AreaInfo.floor);
                    }
                    player.duel = null;
                    player.SaveFileMongo();
                }
                else
                    await TurnCombat(player, "~Run", chan, edit);
            }
        }

        private static async Task EndArenaChallenge(Player player, IMessageChannel chan)
        {
            Encounter enc = new Encounter(Encounter.Names.Loot, player);
            await player.Area.arena.EndChallenge(enc, player.Area, player.AreaInfo.floor);
            player.NewEncounter(enc, true);
            await chan.SendMessageAsync(embed: enc.GetEmbed(null).Build());
        }

        internal static async Task AutoBrawl(Player player, ISocketMessageChannel chan)
        {
            if(player.duel == null)
            {
                player.duel = new DuelData(player.Encounter.Name == Encounter.Names.PVP ? "" : null)
                {
                    abilityName = "Brawl",
                    target = "Default"
                };
            }

            if (player.duel.abilityName == null || !player.HasAbility(player.duel.abilityName, out _))
                player.duel.abilityName = player.abilities[0].name;
            await TurnCombat(player, player.duel.abilityName, chan, true);
        }
        internal static async Task TurnCombat(Player player, string abilityName, ISocketMessageChannel chan, bool editMessage = false)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();

            NPC[] mob = player.Encounter.mobs;
            Ability ability = null;
            if (abilityName != null)
            {
                ability = player.GetAbility(abilityName);
                if (ability != null)
                    abilityName = ability.name;
                player.duel.abilityName = abilityName;
            }
            //
            Combat combat = player.Party != null ? 
                await PartyCombat(player, mob, chan) 
                : new Combat(mob, new Player[] { player });

            if (combat == null) return;

            combat.InitiateAll();
            combat.Turn();
            if (!player.IsSolo)
            {
                foreach (var cb in combat.playerParty)
                    if (cb.character is Player pplayer)
                        pplayer.duel.abilityName = null;

            }
            else if (player.duel.abilityName.StartsWith("~")) player.duel.abilityName = null;

            if (player.Party != null)
                await player.PartyKey.SaveAsync();
            //
            string p = combat.GetResultInfo(combat.playerParty, "p");
            string m = combat.GetResultInfo(combat.mobParty, "m");
            //
            EmbedBuilder fight = DUtils.BuildEmbed(
                (player.Party?.partyName ?? player.name) + " VS " + (mob.Length == 1 ? mob[0].displayName : "Creatures"),
                "Turn " + (player.Encounter.turn++),
                ((player.Party == null || player.Party.members.Count == 1) ? $"{EUI.brawl} : {abilityName}" : null)
                + $"           [{watch.ElapsedMilliseconds}ms]",
                player.userSettings.Color,
                DUtils.NewField("__Player Party__", p, true),
                DUtils.NewField("__Creature Party__", m, true)
                );


            CombatEndHandler ceh = new CombatEndHandler(combat, player.Party, player.Encounter, player.Area);
            MsgType resultType = await ceh.Handle(fight);

            if (ability == null && player.ui != null &&
                player.ui.type == MsgType.Combat && player.ui.data != null)
                abilityName = player.ui.data;


            IUserMessage reply;

            if (editMessage)
                reply = await player.EditUI(null, fight.Build(), chan, resultType, abilityName);
            else
            {
                reply = await chan.SendMessageAsync(embed: fight.Build());
                await player.NewUI(reply, resultType, abilityName);
            }

            if (!player.IsSolo)
            {
                player.Party.UpdateUI(player, p => p.duel.abilityName);
            }
        }

        private static async Task<Combat> PartyCombat(Player player, NPC[] mob, IMessageChannel chan)
        {
            CharacterMotherClass[] ps = new CharacterMotherClass[player.Party.MemberCount];
            List<Player> escaping = new List<Player>();

            int? lowestAGI = null;

            int i = 0;
            for (; i < player.Party.members.Count; i++)
            {
                PartyMember m = player.Party.members[i];
                Player p = m.id == player.userid ? player : m.LoadPlayer();
                ps[i] = p;

                if (p.duel?.abilityName == "~Run")
                    escaping.Add(p);

                if ((p.duel == null || p.duel.abilityName == null || p.duel.target == null) && !p.IsDead())
                {
                    player.SaveFileMongo();
                    throw NeitsilliaError.ReplyError("Awaiting other Party member(s) action");
                }

                int agi = player.Agility();
                lowestAGI = Math.Min(lowestAGI ?? agi, agi);
            }
            for (int j = 0; i + j < ps.Length; j++)
                ps[i + j] = player.Party.NPCMembers[j];

            if (escaping.Count > 0)
            {
                if (escaping.Count == player.Party.members.Count)
                {
                    int accuracyMod = Verify.MinMax((lowestAGI ?? 0) - mob[Program.rng.Next(mob.Length)].Agility()
                        , ReferenceData.maximumAgilityDifference, ReferenceData.maximumAgilityDifference * -1);

                    int runchance = Rng.Next(0, 101) + accuracyMod;
                    if (runchance >= (110 - ReferenceData.hitChance))
                    {
                        EmbedBuilder fight = new EmbedBuilder
                        {
                            Title = "Combat Escape"
                        };
                        fight.AddField("Escape Successful", "You ran away from combat");
                        await chan.SendMessageAsync(embed: fight.Build());
                        await PartyEscape(player);

                        if (player.Area.arena != null)
                            await EndArenaChallenge(player, chan);

                        return null;
                    }
                }
                else
                {
                    //TODO
                    //When some, but not all, players can escape
                    //For now, ignore this. No one is left behind
                }
            }
            return new Combat(player.Encounter.mobs, ps);
        }

        static double SneakattackMultiplier(int attackerAgi, int defenderAgi)
        {
            double crit = ((defenderAgi - attackerAgi) / 100) + 1;
            if (crit > 1.5) return 1.5;
            return crit;
        }

        static async Task PartyEscape(Player player)
        {
            if (player.Party == null)
            {
                PerkLoad.CheckPerks(player, Perk.Trigger.EndFight, player);
                player.SaveFileMongo();
            }
            else
            {
                foreach (var m in player.Party.members)
                {
                    Player p = m.id == player.userid ? player : m.LoadPlayer();

                    PerkLoad.CheckPerks(p, Perk.Trigger.EndFight, p);
                    player.duel = null;
                    p.SaveFileMongo();
                }

                foreach (var n in player.Party.NPCMembers)
                {
                    PerkLoad.CheckPerks(n, Perk.Trigger.EndFight, n);
                }

                await player.Party.SaveData();
            }
            player.EndEncounter();
        }

        [Command("Duel")]
        public async Task Duel(IUser enemyUser)
        {
            Context.WIPCheck();
            Player player = Context.Player;
            Player enemy = null;
            try { enemy = Player.Load(enemyUser.Id); }catch(Exception e)
            { await DUtils.Replydb(Context, $"{enemyUser.Username} : {e.Message}" ); }
            if (enemy != null)
            {
                if(player.IsEncounter("Combat"))
                    await DUtils.Replydb(Context, $"{player.name} may not start a duel while in combat");
                else if (player.Party != null)
                    await DUtils.Replydb(Context, $"{player.name} may not start a duel while in a party");
                else if (player.AreaInfo.path != enemy.AreaInfo.path)
                    await DUtils.Replydb(Context, $"{player.name} may only duel players in the same area.");
                else if (enemy.IsEncounter("Combat"))
                    await DUtils.Replydb(Context, $"{enemy.name} may not accept a duel while in combat");
                else
                await enemy.NewUI(await ReplyAsync($"{enemyUser.Mention}, you have been challenged to a duel by {player.name} ({Context.User.Username}). Do you accept?"),
                       MsgType.DuelOffer, $"{player.userid}/{player.name}");
            }
        }
        internal static async Task PVPTurn(Player player, string abilityName, ISocketMessageChannel chan)
        {
            Player enemy = Player.Load(player.duel.opponentPlayerPath);
            if (enemy == null)
            {
                player.EndEncounter();
                await chan.SendMessageAsync("Opponent not found, Duel canceled");
            }
            else
            {
                if (abilityName.StartsWith("~") || player.HasAbility(abilityName, out _))
                { 
                    player.duel.abilityName = abilityName;
                    player.duel.replyToChannel = chan.Id;
                    player.SaveFileMongo();
                }
                if (enemy.duel.abilityName == null)
                    DUtils.DeleteMessage(await chan.SendMessageAsync("Awaiting opponent action"), 0.4);
                else
                {
                    Ability playerAbility = player.GetAbility(player.duel.abilityName);
                    Ability enemyAbility = enemy.GetAbility(enemy.duel.abilityName);
                    Combat combat = new Combat(player, playerAbility,
                        enemy, enemyAbility);
                    combat.InitiateAll();
                    combat.Turn();
                    if (player.duel.abilityName == "~Consume")
                        combat.playerParty[0].action = CombatResult.Action.Consume;
                    if (enemy.duel.abilityName == "~Consume")
                        combat.mobParty[0].action = CombatResult.Action.Consume;
                    string playerResults = combat.playerParty[0].GetResultInfo(null);
                    string enemyResults = combat.mobParty[0].GetResultInfo(null);
                    //
                    await SendPVPResults(player, enemy, playerResults, enemyResults);
                    await SendPVPResults(enemy, player, enemyResults, playerResults);
                    bool playerDead = player.IsDead();
                    bool enemyDead = enemy.IsDead();
                    if (playerDead || enemyDead)
                    {
                        if (playerDead) await player.Respawn(false, false);
                        if (enemyDead) await enemy.Respawn(false, false);

                        player.EndEncounter();
                        enemy.EndEncounter();
                    }
                    else
                    {
                        player.duel.abilityName = null;
                        enemy.duel.abilityName = null;
                    }
                    player.SaveFileMongo();
                    enemy.SaveFileMongo();
                }
            }
        }
        internal static async Task SendPVPResults(Player player, Player enemy, params string[] results)
        {
            EmbedBuilder fight = new EmbedBuilder();
            fight.WithTitle(player.name + " VS " + enemy.name);
            fight = player.UserEmbedColor(fight);
            fight.AddField(player.name, results[0], true);
            fight.AddField(enemy.name, results[1], true);
            //

            string abilityName = player.duel.abilityName;
            player.duel.abilityName = null;
            bool end = false;
            IMessageChannel chan = (IMessageChannel)Program.clientCopy.GetChannel(player.duel.replyToChannel);
            if (player.IsDead() && enemy.IsDead())
            { fight.WithFooter("Both combatants have fallen."); end = true; }
            else if (player.IsDead())
            { fight.WithFooter("You have been defeated."); end = true; }
            else if (enemy.IsDead())
            { fight.WithFooter("You are victorious!"); end = true; }
            if (!end)
                await player.NewUI(await chan.SendMessageAsync(embed: fight.Build()), MsgType.Combat, abilityName);
            else
                await chan.SendMessageAsync(embed: fight.Build());
            
        }

        [Command("Train")]
        [Summary("Spawns a training dummy to practice combat")]
        public async Task Train()
        {
            Player player = Context.Player;
            if(player.IsEncounter("Combat")) { await ReplyAsync("You main not train while in combat."); return; }
            if(player.Area.type == Areas.AreaType.Arena) { await ReplyAsync("You main not train in an Arena."); return; }
            if (!player.IsLeader) { await ReplyAsync("You must be party leader to initiate a training session."); return; }

            player.NewEncounter(Encounter.Names.Mob, true);
            player.Encounter.mobs = new NPC[]
            {
                NPC.TrainningDummy(player.level, 1), NPC.TrainningDummy(player.level, 2),
                NPC.TrainningDummy(player.level, 3), NPC.TrainningDummy(player.level, 4)
            };

            IUserMessage reply = await ReplyAsync(embed: player.Encounter.GetEmbed().Build());
            if (player.IsSolo) await player.NewUI(reply, MsgType.Combat);
            else player.Party.UpdateUI(player, null);
        }

    }
}
