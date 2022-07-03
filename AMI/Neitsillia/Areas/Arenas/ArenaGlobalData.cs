using AMI.Methods;
using AMI.Neitsillia.Areas.AreaPartials;
using AMI.Neitsillia.Encounters;
using AMI.Neitsillia.NPCSystems;
using AMI.Neitsillia.User.PlayerPartials;
using AMYPrototype.Commands;
using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AMI.Neitsillia.Areas.Arenas
{
    class ArenaGlobalData
    {
        static AMIData.MongoDatabase Database => AMYPrototype.Program.data.database;

        public static async Task RefreshAllQuests()
        {
            List<ArenaGlobalData> items = await Database.LoadRecordsAsync<ArenaGlobalData>(null);
            foreach(var i in items)
                await i.RefreshQuests();
        }

        public static async Task Delete(string id)
        => await Database.DeleteRecord<ArenaGlobalData>(null, id);
        public static async Task<ArenaGlobalData> Load(string id)
        {
            ArenaGlobalData v = await Database.LoadRecordAsync<ArenaGlobalData, string>(id);
            if(v == null)
            {
                v = new ArenaGlobalData(id);
                await v.RefreshQuests();
            }

            return v;
        }


        public string _id;
        public int level;
        public ArenaQuest[] quests;

        public ArenaGlobalData(string id) => _id = id;

        public async Task RefreshQuests()
        {
            quests = new ArenaQuest[5];
            level = Area.LoadArea(_id).level;
            for (int i = 0; i < 5; i++)
                quests[i] = new ArenaQuest(i < 2 ? 1 : i < 4 ? 2 : 3, level);

            await Save();
        }

        public async Task Save()
            => await Database.UpdateRecordAsync(null, 
                AMIData.MongoDatabase.FilterEqual<ArenaGlobalData, string>("_id", _id), this);
        
        public async Task Delete() => await Delete(_id);

        public async Task DiscordUI(Player player, IMessageChannel chan)
        {
            await player.NewUI(null, DUtils.BuildEmbed("Arena Fights",
                quests.Join(Environment.NewLine, (q, i) => q.ShortDescription(i+1)),
                null, player.userSettings.Color).Build(), chan, 
                User.UserInterface.MsgType.ArenaFights, $"#{quests.Length}");
        }

        public async Task DiscordUI(int fight, Player player, IMessageChannel chan)
        {
            await player.NewUI(null, DUtils.BuildEmbed("Arena Fights",
                quests[fight].LongDescription(level),
                null, player.userSettings.Color).Build(), chan,
                User.UserInterface.MsgType.ArenaFights, $"{fight}");
        }

        internal async Task StartFight(int v, Player player, IMessageChannel channel)
        {
            ArenaQuest fight = quests[v];

            int cost = fight.Cost(level);
            if(player.KCoins < cost)
            {
                await channel.SendMessageAsync("You do not have the required kutyei coins to enter");
                return;
            }

            player.KCoins -= cost;

            Area parent = player.Area;
            Area dungeon = new Area(AreaType.Arena, $"{parent.name} : {fight.name}", parent) {
                floors = 0,
                level = fight.Level(level),
                loot = fight.drops,
                eLootRate = fight.dropChance,
                mobs = new List<string>[] { fight.enemies.ToList() }
            };

            await player.SetArea(dungeon);

            Encounter enc = player.NewEncounter(new Encounter(Encounter.Names.Mob, player), true);
            enc.mobs = new NPC[fight.enemies.Length];
            for (int i = 0; i < enc.mobs.Length; i++)
                enc.mobs[i] = NPC.GenerateNPC(fight.Level(level), fight.enemies[i]);

            EmbedBuilder embed = enc.GetEmbed();
            await player.NewUI(null, embed.Build(), channel, User.UserInterface.MsgType.Combat);
        }
    }
}
