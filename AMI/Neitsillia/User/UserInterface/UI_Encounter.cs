using AMI.Neitsillia.Encounters;
using AMI.Neitsillia.Items.Quests;
using Discord;
using Discord.WebSocket;
using System.Threading.Tasks;

namespace AMI.Neitsillia.User.UserInterface
{
    partial class UI
    {
        public async Task Puzzle(SocketReaction reaction, IUserMessage msg)
        {
            Encounter enc = player.Encounter;
            if (enc?.Name == Encounter.Names.Puzzle)
            {
                Puzzle puz = enc.puzzle;
                puz.partyName = player.Party?.partyName ?? player.name;
                bool solved = puz.Solve_Puzzle(reaction.Emote.ToString(), enc.turn, out EmbedBuilder embed);
                await EditMessage(null, embed.Build(), removeReactions: solved);

                if (solved)
                {
                    player.QuestTrigger(Quest.QuestTrigger.Puzzle, $"{puz.name};{(int)puz.rewardType};{puz.reward}");
                    puz.Solved_Puzzle(enc);
                    await reaction.Channel.SendMessageAsync(embed: enc.GetEmbed().Build());
                }
                else enc.turn++;

                enc.Save();
            }
        }
    }
}
