using AMI.Methods;
using AMI.Module;
using AMI.Neitsillia.Areas.AreaPartials;
using AMI.Neitsillia.Areas.House;
using AMI.Neitsillia.Areas.Sandbox;
using AMI.Neitsillia.Collections;
using AMI.Neitsillia.User.PlayerPartials;
using AMI.Neitsillia.User.UserInterface;
using AMYPrototype.Commands;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;

namespace AMI.Neitsillia.Commands.AreaCommands
{
    [Name("House")]
    public class HouseCommands : ModuleBase<AMI.Commands.CustomSocketCommandContext>
    {
        
        private static async Task<House> LoadHouse(Player player, ISocketMessageChannel chan)
        {
            if(player.Area.type != Neitsillia.Areas.AreaType.Town)
                throw NeitsilliaError.ReplyError("You may only access your house from a town.");

            House house = await House.Load(player.userid);
            
            
            if(house == null || !house.junctions.Contains(player.AreaInfo.path))
            {
                Area current = player.Area;
                await player.NewUI(null, DUtils.BuildEmbed($"Buy a house in {current.name}?",
                    $"Price: {House.HousePrice(current.level)}", null, player.userSettings.Color)
                    .Build(), chan, MsgType.House, "upgrade");

                throw NeitsilliaError.ReplyError("You must pay a fee to access a house in this area.");
            }

            if (house.sandbox == null)
            {
                house.sandbox = new Sandbox();
                await house.Save();
            }

            return house;
        }

        [Command("House")]
        public async Task HouseInfo()
        {
            Player player = Context.Player;
            House house = await LoadHouse(player, Context.Channel);

            await ViewHouseInfo(player, house, Context.Channel);
        }

        public static async Task ViewHouseInfo(Player player, House house, ISocketMessageChannel chan)
        {
            await player.NewUI("House options", DUtils.BuildEmbed($"{player.name}'s house, from {player.AreaInfo.name}",
                $"{EUI.storage} `House Storage {{action}}` {house.storage.Count}/{house.storageSpace}",
                null, player.userSettings.Color).Build(), chan, MsgType.House);
        }

        [Command("House Storage")]
        public async Task HouseStorage([Summary("view, withdraw or deposit")] string action = "view", 
            [Summary("withdraw or deposit: Item {slot}x{amount} \n" +
            "view: filter")] string argument = null)
        {
            Player player = Context.Player;
            House house = await LoadHouse(player, Context.Channel);

            switch (action.ToLower())
            {
                case "deposit":
                    {
                        string item = Inventory.Transfer(
                        player.inventory, house.storage, house.storageSpace, argument);
                        await ReplyAsync($"Successfully deposited {item} in storage.");
                        await house.Save();
                        await Neitsillia.InventoryCommands.Inventory.UpdateinventoryUI(player, Context.Channel);
                    }
                    break;
                case "withdraw":
                    {
                        string item = Inventory.Transfer(
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

        [Command("House Funds")]
        public async Task HouseFunds([Summary("deposit or withdraw")] string action, long amount)
        {
            Player player = Context.Player;
            House house = await LoadHouse(player, Context.Channel);
            Sandbox sb = house.sandbox;

            switch (action.ToLower())
            {
                case "deposit":
                    {
                        if (player.KCoins < amount) { await ReplyAsync("You do not have enough Kutsyei Coins for this transfer"); return; }
                        player.KCoins -= amount;
                        sb.treasury += amount;
                        await house.Save();
                        player.SaveFileMongo();
                        await ViewHouseInfo(player, house, Context.Channel);
                    }
                    break;
                case "withdraw":
                    {
                        if (sb.treasury < amount) { await ReplyAsync("Treasury does not have enough Kutsyei Coins for this transfer"); return; }
                        player.KCoins += amount;
                        sb.treasury -= amount;
                        await house.Save();
                        player.SaveFileMongo();
                        await ViewHouseInfo(player, house, Context.Channel);
                    }
                    break;
            }
        }

        [Command("House Stock")]
        public async Task HouseStock([Summary("view, withdraw or deposit")] string action = "view",
        [Summary("withdraw or deposit: Item {slot}x{amount} \n" +
            "view: filter")] string argument = null)
        {
            Player player = Context.Player;
            House house = await LoadHouse(player, Context.Channel);
            Sandbox sb = house.sandbox;

            switch (action.ToLower())
            {
                case "deposit":
                    {
                        string item = Inventory.Transfer(
                        player.inventory, sb.storage, house.storageSpace, argument);
                        await ReplyAsync($"Successfully deposited {item} in storage.");
                        await house.Save();
                        await Neitsillia.InventoryCommands.Inventory.UpdateinventoryUI(player, Context.Channel);
                    }
                    break;
                case "withdraw":
                    {
                        string item = Inventory.Transfer(
                        sb.storage, player.inventory, player.InventorySize(), argument);
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

        [Command("House Build")]
        public async Task HouseBuild([Remainder] string build_name = null)
        {
            Context.WIPCheck();

            Player player = Context.Player;
            House house = await LoadHouse(player, Context.Channel);
            Sandbox sb = house.sandbox;

            if (sb.tiles.Count >= (sb.tier + 1))
                await ReplyAsync($"A tier {sb.tier} House may not have more than {sb.tier + 1} buildings");
            else if (EnumExtention.IsEnum(build_name.Replace(' ', '_'), out SandboxTile.TileType result))
            {
                TileSchematic ts = TileSchematics.GetSchem(result, 0);
                await player.NewUI($"Are you sure you wish to build a {ts}?",
                    ts.ToEmbed(player.userSettings.Color), Context.Channel, MsgType.ComfirmBuilding, $"0;{(int)result};house");
            }
            else
                await ReplyAsync($"{build_name} is not a valid build name. Please write a valid name:"
                    + Environment.NewLine + EnumExtention.GetNames<SandboxTile.TileType>()
                    .Join(Environment.NewLine, n => n.Replace('_', ' '))
                );
        }

        public static async Task BuildTile(Player player, SandboxTile.TileType type, ISocketMessageChannel chan)
        {
            House house = await LoadHouse(player, chan);
            Sandbox sb = house.sandbox;
            SandboxTile tile = sb.Build(type);
            await house.Save();

            await player.EditUI(null, tile.ToEmbed(sb.tier, player.userSettings.Color), chan, 
                MsgType.BuildingControls, $"{sb.tiles.Count - 1};{tile.production ?? tile.productionOptions.ToString()}");
        }
    }
}
