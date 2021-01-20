﻿using AMI.Methods;
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
using MongoDB.Bson.Serialization.Attributes;
using Neitsillia.Items.Item;
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
            "Survive never ending waves of enemies with no breaks in between each fight.",
            "Test Mode 1.",
            "Test Mode 2.",
            "Test Mode 3.",
            "Test Mode 4.",
            "Test Mode 5.",
        };
            
        internal static async Task Service(Player player, ISocketMessageChannel chan)
        {
            await player.NewUI("", DUtils.BuildEmbed("Arena Lobby Services",
                $"{EUI.sideQuest} View arena quests" + Environment.NewLine +
                $"{EUI.bounties} View arena challenges",
                null, player.userSettings.Color()).Build(), chan, MsgType.ArenaService);
        }

        internal static async Task SelectMode(Player player, int i, ISocketMessageChannel chan, bool edit = false)
        {
            Array modes = Enum.GetValues(typeof(ArenaMode));
            //Loop index
            if (i >= modes.Length)
                i = 0;
            else if (i < 0)
                i = modes.Length - 1;
            //Create Embed
            EmbedBuilder embed = DUtils.BuildEmbed(
                "Arena", "Select Challenge", color : player.userSettings.Color(),
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
            string boolArray, ISocketMessageChannel chan, bool edit = false)
        {
            Array mods = Enum.GetValues(typeof(ArenaModifier.ArenaModifiers));
            //Loop index
            if (page >= mods.Length)
                page = 0;
            else if (page < 0)
                page = mods.Length - 1;

            EmbedBuilder embed = DUtils.BuildEmbed(
                "Arena", "Activate Modifiers", color: player.userSettings.Color(),
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
            };

            await player.SetArea(dungeon);
            player.SaveFileMongo();

            return dungeon;
        }

        public ArenaMode gameMode;
        public ArenaModifier Modifiers;

        public Arena(bool json) { }
        public Arena(ArenaMode mode, string[] boolArray = null)
        {
            gameMode = mode;
            Modifiers = new ArenaModifier(boolArray);
        }

        private Inventory GetLoot(Area arena, int level)
        {
            long score = Modifiers.CurrentScore;
            Inventory loot = new Inventory();
            for(; score > 0; score -= 100)
            {
                if (Program.Chance(score))
                {
                    Item item;
                    if (Program.Chance(10))
                        item = Item.CreateRune(1);
                    else if (arena.ValidTable(arena.loot))
                    {
                        int t1 = ArrayM.IndexWithRates(arena.loot.Length - 1, Program.rng);
                        int t2 = ArrayM.IndexWithRates(arena.loot[t1].Count - 1, Program.rng);
                        //gets the item in that tier, creates a new item and adds it to the loot event collection
                        item = Item.LoadItem(arena.loot[t1][t2].Trim());
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

        internal EmbedBuilder Explore(Area area, Player player, EmbedBuilder embed)
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
            string[] mobNames = ArenaQuest.BadBatch(level, 1).mobs;

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

        public async Task EndChallenge(Encounter enc, Area arena)
        {
            //Game mode specific rewards
            switch (gameMode)
            {
                default:
                    if(arena.ValidTable(arena.loot))
                        enc.AddLoot(GetLoot(arena, arena.level));
                    break;
            }

            //Add user scores
            //await TODO

            //Koins rewards
            enc.koinsToGain += NumbersM.NParse<long>(Modifiers.CurrentScore * Modifiers.koinMult);
            enc.xpToGain += NumbersM.NParse<long>((Modifiers.CurrentScore * arena.level) * Modifiers.xPMult);
        }

        internal bool WaveProgress(int floor)
        {
            return Modifiers.WaveProgress(floor * gameMode switch
            {
                ArenaMode.Survival => 5,
                _ => 1,
            });
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
