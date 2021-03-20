using AMI.Methods;
using AMI.Neitsillia.User.UserInterface;
using AMYPrototype;
using AMYPrototype.Commands;
using Discord;
using AMI.Neitsillia.Items.ItemPartials;
using System;

namespace AMI.Neitsillia.Encounters
{
    public class Puzzle
    {
        public enum Reward { Random, Loot, Combat, /*Dungeon*/ }
        static Random Rng => Program.rng;

        internal string partyName;
        public string name;
        public string description;

        public int answer;
        public int[][] controls;
        public int position;

        public Reward rewardType;
        public string reward = "~Random";
        public int level;

        public string[] emotes;

        public static Puzzle Random() => Utils.RandomElement<Func<Puzzle>>(Load_Disk).Invoke();
        public static Puzzle Load(string name) => (Puzzle)Utils.GetFunction(typeof(Puzzle), "Load_" + name, true)?.Invoke(null, null) ?? Random();

        #region Initializations

        public static Puzzle Load_Disk()
        {
            Puzzle puzzle = new Puzzle()
            {
                name = "Disk",
                description = "You find yourself in a small room in which's center is a disc embedded in stone. Beside it, two switches seem to indicate directions.",
                controls = new int[][] { new int[5], new int[5] },
                emotes = new string[] { EUI.prev, EUI.next }
            };

            puzzle.answer = Rng.Next(1, 8);

            Func<int, int> cycle = (int i) =>
            {
                int k = Rng.Next(0, 8);

                if (i == 0 && k == puzzle.answer) k += Program.Chance(50) ? -1 : 1;

                return k;
            };

            for(int i = 0; i < 5; i++)
            {
                puzzle.controls[0][i] = cycle(i);
                puzzle.controls[1][i] = cycle(i);
            }

            return puzzle;
        }

        #endregion

        internal bool Solve_Puzzle(string emote, int turn, out EmbedBuilder embed)
        {
            bool solved = false;
            try
            {
                (embed, solved) = Utils.RunMethod<(EmbedBuilder, bool)>(name, this, emote, turn);

                if (solved)
                {
                    embed.Description = "That's it, the puzzle unlocked itself!";
                    embed.Color = Color.Green;
                }
            }
            catch(Exception e)
            {
                _ = Handlers.UniqueChannels.Instance.SendToLog(e, "Puzzle Error: " + name);
                embed = DUtils.BuildEmbed($"{name} | {partyName}", "It seems the puzzle broke down and unlocked. Lucky break.", null, Color.DarkBlue);
                solved = true;
            }
            return solved;
        }

        internal Encounter Solved_Puzzle(Encounter encounter)
        {
            if (rewardType == Reward.Random) rewardType = (Reward)Rng.Next(1, Enum.GetValues(typeof(Reward)).Length);
            encounter.puzzle = null;
            switch (rewardType)
            {
                case Reward.Loot:
                    {
                        encounter.Load(Encounter.Names.Loot);
                        if (reward == "~Random")
                        {
                            if (Program.Chance(1))
                                encounter.AddLoot(Item.CreateRune(1));
                            else encounter.AddLoot(Item.RandomItem(level));
                        }
                        else
                        {
                            var item = Item.LoadItem(reward);
                            item.Scale(level);
                            encounter.AddLoot(item);
                        }

                    }
                    break;
                case Reward.Combat:
                    {
                        if (reward == "~Random") reward = Utils.RandomElement("Odez' Spawn", "Tainted Specter");
                        encounter.turn = 0;
                        encounter.Load(Encounter.Names.Mob);
                        var mob = NPCSystems.NPC.GenerateNPC(level, reward);
                        mob.Evolve(5);
                        encounter.mobs = new NPCSystems.NPC[] { mob };
                    }
                    break;
            }

            return null;
        }

        #region Solving

        public (EmbedBuilder, bool) Disk(string emote, int turn)
        {
            EmbedBuilder embed = DUtils.BuildEmbed(name, " ", null, Color.Red);

            if (emote != null)
            {
                int turning = 0;
                switch (emote)
                {
                    case EUI.prev: turning = -controls[0][turn % 5]; break;
                    case EUI.next: turning = controls[0][turn % 5]; break;

                    default: turning = 0; break;
                }

                if (turning == 0)
                    embed.WithDescription("Nothing happened, the disc did not move");
                else
                {
                    
                    embed.WithDescription($"The arrow turned **{Math.Abs(turning)}** click{(Math.Abs(turning) > 1 ? "s" : null)} **" 
                        + (turning > 0 ? "clockwise" : "counter clockwise") + "**");

                    position += turning;

                    if (position > 7) position %= 8;
                    while (position < 0) position += 8;
                }
            }
            else embed.WithDescription(description);

            (string name, string arrow)[] direction = 
            {
                ("North", "⬆️"),
                ("North East","↗️"),
                ("East","➡️"),
                ("South East","↘️"),
                ("South","⬇️"),
                ("South West","↙️"),
                ("West","⬅️"),
                ("North West", "↖️")
            };

            embed.Description += 
                Environment.NewLine + "The arrow is now pointing " + direction[position].name +
                Environment.NewLine + "The circle is " + direction[answer].name;

            Func<int, string> getColor = (int i) => i == answer ? "🟩" : "🟨";

            embed.AddField("Disk",
                $"⬛⬛⬛{getColor(0)}⬛⬛⬛" 
                + Environment.NewLine + $"⬛{getColor(7)}⬛⬛⬛{getColor(1)}⬛" 
                + Environment.NewLine + $"⬛⬛⬛⬛⬛⬛⬛" 
                + Environment.NewLine + $"{getColor(6)}⬛⬛{direction[position].arrow}⬛⬛{getColor(2)}"
                + Environment.NewLine + $"⬛⬛⬛⬛⬛⬛⬛"
                + Environment.NewLine + $"⬛{getColor(5)}⬛⬛⬛{getColor(3)}⬛"
                + Environment.NewLine + $"⬛⬛⬛{getColor(4)}⬛⬛⬛"
                );

            return (embed, position == answer);
        }

        #endregion

    }
}
