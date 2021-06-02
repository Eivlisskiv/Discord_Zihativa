using AMI.Neitsillia.User.UserInterface;
using AMYPrototype.Commands;
using Discord;
using AMI.Neitsillia.Items.ItemPartials;
using System;

namespace AMI.AMIData.HelpPages
{
    partial class Help
    {
        public Embed H_inventory => Embed(Inventory, Equipment, Inventory_Icons, Rarity_Icons);

        public Embed H_crafting => Embed(Crafting, GeneralSchematics, Inventory_Icons);

        public Embed H_schematic => Embed(GeneralSchematics, PermaSchematics, TempoSchematics);

        EmbedFieldBuilder Inventory => DUtils.NewField("Inventory",
            $"`{prefix}inv` **open and view your inventory**" + Environment.NewLine
            + $"`{prefix}sort inv` **Sort your inventory**" + Environment.NewLine
            + $"`{prefix}inspect` **View details on an item from the inventory slot number**" + Environment.NewLine
            + $"`{prefix}drop` **Remove an item from your inventory (cannot be undone, item will be lost)**" + Environment.NewLine
            + $"`{prefix}use` **Use an item from your inventory from the inventory slot. The effect depends on the item used**"
        );

        EmbedFieldBuilder Inventory_Icons => DUtils.NewField("Inventory Icons",
            $"{EUI.greaterthan} Gear piece of **higher** tier than equipped" + Environment.NewLine
            + $"{EUI.equalStats} Gear piece of **equal** tier than equipped" + Environment.NewLine
            + $"{EUI.lowerthan} Gear piece of **lower** tier than equipped" + Environment.NewLine
            + $"{EUI.ItemType(Item.IType.Material)} Materials: Mainly used for crafting and upgrading" + Environment.NewLine
            + $"{EUI.ItemType(Item.IType.Consumable)} Consumable: Mainly to consume for healing or effects, can be used for crafting"
        );

        EmbedFieldBuilder Crafting => DUtils.NewField("Crafting",
             $"`{prefix}scrap` **Scrap/Dismantle an item and retrieve crafting materials (Item requires a recipe linked to it)**" + Environment.NewLine
             + $"`{prefix}bscrap` **Bulk scrap items (enter multiple values)**" + Environment.NewLine
             + $"`{prefix}schems` **Get a list of all Schematics known by this character**" + Environment.NewLine
             + $"`{prefix}craft` **Craft an item from your 'Schematics' list**" + Environment.NewLine
             + $"`{prefix}use` **Use a temporary schematic from your inventory to craft from inventory slot**"
        );

        EmbedFieldBuilder GeneralSchematics => DUtils.NewField("Schematics Informations",
            "**Only gear schematics can be obtained from scraping**." + Environment.NewLine +
            "Some items can only be obtained from **Consumable Schematics**.");

        EmbedFieldBuilder PermaSchematics => DUtils.NewField("Permanent Schematics",
            $"Your character's Permanent Schematics are located in `{prefix}schems`" + Environment.NewLine
            + $"**-** They are permanent and may not be dropped or removed." + Environment.NewLine
            + $"**-** Can be used unlimited times." + Environment.NewLine
            + $"**-** Required to upgrade the item." + Environment.NewLine
            + $"**-** Exclusively obtained from scrapping."
            );
        EmbedFieldBuilder TempoSchematics => DUtils.NewField("Consumable Schematics",
            $"These are items stored in `{prefix}inv`" + Environment.NewLine
            + $"**-** They can only be used once using `{prefix}use`." + Environment.NewLine
            + $"**-** Some rare items may only be obtained from crafting from these kind of schematics." + Environment.NewLine
            + $"**-** Various drop locations."
            );

        EmbedFieldBuilder Rarity_Icons => DUtils.NewField("Inventory Icons",
            "<:White:808032848305061898> Normal Base Tier or near base tier" + Environment.NewLine
            + "<:Green:808032848044228630> Item is stronger than its base stats" + Environment.NewLine
            + "<:Blue:808027516748431384> Item is greatly stronger than its base stats" + Environment.NewLine
            + "<:Orange:808027517058547743> Item has a perk" + Environment.NewLine
            + "<:Purple:808027517063659520> Item is unique" + Environment.NewLine
            + "<:Red:808027517272588378> Item has a perk and is unique"
        );
    }
}
