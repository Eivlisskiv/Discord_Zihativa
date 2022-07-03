using AMI.Methods;
using AMI.Neitsillia.Areas.Sandbox.Schematics;
using AMI.Neitsillia.Collections;
using AMI.Neitsillia.User.PlayerPartials;
using AMI.Neitsillia.User.UserInterface;
using AMYPrototype.Commands;
using Discord;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;

namespace AMI.Neitsillia.Areas.Sandbox
{
    public static class SandboxActions
    {
        public const int RECIPE_PERP = 5;

        public static bool TransferFunds(Player player, Sandbox sb, string action, long amount, out string result)
        {
            if(amount < 1)
            {
                result = "Amount may not be smaller than 1";
                return false;
            }
            result = "Invalid actions. Must be Withdraw or Deposit";
            switch (action.ToLower())
            {
                case "deposit":
                    if (player.KCoins < amount)
                    {
                        result = "You do not have enough Kutsyei Coins for this transfer";
                        return false;
                    }
                    player.KCoins -= amount;
                    sb.treasury += amount;
                    result = $"Transfered {amount} from inventory to treasury";
                    return true;
                case "withdraw":
                    if (sb.treasury < amount) 
                    {
                        result = "Treasury does not have enough Kutsyei Coins for this transfer";
                        return false;
                    }
                    player.KCoins += amount;
                    sb.treasury -= amount;
                    result = $"Transfered {amount} from treasury to inventory";
                    return true;
                default:
                    return false;
            }
        }

        public static bool TransferItem(Player player, Sandbox sb, string action, 
            string argument, out string reply)
        {
            reply = null;
            string item;
            switch (action.ToLower())
            {
                case "deposit":
                    item = Inventory.Transfer(
                    player.inventory, sb.storage, sb.StorageSize, argument);
                    reply = $"Successfully deposited {item} in storage.";
                    return true;
                case "withdraw":
                    item = Inventory.Transfer(
                    sb.storage, player.inventory, player.InventorySize(), argument);
                    reply = $"Successfully withdrew {item} from storage.";
                    return true;
                default: return false;
            }
        }

        public static async Task StorageView(Player player, Sandbox sb, string source, int page, string filter, IMessageChannel chan, bool edit = true)
        {
            Embed embed = sb.storage.ToEmbed(ref page, ref filter,
                "Storage", sb.StorageSize, player.equipment).Build();
            if (edit) await player.EditUI(null, embed, chan, MsgType.SandboxStorage, $"{source};{page};{filter}");
            else await player.NewUI(null, embed, chan, MsgType.SandboxStorage, $"{source};{page};{filter}");
        }

        internal static async Task ViewTiles(Player player, Sandbox sandbox, string source, IMessageChannel channel)
        {
            await player.EditUI(null, DUtils.BuildEmbed("Buildings", 
                sandbox.tiles.Join(Environment.NewLine, (tile, i) => $"{EUI.GetNum(i + 1)} {tile.Name}") ?? "No buildings to display"
                ).Build(), channel, MsgType.TileControls, $"{source};{sandbox.tiles.Count}");
        }

        internal static async Task InspectTile(Player player, Sandbox sb, string source, int index, IMessageChannel channel)
        {
            SandboxTile tile = sb.tiles[index];
            await player.EditUI(null, tile.ToEmbed(sb.tier, player.userSettings.Color), channel,
                MsgType.TileControls, $"{source};{index};{(tile.production != null ? tile.AmountReady.ToString() : "null")}");
        }

        internal static async Task ProduceAmount(Player player, Sandbox sb, string source, int tileIndex, int productIndex, int amount, IMessageChannel channel)
        {
            SandboxTile tile = sb.tiles[tileIndex];
            ProductionRecipe recipe = ProductionRecipes.Get(tile.type, tile.productionOptions[productIndex]);

            await player.EditUI(null, DUtils.BuildEmbed($"Product {recipe} from {tile.Name}", null, null, player.userSettings.Color,
                DUtils.NewField("Select Amount", 
                $"{EUI.ok} Produce **[{amount}]**" + Environment.NewLine +
                $"{EUI.prev}|{EUI.lowerthan}|{EUI.greaterthan}|{EUI.next}"
                + Environment.NewLine +
                $" -5 | -1 | +1 | +5 |" + Environment.NewLine +
                $"Or `amount` command: `amount 10`"),
                recipe.ToField(amount)
                ).Build(), channel, MsgType.TileProduce, $"{source};{tileIndex};{productIndex};{amount}");
        }

        internal static async Task ProductSelection(Player player, Sandbox sb, string source, int tileIndex, int page, IMessageChannel channel)
        {
            SandboxTile tile = sb.tiles[tileIndex];

            int count = tile.productionOptions.Count;
            int max = count / RECIPE_PERP;
            if (page < 0) page = max;
            else if (page > max) page = max;

            int start = page * RECIPE_PERP;
            var products = tile.productionOptions.GetRange(start, Math.Min(RECIPE_PERP, count - start));

            await player.EditUI(null, DUtils.BuildEmbed($"{tile.Name} Productions", 
                products.Join(Environment.NewLine, (name, i) => $"{EUI.GetNum(i + 1)} {name}"),
                null, player.userSettings.Color).Build(), channel, 
                MsgType.TileProductions, $"{source};{tileIndex};{products.Count};{page}");
        }
    }
}
