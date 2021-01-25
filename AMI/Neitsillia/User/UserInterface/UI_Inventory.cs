using AMI.Module;
using Discord;
using Discord.WebSocket;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AMI.Neitsillia.User.UserInterface
{
    public partial class UI
    {
        static void InitO_Inventory()
        {
            OptionsLoad.Add(MsgType.Inventory, ui =>
               ui.options = new List<string>()
               { EUI.prev, EUI.cycle, EUI.next, EUI.loot }
           );
        }

        public async Task Inventory(SocketReaction reaction, IUserMessage msg)
        {
            (int i, string filter) = InventoryCommands.Inventory.ParseInvUIData(data);
            switch (reaction.Emote.ToString())
            {
                case EUI.prev:
                    await GameCommands.DisplayInventory(player, 
                        reaction.Channel, i - 1, filter, true);
                    break;
                case EUI.next:
                    await GameCommands.DisplayInventory(player, 
                        reaction.Channel, i + 1, filter, true);
                    break;
                case EUI.cycle:
                    filter = filter switch
                    {
                        "all" => "gear",
                        "gear" => "consumable",
                        _ => "all"
                    };
                    await GameCommands.DisplayInventory(player, 
                        reaction.Channel, i, filter, true);
                    break;
            }
        }
    }
}
