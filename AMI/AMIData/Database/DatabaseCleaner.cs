﻿using AMI.AMIData;
using AMI.Methods;
using AMI.Neitsillia.User.PlayerPartials;
using AMYPrototype;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AMI.Handlers
{
    class DatabaseCleaner
    {
        readonly MongoDatabase database;

        public DatabaseCleaner(MongoDatabase database)
        {
            this.database = database;
        }

        public async Task StartCleaning()
        {
            if (!Program.FirstBoot) return;

            await CleanUsers();

            await CleanAreas();

            var players = await database.LoadRecordsAsync<Player>("Character");

            await CleanParties(players);
            await CleanQuestBoards(players);
            await CleanDecks(players);
            await CleanEggPockets(players);
            await CleanFaith(players);

            await CleanHouses();
        }

        private async Task CleanUsers()
        {
            Log.LogS("Deleting empty User entries");
            await database.database.GetCollection<Neitsillia.User.BotUser>("User").DeleteManyAsync(
                "{$and: [ {loaded:null}, {ui:null}, {$or: [{ResourceCrates:null}, {ResourceCrates: [0,0,0,0,0]} ]} ]}");
        }

        private async Task CleanAreas()
        {
            Log.LogS("Cleaning Areas");
            var list = database.LoadRecords<Neitsillia.Areas.AreaPartials.Area>("Area");

            for(int i = 0; i < list.Count; i++)
            {
                var area = list[i];

                //If the area or nest does not exist
                if(area.junctions == null)
				{
                    area.junctions = new List<NeitsilliaEngine.Junction>();
				}

                if (area.junctions.Count == 0)
                {
                    Log.LogS($"{area.AreaId} has no junctions");
                }
                else
                {
                    area.junctions.RemoveAll(j =>
                        !list.Exists(a => a.AreaId == j.filePath) &&
                        database.LoadRecord<Neitsillia.Areas.Nests.Nest, string>("Nest", j.filePath) == null
                    );
                }

				if (area.parent != null)
				{
					string parentPath = area.GeneratePath(false) + area.parent;
					bool hasParentJunction = area.junctions.Any(j => j.filePath == parentPath);

					if (!hasParentJunction)
					{
						area.junctions.Add(new NeitsilliaEngine.Junction(area.parent, 0, parentPath));
					}
				}

                if (area.passiveEncounter == null)
                {
                    area.passiveEncounter = area.type switch
                    {
                        Neitsillia.Areas.AreaType.Stronghold => new[]{ "NPC" },
                        Neitsillia.Areas.AreaType.Town => new[]{ "NPC" },
                        Neitsillia.Areas.AreaType.Tavern => new[]{ "NPC" },

                        Neitsillia.Areas.AreaType.Mines => new string[] { "NPC", "Dungeon" },
                        Neitsillia.Areas.AreaType.Ruins => new string[] { "NPC", "Dungeon" },
                        Neitsillia.Areas.AreaType.Caves => new string[] { "NPC", "Dungeon" },
                        Neitsillia.Areas.AreaType.Wilderness => new string[] { "NPC", "Dungeon" },
                        Neitsillia.Areas.AreaType.Shrine => new string[] { "Puzzle" },

                        _ => null
                    };
                }

                if (area.loot != null)
                {
                    for (int k = 0; k < area.loot.Length; k++)
                    {
                        for (int j = 0; j < area.loot[k].Length; j++)
                            area.loot[k][j] = area.loot[k][j].Trim();
                    }
                }

                await database.UpdateRecordAsync("Area", MongoDatabase.FilterEqual<Neitsillia.Areas.AreaPartials.Area, string>("_id", area.AreaId), area);
            }
        }

        async Task CleanHouses()
        {
            var items = await database.LoadRecordsAsync<Neitsillia.Areas.House.House>(null);
            await Utils.MapAsync(items, async (item, index) =>
            {
                if (item.sandbox == null)
                {
                    item.sandbox = new Neitsillia.Areas.Sandbox.Sandbox();
                    await item.Save();
                    Log.LogS($"Fixed {item.GetType().Name} {item._id}");
                }
                else if (item.sandbox.tier == 0)
                {
                    item.sandbox.tier = 1;
                    await item.Save();
                    Log.LogS($"Fixed {item.GetType().Name} {item._id}");
                }

                return true;
            });
        }

        private async Task CleanParties(List<Player> players)
        {
            var list = database.LoadRecords<Neitsillia.NeitsilliaCommands.Party>("Party");

            for (int k = 0; k < list.Count; k++)
            {
                var party = list[k];

                for(int i = 0; i < party.members.Count;)
                {
                    var player = players.Find(p => p._id == party.members[i].Path);
                    if (player == null || player.PartyKey?._id != party._id) party.members.RemoveAt(i);
                    else i++;
                }
                if(party.members.Count == 0)
                {
                    party.DisbandNPCs(null);
                    await UniqueChannels.Instance.SendToLog($"Party {party} was cleared due to no player members found");
                    await database.DeleteRecord<Neitsillia.NeitsilliaCommands.Party>("Party", party._id);
                }
                else if (party.partyName == null)
                {
                    await database.DeleteRecord<Neitsillia.NeitsilliaCommands.Party>("Party", party._id);

                    party.partyName = party._id;
                    party._id = party._id.ToLower();

                    database.SaveRecord("Party", party);
                    

                    await UniqueChannels.Instance.SendToLog($"Updated party {party}'s id: {party._id}.");
                    foreach (var m in party.members)
                    {
                        var player = m.LoadPlayer();
                        player.PartyKey._id = party._id;
                        player.EncounterKey._id = party.EncounterKey;

                        player.SaveFileMongo();
                    }
                }
            }
        }

        private async Task CleanQuestBoards(List<Player> players)
        {
            var items = await database.LoadRecordsAsync<Neitsillia.User.DailyQuestBoard>("DailyQuestBoard");
            await Utils.MapAsync(items, async (item, index) =>
            {
                if (!players.Exists(p => p._id == item._id))
                { 
                    await database.DeleteRecord<Neitsillia.User.DailyQuestBoard>("DailyQuestBoard", item._id);
                    Log.LogS($"Cleaned {item.GetType().Name} {item._id}");
                }
                return true;
            });
        }

        private async Task CleanDecks(List<Player> players)
        {
            var items = await database.LoadRecordsAsync<Neitsillia.Gambling.Cards.Deck>("Decks");
            await Utils.MapAsync(items, async (item, index) =>
            {
                var player = players.Find(p => p._id == item._id);
                if (player?.GamblingHandKey?._id == null)
                { 
                    await database.DeleteRecord<Neitsillia.Gambling.Cards.Deck>("Decks", item._id);
                    Log.LogS($"Cleaned {item.GetType().Name} {item._id}");
                }
                return true;
            });
        }

        private async Task CleanEggPockets(List<Player> players)
        {
            var items = await database.LoadRecordsAsync<Neitsillia.NPCSystems.Companions.EggPocket>("EggPocket");
            await Utils.MapAsync(items, async (item, index) =>
            {
                if (!players.Exists(p => p._id == item._id))
                {
                    await database.DeleteRecord<Neitsillia.NPCSystems.Companions.EggPocket>("EggPocket", item._id);
                    Log.LogS($"Cleaned {item.GetType().Name} {item._id}");
                }
                return true;
            });
        }

        private async Task CleanFaith(List<Player> players)
        {
            var items = await database.LoadRecordsAsync<Neitsillia.Religion.Faith>("Faith");
            await Utils.MapAsync(items, async (item, index) =>
            {
                if (!players.Exists(p => p._id == item._id))
                {
                    await database.DeleteRecord<Neitsillia.Religion.Faith>("Faith", item._id);
                    Log.LogS($"Cleaned {item.GetType().Name} {item._id}");
                }
                return true;
            });
        }
    }
}
