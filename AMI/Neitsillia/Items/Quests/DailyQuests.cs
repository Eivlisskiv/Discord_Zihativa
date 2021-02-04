using AMI.Neitsillia.Items.Quests;
using AMI.Neitsillia.User.PlayerPartials;
using AMI.Neitsillia.User.UserInterface;
using AMYPrototype.Commands;
using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AMI.Neitsillia.User
{
    class DailyQuestBoard
    {
        static AMIData.MongoDatabase Database => AMYPrototype.Program.data.database;
        internal enum Cycle { Daily = 1, Weekly = 7 };

        internal static DailyQuestBoard Load(string id)
        {
            return Database.LoadRecord<DailyQuestBoard, string>(null, id) ?? new DailyQuestBoard(id);
        }

        public string _id;

        public DailyQuest[] dailies;
        public DailyQuest[] weeklies;

        public DailyQuestBoard(string id)
        {
            _id = id;
            dailies = new DailyQuest[]
            {
                new DailyQuest(0),
                new DailyQuest(10),
                new DailyQuest(25),
            };

            weeklies = new DailyQuest[]
            {
                new DailyQuest(5),
                new DailyQuest(20),
                new DailyQuest(50),
                new DailyQuest(75),
                new DailyQuest(120),
            };

            Save();
        }

        internal void Save()
        {
            Database.UpdateRecord(null, null, this);
        }

        internal Quest AcceptQuest(int i, Cycle c)
        {
            DailyQuest q = (c == Cycle.Daily ? dailies : weeklies)[i];
            Quest quest = q.quest;
            q.quest = null;
            q.next = DateTime.UtcNow.AddDays((int)c);
            Save();
            return quest;
        }

        internal (bool, string) LoadQuest(Player player, int i, Cycle time)
        {
            DailyQuest quest = (time == Cycle.Daily ? dailies : weeklies)[i];

            if (quest.level > player.level) return (false, $"[Unlock at level {quest.level}]");

            if (quest.next > DateTime.UtcNow) return (false, $"[{Timers.CoolDownToString(quest.next)}]");

            if(quest.quest == null)
            {
                quest.quest = Quest.Random(time == Cycle.Daily ? 1 : 2);
                Save();
            }

            return (player.quests.Count < Quest.MaxQuests, $" **__{quest.quest.title}__** {Environment.NewLine} {quest.quest.description}");
        }

        internal (List<int>, EmbedFieldBuilder) LoadCategory(Player player, Cycle cycle)
        {
            var list = cycle == Cycle.Daily ? dailies : weeklies;

            string board = null;
            List<int> options = new List<int>();
            for(int i=0; i< list.Length; i++)
            {
                var (available, info) = LoadQuest(player, i, cycle);
                int k = i + (cycle == Cycle.Daily ? 1 : 4);
                board += (available ? EUI.GetNum(k) : "> ") + info + Environment.NewLine;
                if (available) options.Add(k);
            }

            return (options, DUtils.NewField($"{cycle} Quests", board));
        }

        internal async Task ShowBoard(Player player, ISocketMessageChannel chan)
        {
            EmbedBuilder embed = DUtils.BuildEmbed($"{player.name}'s Quest Board", "Use the reactions to accept Quests.", null, player.userSettings.Color);
            List<int> options = new List<int>();

            var d = LoadCategory(player, Cycle.Daily);
            var w = LoadCategory(player, Cycle.Weekly);

            options.AddRange(d.Item1);
            options.AddRange(w.Item1);

            embed.WithFields(d.Item2, w.Item2);
            if(options.Count == 0)
            {
                embed.Description = player.quests.Count >= Quest.MaxQuests ?
                    $"**You may not currently accept more quests [Max {Quest.MaxQuests}], " +
                    $"complete your current ones before accepting more.**" :
                    "**No quest available**";
            }
            await player.EditUI(null, embed.Build(), chan,
                MsgType.DailyQuestBoard, string.Join(";", options));
        }
    }
}
