using AMI.Methods;
using AMI.Module;
using AMI.Neitsillia.Areas.AreaPartials;
using AMI.Neitsillia.Areas.House;
using AMI.Neitsillia.Areas.Sandbox;
using AMI.Neitsillia.Areas.Sandbox.Schematics;
using AMI.Neitsillia.User.PlayerPartials;
using AMI.Neitsillia.User.UserInterface;
using AMYPrototype.Commands;
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
                    .Build(), chan, MsgType.Sandbox, "house;confirm");

                throw NeitsilliaError.ReplyError("You must pay a fee to access a house in this area.");
            }

            if (house.sandbox == null)
            {
                house.sandbox = new Sandbox();
                await house.Save();
            }

            return house;
        }

        internal static async Task Upgrade(Player player, ISocketMessageChannel channel)
        {
            House house = await House.Load(player.userid);
            if (house == null) //new house
            {
                if (player.KCoins < House.HousePrice(player.Area.level))
                    await channel.SendMessageAsync("You do not have the funds for this purchase.");
                else
                {
                    house = new House(player);
                    await house.Save();
                    await ViewHouseInfo(player, house, channel);
                }
            }
            else if (!house.junctions.Contains(player.AreaInfo.path))
            {
                if (player.KCoins < House.HousePrice(player.Area.level))
                    await channel.SendMessageAsync("You do not have the funds for this purchase.");
                else
                {
                    house.junctions.Add(player.AreaInfo.path);
                    await house.Save();
                    await ViewHouseInfo(player, house, channel);
                }
            }
            else //House Upgrade
            {
                house.sandbox.Upgrade(Sandbox.MAX_TIER_HOUSE);
                await house.Save();
                await ViewHouseInfo(player, house, channel);
            }
        }

        [Command("House")]
        public async Task HouseInfo()
        {
            Player player = Context.Player;
            House house = await LoadHouse(player, Context.Channel);

            await ViewHouseInfo(player, house, Context.Channel, false);
        }

        public static async Task ViewHouseInfo(Player player, House house, ISocketMessageChannel chan, bool edit = true)
            => await player.EnUI(edit, "House options", house.sandbox.ToEmbed("House", player), chan, MsgType.Sandbox, 
                $"house;{(house.sandbox.CanUpgrade(Sandbox.MAX_TIER_HOUSE) ? "upgrade" : "none")}");

        [Command("House Funds")]
        public async Task HouseFunds([Summary("deposit or withdraw")] string action, long amount)
        {
            Player player = Context.Player;
            House house = await LoadHouse(player, Context.Channel);
            Sandbox sb = house.sandbox;

            if (SandboxActions.TransferFunds(player, sb, action, amount, out string result))
            {
                await house.Save();
                player.SaveFileMongo();
                await ViewHouseInfo(player, house, Context.Channel);
            }

            await ReplyAsync(result);
        }

        [Command("House Storage")]
        public async Task HouseStock([Summary("view, withdraw or deposit")] string action = "view",
        [Summary("withdraw or deposit: Item {slot}x{amount} \n" +
            "view: filter")] string argument = null)
        {
            Player player = Context.Player;
            House house = await LoadHouse(player, Context.Channel);
            Sandbox sb = house.sandbox;

            if (SandboxActions.TransferItem(player, sb, action, argument, out string reply))
            {
                await house.Save();
                await Neitsillia.InventoryCommands.Inventory.
                    UpdateinventoryUI(player, Context.Channel);
                await ReplyAsync(reply);
            }
            else await SandboxActions.StorageView(player, house.sandbox, "house", 0, argument ?? "all",  Context.Channel, false);
        }

        [Command("House Build")]
        public async Task HouseBuild([Remainder] string build_name = null)
        {
            Player player = Context.Player;
            House house = await LoadHouse(player, Context.Channel);
            Sandbox sb = house.sandbox;

            if (sb.tiles.Count >= sb.tier)
                await ReplyAsync($"A tier {sb.tier} House may not have more than {sb.tier} buildings");
            else if (EnumExtention.IsEnum(build_name?.Replace(' ', '_'), out SandboxTile.TileType result))
            {
                TileSchematic ts = TileSchematics.GetSchem(result, 0);
                await player.NewUI($"Are you sure you wish to build a {ts}?",
                    ts.ToEmbed(player.userSettings.Color), Context.Channel, MsgType.ComfirmTile, $"house;0;{(int)result}");
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
            sb.Build(type);
            await house.Save();

            await SandboxActions.InspectTile(player, sb, "house", sb.tiles.Count - 1, chan);
        }

        public static async Task UpgradeTile(Player player, int index, ISocketMessageChannel chan)
        {
            House house = await LoadHouse(player, chan);
            Sandbox sb = house.sandbox;
            sb.UpgradeTile(sb.tiles[index]);
            await house.Save();

            await SandboxActions.InspectTile(player, sb, "house", sb.tiles.Count - 1, chan);
        }

        internal static async Task DestroyTile(Player player, int index, ISocketMessageChannel channel)
        {
            House house = await LoadHouse(player, channel);
            Sandbox sb = house.sandbox;
            sb.tiles.RemoveAt(index);
            await house.Save();
            await channel.SendMessageAsync("Tile was destoyed");
        }

        internal static async Task Produce(Player player, int tileIndex, ProductionRecipe recipe, int amount, ISocketMessageChannel channel)
        {
            House house = await LoadHouse(player, channel);
            Sandbox sb = house.sandbox;
            recipe.Consume(sb, amount);
            sb.tiles[tileIndex].Start(recipe, amount);
            await house.Save();
            await SandboxActions.InspectTile(player, sb, "house", tileIndex, channel);
        }

        internal static async Task CollectProduction(Player player, int i, ISocketMessageChannel channel)
        {
            House house = await LoadHouse(player, channel);
            Sandbox sb = house.sandbox;
            await channel.SendMessageAsync(sb.tiles[i].Collect(sb));
            await house.Save();
            await SandboxActions.InspectTile(player, sb, "house", i, channel);
        }

        internal static async Task CancelProduction(Player player, int i, ISocketMessageChannel channel)
        {
            House house = await LoadHouse(player, channel);
            Sandbox sb = house.sandbox;
            await channel.SendMessageAsync(sb.tiles[i].Cancel(sb));
            await house.Save();
            await SandboxActions.InspectTile(player, sb, "house", i, channel);
        }

        [Command("House Travel")]
        public async Task HouseTravel([Remainder] string area_name)
        {
            Player player = Context.Player;
            House house = await LoadHouse(player, Context.Channel);

            string path = house.junctions.Find(s => s.Split('\\')[^1].Equals(area_name, StringComparison.OrdinalIgnoreCase));
            if(path == null)
            {
                await ReplyAsync($"You do not have a house in {area_name}");
                return;
            }

            if (!player.IsSolo)
            {
                await ReplyAsync($"You may not do this while in a party.");
                return;
            }

            var area = Area.LoadArea(path);
            await player.SetArea(area);
            player.SaveFileMongo();
            await ReplyAsync(embed: area.AreaInfo(0).Build());
        }
    }
}
