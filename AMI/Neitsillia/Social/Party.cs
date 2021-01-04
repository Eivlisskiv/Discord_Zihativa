using AMI.Neitsillia.Areas;
using AMI.Neitsillia.Areas.AreaPartials;
using AMI.Neitsillia.NPCSystems;
using AMI.Neitsillia.NPCSystems.Companions;
using AMI.Neitsillia.NPCSystems.Companions.Pets;
using AMI.Neitsillia.User.PlayerPartials;
using AMI.Neitsillia.User.UserInterface;
using Discord;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AMI.Neitsillia.NeitsilliaCommands
{
    class Party
    {
        public string _id;
        public string partyName;

        public int maxPartySize = 4;

        public List<PartyMember> members;
        public List<NPC> NPCMembers;

        public AreaPath areaKey;

        internal string EncounterKey => $"{partyName}_Encounter";
        internal bool SoloPlayer => members.Count <= 1;
        internal int MemberCount => members.Count + NPCMembers.Count;

        public override string ToString() => partyName;

        [JsonConstructor]
        public Party(bool json) { }
        internal Party(string name, Player player)
        {
            _id = name.ToLower();
            partyName = name;

            members = new List<PartyMember>
            { new PartyMember(player.userid, player.name) };
            NPCMembers = new List<NPC>();
            areaKey = player.areaPath;
            player.PartyKey = new AMIData.DataBaseRelation<string, Party>(_id, this);
        }

        internal Embed EmbedInfo()
        {
            EmbedBuilder info = new EmbedBuilder();
            info.WithTitle(partyName);
            for(int i = 0; i < members.Count; i++)
            {
                info.Description += $"{(i == 0 ? "👑" : "🔘")} {members[i].characterName} {Environment.NewLine}";   
            }
            if (NPCMembers.Count > 0)
            {
                info.Description += "--Followers--" + Environment.NewLine;
                for (int i = 0; i < NPCMembers.Count; i++)
                {
                    info.Description += EUI.GetNum(i + 1) + NPCMembers[i].ToString() + Environment.NewLine;
                }
            }
            return info.Build();
        }

        internal ulong GetLeaderID() => members[0].id;

        internal Player GetLeader() => members[0].LoadPlayer();

        internal async Task UpdateUI(Player current, IMessageChannel chan, MsgType type, string data, bool edit, string content, Embed embed)
        {
            IUserMessage msg = await current.EnUI(edit, content, embed, chan, type, data);
            foreach (PartyMember member in members)
            {
                if(current.userid != member.id)
                    member.LoadPlayer().SetUI(msg, type, data);
            }
        }

        internal void UpdateUI(Player current, IUserMessage msg, MsgType type, string data = null)
        {
            foreach (PartyMember member in members)
            {
                ((current.userid != member.id) ?
                    member.LoadPlayer()
                    : current
                ).SetUI(msg, type, data);
            }
        }

        internal async Task ForEachPlayer(Player current, Func<Player, Task> func)
        {
            foreach(PartyMember member in members)
            {
                Player player = current.userid == member.id ? current : member.LoadPlayer();
                await func(player);
            }
        }

        internal void ForEachPlayerSync(Player current, Action<Player> func)
        {
            foreach (PartyMember member in members)
            {
                Player player = current.userid == member.id ? current : member.LoadPlayer();
                func(player);
            }
        }

        internal async Task Add(Player player)
        {
            if (Search(player.userid) > -1)
                throw Module.NeitsilliaError.ReplyError("Player already has a character in this party");
            members.Add(new PartyMember(player.userid, player.name));
            player.PartyKey = new AMIData.DataBaseRelation<string, Party>(
                partyName, this);
            await SaveData();
        }
        internal async Task Remove(Player player)
        {
            int i = Search(player.userid);
            if (i > -1)
               members.RemoveAt(i);
            if (members.Count > 0)
            {
                RemoveAllPets(player);
                await SaveData();
            }
            else
            {
                if (NPCMembers.Count > 0)  DisbandNPCs(player.Area);
                await AMYPrototype.Program.data.database.DeleteRecord<Party>("Party", partyName, "partyName");
                await AMYPrototype.Program.data.database.DeleteRecord<Party>("Encounter", EncounterKey, "_id");
            }
            if(player.GamblingHand != null)
                await player.GamblingHandKey?.Delete();
            player.PartyKey = null;
        }

        internal Area Remove(int i, Area area)
        {
            if (i < 0 || i > NPCMembers.Count) return area;

            NPC npc = NPCMembers[i];
            if (npc.IsPet())
            {
                string[] data = npc.origin.Split('\\');
                if (data.Length > 1)
                {
                    string id = $"{data[0]}\\{data[1]}";
                    PetList pl = PetList.Load(id);
                    if (pl != null && pl._id == id)
                        _ = pl.UpdatePet(npc);
                    else Orphanage.AddPet(npc);
                }
                else Orphanage.AddPet(npc);
            }
            else
            {
                if (area != null) PopulationHandler.Add(area, npc);
                else npc.Respawn();
            }
            NPCMembers.RemoveAt(i);
            return area;
        }

        internal void RemoveAllPets(Player player)
        {
            PetList pl = null;
            for (int i = 0; i < NPCMembers.Count;)
            {
                NPC npc = NPCMembers[i];
                if (npc.profession == ReferenceData.Profession.Creature)
                {
                    string[] data = npc.origin.Split('\\');
                    if (data.Length == 3 && ulong.TryParse(data[0], out ulong id) && id == player.userid)
                    {
                        pl = pl ?? PetList.Load($"{data[0]}\\{data[1]}");
                        if (pl != null)
                        {
                            _ = pl.UpdatePet(npc);
                        }
                        else Orphanage.AddPet(npc);

                        NPCMembers.RemoveAt(i);
                    }
                    else i++;
                }
                else i++;
            }
            pl?.Save();
        }

        internal bool UpdateFollower(NPC npc)
        {
            for(int i = 0; i < NPCMembers.Count; i++)
                if(NPCMembers[i].displayName.Equals(npc.displayName))
                {
                    NPCMembers[i] = npc;
                    SaveData().Wait();
                    return true;
                }
            return false;
        }

        internal void DisbandNPCs(Area area)
        {
            for(int i=0; i<NPCMembers.Count;)
                area = Remove(i, area);
        }

        public int Search(ulong argId)
        {
            return members.FindIndex(m => m.id == argId);
        }

        public void QuestProgress(Player current, Items.Quests.Quest.QuestTrigger trigger, string argument)
        {
            ForEachPlayerSync(current, (p) =>
            {
                p.Quest_Trigger(trigger, argument);
            });
        }

        internal void EggPocketTrigger(Player player, Egg.EggChallenge challenge)
        {
            ForEachPlayerSync(player, (p) =>
            {
                p.EggPocket_Trigger(challenge);
            });
        }
        internal async Task SyncArea(AreaPath areaKey)
        {
            this.areaKey = areaKey;
            await SaveData();
        }
        internal async Task SaveData()
        {
            await AMYPrototype.Program.data.database.UpdateRecordAsync(
                "Party", "_id", _id, this);
        }

       
    }
}
