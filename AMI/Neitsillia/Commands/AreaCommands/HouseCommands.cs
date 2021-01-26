using AMI.Module;
using AMI.Neitsillia.Areas.AreaPartials;
using AMI.Neitsillia.Areas.House;
using AMI.Neitsillia.Collections;
using AMI.Neitsillia.User.PlayerPartials;
using AMI.Neitsillia.User.UserInterface;
using AMYPrototype.Commands;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Threading.Tasks;

namespace AMI.Neitsillia.Commands.AreaCommands
{
    [Name("House")]
    public class HouseCommands : ModuleBase<AMI.Commands.CustomSocketCommandContext>
    {
        
        private async Task<House> LoadHouse()
        {
            Player player = Context.Player;
            if(player.Area.type != Neitsillia.Areas.AreaType.Town)
                throw NeitsilliaError.ReplyError("You may only access your house from a town.");

            House house = await House.Load(player.userid);
            
            
            if(house == null || !house.junctions.Contains(player.AreaInfo.path))
            {
                Area current = player.Area;
                await player.NewUI(null, DUtils.BuildEmbed($"Buy a house in {current.name}?",
                    $"Price: {House.HousePrice(current.level)}", null, player.userSettings.Color())
                    .Build(), Context.Channel, MsgType.House, "upgrade");

                throw NeitsilliaError.ReplyError("You must pay a fee to access a house in this area.");
            }
            return house;
        }

        [Command("House")]
        public async Task HouseInfo()
        {
            Player player = Context.Player;
            House house = await LoadHouse();

            await ViewHouseInfo(player, house, Context.Channel);
        }

        public static async Task ViewHouseInfo(Player player, House house, ISocketMessageChannel chan)
        {
            await player.NewUI("House options", DUtils.BuildEmbed($"{player.name}'s house, from {player.AreaInfo.name}",
                $"{EUI.storage} `House Storage {{action}}` {house.storage.Count}/{house.storageSpace}",
                null, player.userSettings.Color()).Build(), chan, MsgType.House);
        }

        [Command("House Storage")]
        public async Task HouseStorage([Summary("view, withdraw or deposit")] string action = "view", 
            [Summary("withdraw or deposit: Item {slot}x{amount} \n" +
            "view: filter")] string argument = null)
        {
            Player player = Context.Player;
            House house = await LoadHouse();

            switch (action.ToLower())
            {
                case "deposit":
                    {
                        string item = await Inventory.Transfer(
                        player.inventory, house.storage, house.storageSpace, argument);
                        await ReplyAsync($"Successfully deposited {item} in storage.");
                        await house.Save();
                        await Neitsillia.InventoryCommands.Inventory.UpdateinventoryUI(player, Context.Channel);
                    }
                    break;
                case "withdraw":
                    {
                        string item = await Inventory.Transfer(
                        house.storage, player.inventory, player.InventorySize(), argument);
                        await ReplyAsync($"Successfully withdrew {item} from storage.");
                        await house.Save();
                        await Neitsillia.InventoryCommands.Inventory.UpdateinventoryUI(player, Context.Channel);
                    }
                    break;
                default:
                    await ViewHouseStorage(player, house, 0, argument ?? "all", Context.Channel, false);
                    break;
            }
        }

        public static async Task ViewHouseStorage(Player player, House house, int page, string filter, ISocketMessageChannel chan, bool edit = true)
        {
            Embed embed = house.storage.ToEmbed(ref page, ref filter,
                "Storage", house.storageSpace, player.equipment).Build();
            if (edit) await player.EditUI(null, embed, chan, MsgType.HouseStorage, $"{page};{filter}");
            else await player.NewUI(null, embed, chan, MsgType.HouseStorage, $"{page};{filter}");
        }
    }
}
