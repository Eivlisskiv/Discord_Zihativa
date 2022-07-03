using AMI.AMIData;
using AMI.Methods;
using AMI.Neitsillia.Areas.AreaPartials;
using AMI.Neitsillia.Collections;
using AMI.Neitsillia.Encounters;
using AMI.Neitsillia.NPCSystems;
using AMI.Neitsillia.User.PlayerPartials;
using AMI.Neitsillia.User.UserInterface;
using AMYPrototype;
using AMYPrototype.Commands;
using Discord;
using Discord.WebSocket;
using AMI.Neitsillia.Items.ItemPartials;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AMI.Neitsillia.Areas.Arenas
{
    public class Arena
    {
        static AMIData.MongoDatabase Database => Program.data.database;
        public enum ArenaMode { Survival, };
        static readonly string[] ModesDesc =
        {
            "Survive never ending waves of enemies to test your endurance."
            + Environment.NewLine + "Bonus: 5x XP" 
            + Environment.NewLine + "Loot and coins are given out once you quit or are deafeated (no defeat cost)",
            "Test Mode 1.",
            "Test Mode 2.",
            "Test Mode 3.",
            "Test Mode 4.",
            "Test Mode 5.",
        };
            
        internal static async Task Service(Player player, IMessageChannel chan)
        {
            await player.NewUI("", DUtils.BuildEmbed("Arena Lobby Services",
                $"{EUI.sideQuest} View arena quests" + Environment.NewLine +
                $"{EUI.bounties} View arena challenges",
                null, player.userSettings.Color).Build(), chan, MsgType.ArenaService);
        }

        internal static async Task SelectMode(Player player, int i, IMessageChannel chan, bool edit = false)
        {
            Array modes = Enum.GetValues(typeof(ArenaMode));
            //Loop index
            if (i >= modes.Length) i = 0;
            else if (i < 0) i = modes.Length - 1;
            //Create Embed
            EmbedBuilder embed = DUtils.BuildEmbed(
                "Arena", "Select Challenge", color : player.userSettings.Color,
                fields : DUtils.NewField($"**{(ArenaMode)i}** (Level {player.Area.level}+)", ModesDesc[i])
                );
            if (edit)
                await player.EditUI(null, embed.Build(), chan,
                    MsgType.ArenaGameMode, i.ToString());
            else
                await player.NewUI(await chan.SendMessageAsync(embed: embed.Build()),
             MsgType.ArenaGameMode, i.ToString());
        }

        internal static async Task SelectModifiers(Player player, string mode, int page,
            string boolArray, IMessageChannel chan, bool edit = false)
        {
            Array mods = Enum.GetValues(typeof(ArenaModifier.ArenaModifiers));
            //Loop index
            if (page >= mods.Length)
                page = 0;
            else if (page < 0)
                page = mods.Length - 1;

            EmbedBuilder embed = DUtils.BuildEmbed(
                "Arena", "Activate Modifiers", color: player.userSettings.Color,
                fields: DUtils.NewField( $"**{(ArenaModifier.ArenaModifiers)page}**" ,
                ArenaModifier.ModifiersDesc[page])
                );

            if(edit)
                await player.EditUI(null, embed.Build(), chan,
                    MsgType.ArenaModifiers, $"{mode};{page};{boolArray}");
            else
                await player.NewUI(await chan.SendMessageAsync(embed: embed.Build()),
                 MsgType.ArenaModifiers, $"{mode};{page};{boolArray}");
        }

        internal static async Task<Area> Generate(Area parent, Player player, int mode, string[] boolArray)
        {
            Area dungeon =
            new Area(AreaType.Arena, $"{parent.name} {(ArenaMode)mode}", parent)
            {
                eMobRate = 100,
                floors = -1,
                arena = new Arena((ArenaMode)mode, boolArray),
                junctions = new List<NeitsilliaEngine.Junction>() { new NeitsilliaEngine.Junction(parent, 0, 0) }
            };

            await player.SetArea(dungeon);
            player.SaveFileMongo();

            return dungeon;
        }

        public ArenaMode gameMode;
        public ArenaModifier Modifiers;

        public Arena(bool _) { }
        public Arena(ArenaMode mode, string[] boolArray = null)
        {
            gameMode = mode;
            Modifiers = new ArenaModifier(mode, boolArray);
        }

        private Inventory GetLoot(Area arena, int level)
        {
            long score = Modifiers.CurrentScore;
            Inventory loot = new Inventory();
            LootTables<string> lt = new LootTables<string>(arena.loot, Program.rng);
            for(; score > 0; score -= 100)
            {
                if (Program.Chance(score))
                {
                    Item item;
                    if (Program.Chance(10))
                        item = Item.CreateRune(1);
                    else if (arena.ValidTable(arena.loot))
                    {
                        item = Item.LoadItem(lt.GetItem(out _).Trim());
                        item.Scale(level);
                    }
                    else item = Item.RandomGear(level * 5, true);

                    loot.Add(item, 1, -1);

                    if (Program.Chance(20))
                        loot.Add(Item.CreateRepairKit(1), 1, -1);
                }
            }

            return loot;
        }

        internal EmbedBuilder Explore(Player player, EmbedBuilder embed)
        {
            return gameMode switch
            {
                ArenaMode.Survival => EncounterBadBatch(player, embed),
                _ => embed,
            };
        }

        private EmbedBuilder EncounterBadBatch(Player player, EmbedBuilder embed)
        {
            int level = Math.Max(player.Area.level, 1);
            string[] mobNames = ArenaQuest.BadBatch(level, (player.Party?.MemberCount ?? 1) + 1 ).mobs;

            NPC[] mobs = new NPC[mobNames.Length];

            for (int i = 0; i < mobNames.Length; i++)
            {
                NPC mob = NPC.GenerateNPC(level, mobNames[i]);
                mob.inventory.inv.Clear();
                mob.KCoins = 0;
                mobs[i] = mob;
            }

            player.NewEncounter(new Encounter("Mob", player)
            { mobs = mobs });

            embed.WithDescription("You've encountered a group of creatures");

            for (int i = 0; i < mobs.Length; i++)
            {
                NPC m = mobs[i];

                embed.AddField(
                    m.displayName + $" | m{i}",
                    "Level: " + m.level + Environment.NewLine +
                    "Health: " + m.health + '/' + m.Health() + Environment.NewLine
                );
            }

            player.SaveFileMongo();

            return embed;
        }

        public async Task EndChallenge(Encounter enc, Area arena, int floor)
        {
            //Game mode specific rewards
            switch (gameMode)
            {
                default:
                    if(arena.ValidTable(arena.loot))
                        enc.AddLoot(GetLoot(arena, arena.GetAreaFloorLevel(Program.rng, floor)));
                    break;
            }

            //Add user scores
            //await TODO

            //Koins rewards
            enc.koinsToGain += (long)(Modifiers.CurrentScore * Modifiers.koinMult);
            enc.xpToGain += (long)((Modifiers.CurrentScore * arena.level) * Modifiers.xpMult);
        }

        internal bool WaveProgress(double score)
        {
            return Modifiers.WaveProgress(NumbersM.CeilParseLong(score * (gameMode switch
            {
                ArenaMode.Survival => 5,
                _ => 1,
            })));
        }
    }
    //TEST - IGNORE
    class ArenaScoreList
    {
        //same as area (ArenaLobby) id
        public string _id;

               //charname\userid, score
        public SortedList<string, long> PlayerScores { get; set; }

        public ArenaScoreList()
        { }
    }
}
