using AMI.Neitsillia.Areas.Sandbox;
using AMI.Neitsillia.Areas.Sandbox.Schematics;
using AMI.Neitsillia.Commands.AreaCommands;
using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AMI.Neitsillia.User.UserInterface
{
    public partial class UI
    {
        static void InitO_Sandbox()
        {
            OptionsLoad.Add(MsgType.SandboxStorage, ui =>
            {
                ui.options = new List<string>()
                   { EUI.prev, EUI.cycle, EUI.next };
            });

            OptionsLoad.Add(MsgType.TileControls, ui =>
            {
                string[] ds = ui.data.Split(';');
                if (ds.Length > 2)
                {
                    ui.options = new List<string>()
                    {
                        EUI.greaterthan,
                        EUI.explosive
                    };

                    if (int.TryParse(ds[2], out int readyAmount))
                    {
                        ui.options.Insert(0, EUI.cancel);
                        if (readyAmount > 0) ui.options.Insert(0, EUI.collect);
                    }
                    else ui.options.Insert(0, EUI.produce);
                }
                else
                {
                    ui.options = new List<string>();
                    int.TryParse(ds[1], out int tileCount);
                    for (int i = 0; i < tileCount; i++)
                        ui.options.Add(EUI.GetNum(i + 1));
                }
            });

            OptionsLoad.Add(MsgType.TileProductions, ui =>
            {
                string[] ds = ui.data.Split(';');
                ui.options = new List<string>() 
                { EUI.prev, EUI.next };
                int.TryParse(ds[2], out int count);
                for (int i = 0; i < count; i++)
                    ui.options.Add(EUI.GetNum(i + 1));
            });

            OptionsLoad.Add(MsgType.TileProduce, ui 
            => ui.options = new List<string>()
            {
                        EUI.prev,
                        EUI.lowerthan,
                        EUI.greaterthan,
                        EUI.next,
                        EUI.ok
            });
        }

        private async Task<Sandbox> LoadSource(string source)
        {
            return source switch
            {
                "house" => (await Areas.House.House.Load(player.userid))?.sandbox,
                //"stronghold" => (await Areas.House.House.Load(player.userid))?.sandbox,
                _ => null,
            };
        }

        public async Task SandboxStorage(SocketReaction reaction, IUserMessage _)
        {
            string[] ds = data.Split(';');
            string source = ds[0];
            Sandbox sb = await LoadSource(source);
            (int i, string filter) = InventoryControls(reaction);
            await SandboxActions.StorageView(player, sb, source, i, filter, reaction.Channel);
        }

        public async Task ComfirmTile(SocketReaction reaction, IUserMessage msg)
        {
            //tier: -1(destroy), 0(build), ...(upgrade)
            //{locations};{tier};{type || index}
            //{house | stronghold};{int};{int}
            string[] ds = data.Split(';');
            string source = ds[0];
            int tier = int.TryParse(ds[1], out int t) ? t : 0;
            switch (reaction.Emote.ToString())
            {
                case EUI.ok:
                    if (tier == 0) //new build
                    {
                        SandboxTile.TileType type = int.TryParse(ds[2], out t) ? (SandboxTile.TileType)t : 0;
                        switch (source)
                        {
                            case "house":
                                await HouseCommands.BuildTile(player, type, reaction.Channel);
                                break;
                        }
                    }
                    else
                    {
                        int index = int.TryParse(ds[2], out t) ? t : 0;
                        switch (source)
                        {
                            case "house":
                                if (tier == -1)
                                {
                                    await HouseCommands.DestroyTile(player, index, reaction.Channel);
                                    await TryDeleteMessage();
                                }
                                else await HouseCommands.UpgradeTile(player, index, reaction.Channel);
                                break;
                            case "stronghold": break;
                        }
                    }
                    break;
                case EUI.cancel:
                    Sandbox sb = await LoadSource(source);
                    if (tier == 0)
                        await SandboxActions.ViewTiles(player, sb, source, reaction.Channel);
                    else 
                    {
                        int index = int.TryParse(ds[2], out t) ? t : 0;
                        await SandboxActions.InspectTile(player, sb, source, index, reaction.Channel);
                    }
                    break;
            }
        }

        public async Task TileControls(SocketReaction reaction, IUserMessage msg)
        {
            string[] ds = data.Split(';');
            string source = ds[0];
            Sandbox sb = await LoadSource(source);
            string emote = reaction.Emote.ToString();
            if (ds.Length > 2) //Is specified tile controls
            {
                int.TryParse(ds[1], out int i);
                switch (emote)
                {
                    case EUI.produce:
                        await SandboxActions.ProductSelection(player, sb, source, i, 0, reaction.Channel);
                        break;
                    case EUI.cancel:
                        switch (source)
                        {
                            case "house": await HouseCommands.CancelProduction(player, i, reaction.Channel); break;
                        }
                        break;
                    case EUI.collect:
                        switch (source)
                        {
                            case "house": await HouseCommands.CollectProduction(player, i, reaction.Channel); break;
                        }
                        break;
                    case EUI.greaterthan:
                        var tile = sb.tiles[i];
                        int tier = tile.tier + 1;
                        if (tier > sb.tier)
                            await reaction.Channel.SendMessageAsync($"Tiles may not be higher tier than your {source}. Max Tier: {sb.tier}");
                        else
                        {
                            var schem = TileSchematics.GetSchem(tile.type, tier);
                            await player.EditUI($"Upgrade {tile.Name} to tier {tier}?", 
                                schem.ToEmbed(player.userSettings.Color), reaction.Channel, MsgType.ComfirmTile, $"{source};{tier};{i}");
                        }
                        break;
                    case EUI.explosive:
                        await player.EditUI($"Destroy {sb.tiles[i].Name} ?" + Environment.NewLine
                            + "Current production will be lost! No refunds.", null, reaction.Channel,
                            MsgType.ComfirmTile, $"{source};-1;{i}");
                        break;
                }
                return;
            }

            int index = EUI.GetNum(emote) - 1;
            if (index <= -1) return;
            await SandboxActions.InspectTile(player, sb, "house", index, reaction.Channel);
        }

        public async Task TileProductions(SocketReaction reaction, IUserMessage _)
        {
            string[] ds = data.Split(';');
            string source = ds[0];
            Sandbox sb = await LoadSource(source);
            string emote = reaction.Emote.ToString();
            int.TryParse(ds[1], out int tileIndex);
            int.TryParse(ds[3], out int page);
            
            switch (emote)
            {
                case EUI.prev:
                    await SandboxActions.ProductSelection(player, sb, source, tileIndex, page - 1, reaction.Channel);
                    break;
                case EUI.next:
                    await SandboxActions.ProductSelection(player, sb, source, tileIndex, page + 1, reaction.Channel);
                    break;
            }

            int productIndex = EUI.GetNum(emote) - 1;
            if (productIndex >= 0)//{source};{tile index};{products count}
            {
                await SandboxActions.ProduceAmount(player, sb,
                   source, tileIndex, (page * SandboxActions.RECIPE_PERP) + productIndex, 1, reaction.Channel);
                return;
            }
        }

        public async Task TileProduce(SocketReaction reaction, IUserMessage _)
        {
            string[] ds = data.Split(';');
            string source = ds[0];
            Sandbox sb = await LoadSource(source);
            string emote = reaction.Emote.ToString();
            int.TryParse(ds[1], out int tileIndex);

            //{source};{tile index};{product index};{amount}
            int.TryParse(ds[2], out int productIndex);
            int.TryParse(ds[3], out int amount);
            switch (emote)
            {
                case EUI.prev:
                    await SandboxActions.ProduceAmount(player, sb,
                    source, tileIndex, productIndex, Math.Max(amount - 5, 1), reaction.Channel);
                    break;
                case EUI.lowerthan:
                    if (amount == 1) return;
                    await SandboxActions.ProduceAmount(player, sb,
                    source, tileIndex, productIndex, amount - 1, reaction.Channel);
                    break;
                case EUI.greaterthan:
                    await SandboxActions.ProduceAmount(player, sb,
                    source, tileIndex, productIndex, amount + 1, reaction.Channel);
                    break;
                case EUI.next:
                    await SandboxActions.ProduceAmount(player, sb,
                    source, tileIndex, productIndex, amount + 5, reaction.Channel);
                    break;


                case EUI.ok:
                    SandboxTile tile = sb.tiles[tileIndex];
                    ProductionRecipe recipe = ProductionRecipes.Get(tile.type, tile.productionOptions[productIndex]);
                    switch (source)
                    {
                        case "house":
                            await HouseCommands.Produce(player, tileIndex, recipe, amount, reaction.Channel);
                            break;
                    }
                    break;
            }
        }
    }
}
