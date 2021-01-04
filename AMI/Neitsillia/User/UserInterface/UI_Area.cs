using AMI.Module;
using AMI.Neitsillia.Items.Quests;
using AMI.Neitsillia.NeitsilliaCommands;
using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AMI.Neitsillia.User.UserInterface
{
    partial class UI
    {
        public async Task PetShop(SocketReaction reaction, IUserMessage msg)
        {
            switch (reaction.Emote.ToString())
            {
                case EUI.mainQuest:
                    {
                        await Quest.AvailableQuestList(player, reaction.Channel, data);

                    } break;
            }
        }

        public async Task Tavern(SocketReaction reaction, IUserMessage msg)
        {
            if (EUI.Dice(reaction.Emote.ToString()) == 1)
            {
                await GamblingCommands.TavernGames(player, reaction.Channel);
                return;
            }
            switch (reaction.Emote.ToString())
            {
                case EUI.sideQuest:
                    {
                        var qt = User.DailyQuestBoard.Load(player._id);
                        await qt.ShowBoard(player, reaction.Channel);
                    }
                    break;
                case EUI.bounties:
                    {
                        Areas.AreaPartials.Area tavern = player.Area;
                        if (tavern.parent != null)
                            tavern = Areas.AreaPartials.Area.LoadArea(tavern.GeneratePath(false) + tavern.parent);
                        await Commands.Areas.GenerateBountyFile(player, tavern, -1, reaction.Channel);
                    }
                    break;
            }
        }

        public async Task Adventure(SocketReaction reaction, IUserMessage msg)
        {
            string e = reaction.Emote.ToString();
            int i = EUI.GetNum(e);
            if(i > -1)
            {
                if (data[0] == 'D') //Select difficulty
                {
                    await Adventures.Adventure.StartAdventure(player, Channel, 
                        Adventures.Adventure.AdventureType.FreeRoam,
                        ((Adventures.Adventure.Intensity[])Enum.GetValues(typeof(Adventures.Adventure.Intensity)))[i - 1], 
                        null);
                }
                else //Select a quest
                {

                }
                return;
            }
            switch(e)
            {
                case EUI.ok:
                    await Adventures.Adventure.SelectIntensity(player, reaction.Channel);
                    break;
                case EUI.cancel:
                    if (player.IsInAdventure) await player.Adventure.End(player, reaction.Channel);
                    else await GameCommands.StatsDisplay(player, reaction.Channel);
                    break;
            }
        }



    }
}
