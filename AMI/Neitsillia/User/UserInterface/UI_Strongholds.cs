using AMI.Neitsillia.Areas.House;
using AMI.Neitsillia.Commands.AreaCommands;
using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AMI.Neitsillia.User.UserInterface
{
    public partial class UI
    {
        static void InitO_Strongholds()
        {
            OptionsLoad.Add(MsgType.House, ui =>
                ui.options = (ui.data == "upgrade" ? 
                new List<string>() { EUI.ok, EUI.cancel } : 
                new List<string>() { EUI.storage }
                )
            );

            OptionsLoad.Add(MsgType.HouseStorage, ui =>
            {
                ui.options = new List<string>()
                   { EUI.prev, EUI.cycle, EUI.next };
            });
        }

        public async Task House(SocketReaction reaction, IUserMessage _)
        {
            House house = await Areas.House.House.Load(player.userid);

            switch (reaction.Emote.ToString())
            {
                case EUI.ok:
                    if(house == null || !house.junctions.Contains(player.AreaInfo.path)) //new house
                    {
                        if (player.KCoins < Areas.House.House.HousePrice(player.Area.level))
                            await reaction.Channel.SendMessageAsync("You do not have the funds for this purchase.");
                        else
                        {
                            house = new House(player);
                            await house.Save();
                            await HouseCommands.ViewHouseInfo(player, house, reaction.Channel);
                        }
                    }
                    else //House Upgrade
                    {
                        await reaction.Channel.SendMessageAsync("No house upgrade available");
                    }
                    break;
                case EUI.cancel: await TryDeleteMessage(); break;

                case EUI.storage: await HouseCommands.ViewHouseStorage(player, house, 0, "all", reaction.Channel); break;
            }
        }

        public async Task HouseStorage(SocketReaction reaction, IUserMessage _)
        {
            House house = await Areas.House.House.Load(player.userid);
            (int i, string filter) = InventoryControls(reaction);
            await HouseCommands.ViewHouseStorage(player, house, i, filter, reaction.Channel);
        }
    }
}
