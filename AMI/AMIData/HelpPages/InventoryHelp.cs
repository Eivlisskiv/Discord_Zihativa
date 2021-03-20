using AMI.Neitsillia.User.UserInterface;
using AMYPrototype.Commands;
using Discord;
using AMI.Neitsillia.Items.ItemPartials;
using System;

namespace AMI.AMIData.HelpPages
{
    partial class Help
    {
        public Embed H_inventory => Embed(Inventory, Equipment, Inventory_Icons);

        public Embed H_crafting => Embed(Crafting, Inventory_Icons);

        public Embed H_schematic => Embed(PermaSchematics, TempoSchematics);

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

        EmbedFieldBuilder PermaSchematics => DUtils.NewField("Permanent Schematics",
            $"Your character's Permanent Schematics are located in `{prefix}schems`" + Environment.NewLine
            + $"**-** They are permanent and may not be dropped or removed." + Environment.NewLine
            + $"**-** Can be used unlimited times." + Environment.NewLine
            + $"**-** Required to upgrade the item." + Environment.NewLine
            + $"**-** Exclusively obtained from scrapping." + Environment.NewLine
            );
        EmbedFieldBuilder TempoSchematics => DUtils.NewField("Consumable Schematics",
            $"These are items stored in `{prefix}inv`" + Environment.NewLine
            + $"**-** They can only be used once using `{prefix}use`." + Environment.NewLine
            + $"**-** Some rare items may only be obtained from crafting from these kind of schematics." + Environment.NewLine
            + $"**-** Various drop locations." + Environment.NewLine
            );
    }
}
