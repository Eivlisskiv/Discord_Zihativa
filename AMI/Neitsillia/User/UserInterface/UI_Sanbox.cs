using AMI.Neitsillia.Areas.Sandbox;
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
        static void InitO_Sandbox()
        {
            OptionsLoad.Add(MsgType.BuildingControls, ui =>
                ui.options = new List<string>()
                {
                    /* Cancel Production/Select Produce
                     * 
                     */
                }
            );
        }

        public async Task ComfirmBuilding(SocketReaction reaction, IUserMessage msg)
        {
            switch (reaction.Emote.ToString())
            {
                case EUI.ok:
                    //tier: -1(destroy), 0(build), ...(upgrade)
                    //{tier};{type || index};{locations}
                    //{int};{int};{house | stronghold}
                    string[] ds = data.Split(';');
                    int tier = ds.Length > 0 && int.TryParse(ds[0], out int t) ? t : 0;
                    string location = ds.Length > 2 ? ds[2] : "home";

                    if (tier == 0) //new build
                    {
                        SandboxTile.TileType type = ds.Length > 1 && int.TryParse(ds[1], out t) ? (SandboxTile.TileType)t : 0;
                        switch (location)
                        {
                            case "home":
                            case "house":
                                await HouseCommands.BuildTile(player, type, reaction.Channel);
                                break;
                        }
                    }
                    else
                    {
                        int index = ds.Length > 1 && int.TryParse(ds[1], out t) ? t : 0;
                    }
                    break;
                case EUI.cancel:
                    await TryMSGDel(msg);
                    break;
            }
        }

        public async Task BuildingControls(SocketReaction reaction, IUserMessage msg)
        {

        }
    }
}
