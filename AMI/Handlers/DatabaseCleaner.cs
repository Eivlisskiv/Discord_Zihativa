using AMI.AMIData;
using AMI.Methods;
using AMI.Neitsillia.User.PlayerPartials;
using AMYPrototype;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AMI.Handlers
{
    class DatabaseCleaner
    {
        MongoDatabase database;

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

        private async Task CleanNests()
        {
            await database.database.GetCollection<Neitsillia.Areas.Nests.Nest>("Nests").DeleteManyAsync("{}");
        }

        async Task CleanAreas()
        {
            Log.LogS("Cleaning Areas");
            var list = database.LoadRecords<Neitsillia.Areas.AreaPartials.Area>("Area");

            for(int i = 0; i < list.Count; i++)
            {
                var area = list[i];

                if (area.type == Neitsillia.Areas.AreaType.Nest)
                {
                    await database.DeleteRecord<Neitsillia.Areas.AreaPartials.Area>("Area", area.AreaId);
                    _ = UniqueChannels.Instance.SendToLog($"Cleared nest {area.name}");
                }
                else
                {
                    //If the area or nest does not exist
                    area.junctions.RemoveAll(j => 
                        !list.Exists(a => a.AreaId == j.filePath) &&
                        database.LoadRecord<Neitsillia.Areas.Nests.Nest, string>("Nest", j.filePath) == null
                    );

                    if (area.passives != null)
                    {

                        List<string> passives = new List<string>();
                        for (int k = 0; k < area.passives.Length; k++)
                        {
                            for (int j = 0; j < area.passives[k].Length; j++)
                                passives.Add(area.passives[k][j]);
                        }

                        area.passiveEncounter = passives.ToArray();
                        area.passives = null;
                    }

                    if (area.loot != null)
                    {
                        for (int k = 0; k < area.loot.Length; k++)
                        {
                            for (int j = 0; j < area.loot[k].Count; j++)
                                area.loot[k][j] = area.loot[k][j].Trim();
                        }
                    }

                    await database.UpdateRecordAsync("Area", MongoDatabase.FilterEqual<Neitsillia.Areas.AreaPartials.Area, string>("_id", area.AreaId), area);
                }
            }
        }

        async Task CleanHouses()
        {
            var items = await database.LoadRecordsAsync<Neitsillia.Areas.House.House>(null);
            await Utils.MapAsync(items, async (item, index) =>
            {
                if (item.sandbox == null) item.sandbox = new Neitsillia.Areas.Sandbox.Sandbox();
                else if (item.sandbox.tier == 0) item.sandbox.tier = 1;

                if(item.storage != null)
                {
                    item.sandbox.storage.Add(item.storage, -1);
                    item.storage = null;
                }

                await item.Save();

                Log.LogS($"Fixed {item.GetType().Name} {item._id}");
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
