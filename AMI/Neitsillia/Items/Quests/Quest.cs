using AMI.Methods;
using AMI.Neitsillia.Collections;
using AMI.Neitsillia.User.PlayerPartials;
using AMI.Neitsillia.User.UserInterface;
using AMYPrototype;
using AMYPrototype.Commands;
using Discord;
using Discord.WebSocket;
using Neitsillia.Items.Item;
using System;
using System.Collections.Generic;

namespace AMI.Neitsillia.Items.Quests
{
    class Quest
    {
        //Static Vars
        public enum QuestTrigger
        {
            None, Incomplete,

            CollectingDaily,
            Kill,

            Enter,
            EnterFloor,

            QuestLine,

            Scrapping,
            Crafting,
            GearUpgrading,

            RecruitNPC,

            Consuming,
            ClearDungeon,

            UpgradeEggPocket, FillEggPocket, HatchEgg, //with pocket or egg tier as objective/argument

            Deliver,
            Puzzle,
        };

        public const int PerPage = 6;
        public const int MaxQuests = 18;

        //Class Vars
        public int[] id = new int[3];

        public string title;
        public string description;
        public string longDesciption;

        public int objectiveCompleted;
        public int totalObjective;

        public QuestTrigger trigger;

        //Rewards
        public int koinsReward;
        public long xpReward;
        public StackedObject<string, int> itemReward;

        public int[][] nextId;

        public string objective;

        //Static Methods

        public static Quest Random(int intensity)
        {
            Random r = Program.rng;
            int[] id = new int[3];
            id[0] = 3;
            id[1] = r.Next(QuestLoad.QuestList[3].Length);
            id[2] = r.Next(QuestLoad.QuestList[3][id[1]].Length);
            Quest q = Load(id, 0, null, r.Next(1, 3)*intensity);
            q.itemReward = intensity <= 1 ?
                new StackedObject<string, int>("-RepairKit;1", 5):
                new StackedObject<string, int>($"-Rune;{intensity - 1}", 1);
            return q;
        }
        public static Quest Load(int id1, int id2, int id3) => Load(new int[] { id1, id2, id3 });
        public static Quest Load((int id1, int id2, int id3) id, int objCompleted = 0, string argument = null, int multiplier = 1) 
            => Load(new int[] { id.id1, id.id2, id.id3 }, objCompleted, argument, multiplier);
        public static Quest Load(int[] id, int objCompleted = 0, string argument = null, int multiplier = 1)
        {
            Quest quest = QuestLoad.QuestList[id[0]][id[1]][id[2]](multiplier, argument);
            quest.id = id;
            quest.objectiveCompleted = objCompleted;
            return quest;
        }

        internal static async System.Threading.Tasks.Task QuestInfo(
            Player player, int page, int qIndex, ISocketMessageChannel channel)
        {
            Quest q = player.quests[qIndex];
            await player.NewUI(await channel.SendMessageAsync(embed:
                player.UserEmbedColor(q.AsEmbed()).Build()),
                MsgType.QuestInfo, qIndex.ToString());
        }

        internal static async System.Threading.Tasks.Task QuestList(
            Player player, int page, ISocketMessageChannel channel)
        {
            EmbedBuilder e = DUtils.BuildEmbed($"{player.name}'s Quests", 
                "__Click on the emotes to view details of a quest__", null, player.userSettings.Color());

            if (player.quests.Count < 1)
                e.Description = "No Quests";
            else
            {
                int m = (page + 1) * PerPage;
                int n = 0;
                for (int i = page * PerPage;
                    i < player.quests.Count && 
                    i < m;
                    i++, n++)
                {
                    Quest q = player.quests[i];
                    if (q.trigger == QuestTrigger.Incomplete)
                    {
                        Quest update = Load(q.id, q.objectiveCompleted);
                        if (update.trigger != QuestTrigger.Incomplete)
                            player.quests[i].Set(update);
                    }
                    e.AddField(q.AsEmbedField(true, EUI.GetNum(n)));
                }
            }
            await player.NewUI(await channel.SendMessageAsync(embed: e.Build())
                , MsgType.QuestList, page.ToString());
        }

        internal static async System.Threading.Tasks.Task AvailableQuestList(
            Player player, ISocketMessageChannel chan, params int[][] ids)
        {
            string data = null;
            EmbedBuilder embed = new EmbedBuilder();
            embed.WithTitle("Available Quests");
            int i = 0;
            foreach(int[] id in ids)
            {
                try
                {
                    Quest q = Load(id);
                    embed.Description += $"{EUI.GetNum(i)} {q.title} : {q.description}" + Environment.NewLine;
                }catch(Exception)
                {
                    Console.WriteLine($"Error loading quest: {string.Join(",", id)}");
                }

                data += string.Join(",", id) + ";";
                i++;
            }

            await player.NewUI(await chan.SendMessageAsync(embed: embed.Build()), MsgType.AcceptQuests, data);
        }

