using AMI.Module;
using AMI.Neitsillia.Encounters;
using AMI.Neitsillia.Items.Quests;
using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AMI.Neitsillia.User.UserInterface
{
    partial class UI
    {
        static void InitO_Encounter()
        {
            OptionsLoad.Add(MsgType.Adventure, ui => { 
                if (ui.player.IsInAdventure)
                    ui.options = new List<string>(new[] { EUI.cycle, EUI.cancel });
                else if (ui.data == null) ui.options = new List<string>(new[] { EUI.ok, EUI.sideQuest });
                else if (ui.data[0] == 'Q')
                {
                    ui.options = new List<string>();
                    for (int i = 0; i < 6; i++) ui.options.Add(EUI.GetNum(i + 1));
                }
                else
                {
                    ui.options = new List<string>();
                    for (int i = 0; i < 4; i++) ui.options.Add(EUI.GetNum(i + 1));
                }
                 
            });
        }

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

        public async Task Adventure(SocketReaction reaction, IUserMessage msg)
        {
            string e = reaction.Emote.ToString();
            int i = EUI.GetNum(e);
            if (i > -1)
            {
                if (data[0] == 'Q') //Select a quest
                {
                    await Adventures.Adventure.SelectIntensity(player, reaction.Channel, i);
                }
                else //Select difficulty
                {
                    int.TryParse(data, out int quest);
                    await Adventures.Adventure.StartAdventure(player, Channel,
                        ((Adventures.Adventure.Intensity[])Enum.GetValues(typeof(Adventures.Adventure.Intensity)))[i - 1],
                        quest > 0 ? Adventures.Adventure.currentQuests[quest - 1] : null);
                }
                return;
            }
            switch (e)
            {
                case EUI.ok:
                    await Adventures.Adventure.SelectIntensity(player, reaction.Channel, 0);
                    break;
                case EUI.sideQuest:
                    await Adventures.Adventure.SelectQuest(player, reaction.Channel);
                    break;
                case EUI.cycle:
                    await player.Adventure.Display(player, reaction.Channel, true);
                    break;
                case EUI.cancel:
                    if (player.IsInAdventure) await player.Adventure.End(player, reaction.Channel);
                    else await GameCommands.StatsDisplay(player, reaction.Channel);
                    break;
            }
        }
    }
}
