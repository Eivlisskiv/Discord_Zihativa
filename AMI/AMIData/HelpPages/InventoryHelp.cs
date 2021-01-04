using AMI.Neitsillia.User.UserInterface;
using AMYPrototype.Commands;
using Discord;
using Neitsillia.Items.Item;
using System;

namespace AMI.AMIData.HelpPages
{
    partial class Help
    {
        public Embed H_inventory => Embed(Inventory, Equipment, Inventory_Icons);

        public Embed H_crafting => Embed(Crafting, Inventory_Icons);

        EmbedFieldBuilder Inventory => DUtils.NewField("Inventory",
            $"`{prefix}inv` **open and view your inventory**" + Environment.NewLine
            + $"`{prefix}sort inv` **Sort your inventory**" + Environment.NewLine
            + $"`{prefix}inspect` **View details on an item from the inventory slot number**" + Environment.NewLine
            + $"`{prefix}drop` **Remove an item from your inventory (cannot be undone, item will be lost)**" + Environment.NewLine
            + $"`{prefix}use` **Use an item from your inventory from the inventory slot. The effect depends on the item used**" + Environment.NewLine
        );

        EmbedFieldBuilder Inventory_Icons => DUtils.NewField("Inventory Icons",
            $"{EUI.greaterthan} Gear piece of **higher** tier than equipped" + Environment.NewLine
            + $"{EUI.equalStats} Gear piece of **equal** tier than equipped" + Environment.NewLine
            + $"{EUI.lowerthan} Gear piece of **lower** tier than equipped" + Environment.NewLine
            + $"{EUI.ItemType(Item.IType.Material)} Materials: Mainly used for crafting and upgrading" + Environment.NewLine
            + $"{EUI.ItemType(Item.IType.Consumable)} Consumable: Mainly to consume for healing or effects, can be used for crafting" + Environment.NewLine
        );

        EmbedFieldBuilder Crafting => DUtils.NewField("Crafting",
             $"`{prefix}scrap` **Scrap/Dismantle an item and retrieve crafting materials (Item requires a recipe linked to it)**" + Environment.NewLine
             + $"`{prefix}bscrap` **Bulk scrap items (enter multiple values)**" + Environment.NewLine
             + $"`{prefix}schems` **Get a list of all Schematics known by this character**" + Environment.NewLine
             + $"`{prefix}craft` **Craft an item from your 'Schematics' list**" + Environment.NewLine
             + $"`{prefix}use` **Use a temporary schematic from your inventory to craft from inventory slot**" + Environment.NewLine
        );

        EmbedFieldBuilder Schematics => DUtils.NewField("Schematics",
            "There are two types of schematics:" + Environment.NewLine
            + $"Learned Schematics: These are permanent and unlimited. They are also needed to upgrade the item or repair without repair kits. Located in `{prefix}schems`" + Environment.NewLine
            + $"One time Schematics: These are located in your `{prefix}inventory` and can only be used once using `{prefix}use` command" + Environment.NewLine
            );
    }
}