        internal static async System.Threading.Tasks.Task AvailableQuestList(
            Player player, ISocketMessageChannel chan, string data)
        {
            string[] ids = data.Split(';');
            EmbedBuilder embed = new EmbedBuilder();
            embed.WithTitle("Available Quests");
            int i = 0;
            foreach (string s in ids)
            {
                try
                {
                    string[] sid = s.Split(',');
                    int[] id =
                    {
                        int.Parse(sid[0]),
                        int.Parse(sid[1]),
                        int.Parse(sid[2]),
                    };
                    Quest q = Load(id);
                    embed.Description += $"{EUI.GetNum(i)} {q.title} : {q.description}" + Environment.NewLine;
                }
                catch (Exception)
                {
                    Console.WriteLine($"Error loading quest: {s}");
                }
                i++;
            }

            await player.NewUI(await chan.SendMessageAsync(embed: embed.Build()), MsgType.AcceptQuests, data);
        }

        static async System.Threading.Tasks.Task<int> CheckDelivery(Player player, int i, ISocketMessageChannel chan)
        {
            Quest q = player.quests[i];
            string[] items = q.objective.Split(';')[1].Split(',');
            
            string missingItems = null;
            List<StackedObject<string, int>> delivery = new List<StackedObject<string, int>>();
            for(int j = 0; j < items.Length; j++)
            {
                string[] d = items[j].Split('*');
                var so = new StackedObject<string, int>(d[0], int.Parse(d[1]));
                if (!player.inventory.Contains(so))
                    missingItems += so.ToString() + Environment.NewLine;
                else delivery.Add(so);
            }
            if (missingItems != null)
            {
                await chan.SendMessageAsync($"Missing items for {q.title} delivery:" + Environment.NewLine + missingItems);
                i++;
            }
            else
            {
                foreach (var so in delivery)
                    player.inventory.Remove(so);
                q.objectiveCompleted++;
                if (q.objectiveCompleted >= q.totalObjective)
                    i = q.Complete(player, i);

                await chan.SendMessageAsync($"Items for {q.title} delivered!");
            }
            return i;
        }

        internal static async System.Threading.Tasks.Task CheckDeliveries(Player player, ISocketMessageChannel chan)
        {
            int deliveries = 0;
            for(int i = 0; i < player.quests.Count;)
            {
                Quest q = player.quests[i];
                if (q.trigger != QuestTrigger.Deliver || 
                    !(player.Area.type.ToString() == q.objective.Split(';')[0]))
                    i++;
                else
                {
                    deliveries++;
                    i = await CheckDelivery(player, i, chan);
                }
            }
            if (deliveries == 0)
                await chan.SendMessageAsync("You have no delivery quest here.");
            else
                player.SaveFileMongo();
        }

        //Class Methods

        public Quest(string title, int tobj)
        {
            totalObjective = tobj;
            this.title = title;
        }

        internal bool Equals(int v1, int v2, int v3)
        {
            return id[0] == v1 && id[1] == v2 && id[2] == v3;
        }

        internal void Set(Quest quest)
        {
            title = quest.title;
            description = quest.description;
            totalObjective = quest.totalObjective;
            trigger = quest.trigger;

            koinsReward = quest.koinsReward;
            xpReward = quest.xpReward;
            itemReward = quest.itemReward;

            nextId = quest.nextId;
            objective = quest.objective;
        }

