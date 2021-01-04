using AMYPrototype.Commands;
using Discord;
using System;

namespace AMI.AMIData.HelpPages
{
    partial class Help
    {
        public Embed H_main => Embed(Basics_Character, Basics_Stats, Basics_Area);
        public Embed H_stats => Embed(Basics_Stats, Equipment, Inventory);


        EmbedFieldBuilder Basics_Character => DUtils.NewField("Character Basics",
            $"`{prefix}new char` **Create a new character**" + Environment.NewLine
            + $"`{prefix}chars` **Get a list of your character**" + Environment.NewLine
            + $"`{prefix}load` **load a character**" + Environment.NewLine
            + $"`{prefix}delete` **delete a character**" + Environment.NewLine
        );

        EmbedFieldBuilder Basics_Stats => DUtils.NewField("Stats Basics",
            $"`{prefix}stats` **View your character's basic stats**" + Environment.NewLine
            + $"`{prefix}ls` **View all character's stats**" + Environment.NewLine
            + $"`{prefix}inv` **Open character inventory**" + Environment.NewLine
            + $"`{prefix}quest` **View character quests**" + Environment.NewLine
        );

        EmbedFieldBuilder Equipment => DUtils.NewField("Equipment",
            $"`{prefix}equip` **Equip gear using the slot number in your inventory**" + Environment.NewLine
            + $"`{prefix}autoequip` **Automatically equips gear from your inventory**" + Environment.NewLine
            + $"`{prefix}unequip` **Return the equipped item to your inventory**" + Environment.NewLine
            + $"`{prefix}strip` **Unequip all gear**" + Environment.NewLine
            + $"`{prefix}compare` **Compare item stats**" + Environment.NewLine
        );
    }
}
