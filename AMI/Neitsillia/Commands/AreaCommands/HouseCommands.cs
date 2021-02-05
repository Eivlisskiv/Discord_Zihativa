﻿using AMI.Methods;
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
            Sandbox sb = house.sandbox;
            await player.NewUI("House options", DUtils.BuildEmbed($"{player.name}'s house, from {player.AreaInfo.name}",
                $"{EUI.info} Commands" + Environment.NewLine +
                $"`House Funds {{action}} {{amount}}` {sb.treasury} Kutsyei Coins" + Environment.NewLine +
                $"{EUI.storage} `House Storage {{action}}` {sb.storage.Count}/{sb.StorageSize}" + Environment.NewLine +
                $"{EUI.building} `House Build {{building name}}` {sb.tiles.Count}/{sb.tier}"// + Environment.NewLine +
                ,
                null, player.userSettings.Color).Build(), chan, MsgType.House);
        }

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

            if (sb.tiles.Count >= (sb.tier + 1))
                await ReplyAsync($"A tier {sb.tier} House may not have more than {sb.tier + 1} buildings");
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
    }
}
