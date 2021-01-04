using AMI.AMIData;
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

            //await CleanUsers();

            //await CleanNests();

            //await CleanAreas();

            //await CleanParties();
        }

        private async Task CleanUsers()
        {
            await database.database.GetCollection<Neitsillia.User.BotUser>("User").DeleteManyAsync(
                "{$and: [ {loaded:null}, {ui:null}, {$or: [{ResourceCrates:null}, {ResourceCrates: [0,0,0,0,0]} ]} ]}");
        }

        private async Task CleanNests()
        {
            await database.database.GetCollection<Neitsillia.Areas.Nests.Nest>("Nests").DeleteManyAsync("{}");
        }

        async Task CleanAreas()
        {
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
                    area.junctions.RemoveAll(j => j.destination.EndsWith("Nest"));


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

        private async Task CleanParties()
        {
            var list = database.LoadRecords<Neitsillia.NeitsilliaCommands.Party>("Party");

            for (int k = 0; k < list.Count; k++)
            {
                var party = list[k];

                for(int i = 0; i < party.members.Count;)
                {
                    var player = party.members[i].LoadPlayer();
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
    }
}