        internal int Complete(Player player, int i)
        {
            player.quests.Remove(this);
            

            EmbedBuilder em = new EmbedBuilder();
            em.WithTitle("Quest Completed!");
            em.WithDescription($"You've completed {title} {Environment.NewLine} {description}");

            string rewards = null;

            if (xpReward > 0)
            {
                player.XPGain(xpReward);
                rewards += Utils.Display(xpReward) + " XP" + Environment.NewLine;
            }
            if (koinsReward > 0)
            {
                player.KCoins += koinsReward;
                rewards += Utils.Display(koinsReward) + " Kuts" + Environment.NewLine;
            }

            if (itemReward != null)
            {
                Item item = null;
                string[] data = itemReward.item.Split(';');
                int tier = 0;
                if (data.Length > 1) int.TryParse(data[1], out tier);
                switch(data[0])
                {
                    case "-Repair Kit": item = Item.CreateRepairKit(tier); break;
                    case "-Rune": item = Item.CreateRepairKit(tier); break;
                    case "-Random": item = Item.RandomItem(tier); break;
                    default: item = Item.LoadItem(itemReward.item); break;
                }
                player.inventory.Add(item, itemReward.count, -1);
                rewards += $"{itemReward.count}x {item.name}" + Environment.NewLine;
            }

            if (nextId != null)
            {
                foreach (int[] nIds in nextId)
                {
                    if (nIds != null)
                    {
                        Quest next = Load(nIds, 0);
                        player.quests.Insert(0, next);
                        rewards += "New Quest: " + next.title + Environment.NewLine;
                        i++;
                    }
                }
            }
            else if (id[0] == 0)
                player.ProgressData.CompletedNewQuests(id);

            string extraRewards = null;
            switch(string.Join(",", id))
            {
                case "0,3,0":
                        player.EggPocket = new NPCSystems.Companions.EggPocket(player._id);
                    extraRewards += "Empty Tier 0 Egg Pocket"; break;
                case "0,2,4":
                    player.specter = new Abilities.Specter();
                    StackedItems[] vials = {
                        new StackedItems(Item.CreateEssenseVial("Heat", 0), 10),
                        new StackedItems(Item.CreateEssenseVial("Toxin", 0), 10),
                        new StackedItems(Item.CreateEssenseVial("Static", 0), 10),
                    };
                    player.inventory.Add(-1, vials);
                    extraRewards += $"Binded {player.specter}"
                        + Environment.NewLine + string.Join<StackedItems>(Environment.NewLine, vials);
                   break;
            }

            em.AddField("Rewards:", rewards + extraRewards ?? "No Rewards");

            player.SendMessageToDM("Use the ``~quest`` command to view quests.", em).Wait();

            return i;
        }

        internal int Triggered(int i, Player player, string argument)
        {
            if (TriggerCheck(argument))
                objectiveCompleted++;

            if (objectiveCompleted >= totalObjective)
                i = Complete(player, i);
            else
                i++;

            return i;
        }

        private bool TriggerCheck(string argument)
        {
            //objective is null = any argument completes
            if (objective == null)
                return true;
            //objective is not null but argument is
            if (argument == null)
                return false;

            switch(trigger)
            {
                case QuestTrigger.Kill:
                    {
                        //name;race;level; 
                        return CheckMultipleValues(argument, 2);
                    }
                //name;amount;type
                case QuestTrigger.Consuming:
                //name;amount;gotschem
                case QuestTrigger.Scrapping:
                //areaID;floorNum
                case QuestTrigger.EnterFloor:
                        return CheckMultipleValues(argument, 1);
                //destination type;item name*amount, item name*amount, item name*amount...
                case QuestTrigger.Deliver:
                    return true; //Check must be done before;

                default: return objective.Equals(argument, StringComparison.OrdinalIgnoreCase);
            };
        }

        bool CheckMultipleValues(string arguments, int parseIntIndex)
        {
            string[] objArgs = objective.Split(';');
            string[] args = arguments.Split(';');

            for(int i = 0; i < objArgs.Length && i < args.Length; i++)
            {
                if(i == parseIntIndex)
                {
                    if (!int.TryParse(objArgs[1], out int parsed))
                        parsed = -1;
                    if (parsed > int.Parse(args[i]))
                        return false;
                }
                else
                {
                    if(objArgs[i] != "null" && objArgs[i].Length > 0)
                    {
                        if (!objArgs[i].Equals(args[i], StringComparison.OrdinalIgnoreCase))
                            return false;
                    }
                }
            }

            return true;
        }

        //Gets
        internal string GetPercentComplited()
        {
            return $"{objectiveCompleted}/{totalObjective}";
        }

        internal string GetLineComplection() => id[0] > 2 ? null : $"{GetPercent()}%";

        internal double GetPercent()
            => Math.Round(id[2] * 100.00 / QuestLoad.QuestList[id[0]][id[1]].Length, 2);

        internal string GetProgression()
        {
            string line = GetLineComplection();

            return "Objective Progress: " + GetPercentComplited()
                + (line != null ?
                Environment.NewLine + $"Quest Line Progress: " + line 
                : null);
        }

        internal EmbedFieldBuilder AsEmbedField(bool inline = true, string add = null)
        {
            return new EmbedFieldBuilder()
            {
                Name = add + $" **__{title}__**",
                Value = 
                description + Environment.NewLine + GetProgression(),
                IsInline = inline,
            };
        }

        internal EmbedBuilder AsEmbed()
        {
            return AMYPrototype.Commands.DUtils.BuildEmbed(
            $"**__{title}__**", description + Environment.NewLine
            + GetProgression() + Environment.NewLine + Environment.NewLine
            + longDesciption);
        }
    }
}