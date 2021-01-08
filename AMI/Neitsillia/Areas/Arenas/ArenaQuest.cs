using AMI.Methods;
using AMI.Neitsillia.User.UserInterface;
using System;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace AMI.Neitsillia.Areas.Arenas
{
    class ArenaQuest
    {
        static AMIData.MongoDatabase Database => AMYPrototype.Program.data.database;
        static readonly string[] blacklistedRaces = { "Human", "Christmas", "Thanksgiving" };

        public string name;
        public int difficulty;
        public string[] enemies;
        public string description;
        public string enemiesDesc;

        public ArenaQuest(int diff, int level)
        {
            difficulty = diff;

            LoadFight(Level(level));

            name += $" {new StringBuilder().Insert(0, EUI.bounties, difficulty)}";
            var vs = enemies.GroupBy(v => v);
            enemiesDesc = vs.Join(", ", v => $"{v.Count()}x {v.Key}");
        }

        internal int Cost(int level) => (difficulty * 7) * (level + 1) * 32;

        internal int Level(int level) => level + (difficulty * 3);

        internal string ShortDescription(int? i = null)
            => $"{(i.HasValue ? EUI.GetNum(i.Value) : "")} **{name}** {Environment.NewLine} {enemiesDesc}";

        internal string LongDescription(int costMult)
            => ShortDescription() + Environment.NewLine + description + Environment.NewLine
            + $"Entry Cost: {Cost(costMult)} kuts";

        private void LoadFight(int level)
        {
            (name, enemies) = RandomFight(level);
        }

        private (string name, string[] mobs) RandomFight(int level)
        {
            string choices = Database.Query("Creature", $"{{ $and: [ " +
                $"{{ baseLevel: {{ $lte: {level} }} }}," +
                $"{{ race: {{ $nin: [ \"{string.Join("\", \"", blacklistedRaces)}\" ] }} }}" +
                $" ] }}", "_id");
            choices = choices.Replace("{ \"_id\" :", "").Replace("}", "");
            string[] options = JsonSerializer.Deserialize<string[]>(choices);

            string[] mobs = new string[5];
            for (int i = 0; i < mobs.Length; i++)
                mobs[i] = Utils.RandomElement(options);

            return ("Bad Batch", mobs);
        }

    }
}
