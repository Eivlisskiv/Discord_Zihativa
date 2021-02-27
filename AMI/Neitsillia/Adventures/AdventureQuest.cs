using AMI.Methods;
using AMI.Neitsillia.Collections;
using AMYPrototype;
using Discord;
using Neitsillia.Items.Item;
using System;
using System.Text;

namespace AMI.Neitsillia.Adventures
{
    [MongoDB.Bson.Serialization.Attributes.BsonIgnoreExtraElements]
    public class AdventureQuest
    {
        static readonly (string, int)[] materialQuest =
        {
            ("Leather", 5), ("Wood", 10),
        };

        public static AdventureQuest Generate(int difficulty)
        => (Program.Chance(difficulty * 5)) ?
            new AdventureQuest(difficulty, Program.rng.Next(6, 12)) :
            (Program.Chance(difficulty * 10)) ? new AdventureQuest(difficulty, -1)
            : new AdventureQuest(difficulty, Utils.RandomElement(materialQuest));

        public string title;
        public double hoursTime;
        public int difficulty;
        //Random Gear's Type
        public int typeLoot;
        //Specific Material
        public StackedItems materialsLoot;

        public AdventureQuest(int diff, int loot)
        {
            difficulty = diff;
            typeLoot = loot;

            Load();
        }

        public AdventureQuest(int diff, (string name, int count) material)
        {
            difficulty = diff;
            materialsLoot = new StackedItems(material.name, material.count);
            typeLoot = -1;

            Load();
        }

        public string Action => $" Questing {title}";

        private void Load()
        {
            hoursTime = (0.5 * difficulty) + (Program.rng.Next(1, 3) + 
                (Program.rng.Next(0, 60)/60.00));
            title = new StringBuilder().Insert(0, User.UserInterface.EUI.bounties, difficulty).ToString();
        }

        public string TimeLeft(DateTime start)
        {
            DateTime end = start.AddHours(hoursTime);
            if (DateTime.UtcNow < end)
                return $"Quest time left: {User.Timers.CoolDownToString(end - DateTime.UtcNow)}";

            return "Quest completed.";
        }

        internal string Info(int level)
            => title + Environment.NewLine +
                $"Duration: {Duration()}" + Environment.NewLine +
                "Bonus Loot: " + TargetedLootInfo(level);

        private string Duration()
        {
            double hours = Math.Floor(hoursTime);
            double minutes = Math.Floor((hoursTime - hours) * 60);
            return $"{hours}h {minutes}m";
        }

        public string TargetedLootInfo(int level)
            => typeLoot > -1 ?
                $"Random Level {level + difficulty} {(Item.IType)typeLoot}" :
                (materialsLoot != null ?
                $"{MaterialAmount(level)}x {materialsLoot.item.originalName}"
                : $"Random Level {level + difficulty} Gear");

        internal EmbedField ToField(int index, int level)
            => AMYPrototype.Commands.DUtils.NewField(
                $"{User.UserInterface.EUI.GetNum(index)} {title}",
                $"Duration: {Duration()}" + Environment.NewLine +
                "Bonus Loot: " + TargetedLootInfo(level)).Build();

        public StackedItems Loot(int level)
            => typeLoot > -1 ? new StackedItems(
                Item.RandomItem((level + difficulty) * 5, typeLoot), 1)
                : (materialsLoot != null ? new StackedItems(materialsLoot.item,
                 MaterialAmount(level))
                : new StackedItems(Item.RandomGear((level + difficulty) * 5, true), 1));

        private int MaterialAmount(int level)
            => (1 + Math.Max(1, (level + difficulty) / 10))
                * materialsLoot.count;
    }
}
