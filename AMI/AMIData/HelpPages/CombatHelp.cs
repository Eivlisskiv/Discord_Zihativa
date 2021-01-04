using AMYPrototype.Commands;
using Discord;
using System;

namespace AMI.AMIData.HelpPages
{
    partial class Help
    {
        public Embed H_combat => Embed(Basic_Combat, Combat_Abilities);

        EmbedFieldBuilder Basic_Combat => DUtils.NewField("Basic Combat",
            $"`{prefix}cast` **Cast an ability**" + Environment.NewLine
            + $"`{prefix}run` **Escape battle**" + Environment.NewLine
            + $"`{prefix}ability` **View abilities your character can cast**" + Environment.NewLine
            + $"`{prefix}consume` **Consuming something during combat will count as a turn**" + Environment.NewLine
        );

        EmbedFieldBuilder Combat_Abilities => DUtils.NewField("Abilities",
            $"**USE YOUR ABILITIES**" + Environment.NewLine
            + $"Abilities level up and become stronger when used! Stronger abilities will allow you to defeat stronger foes no matter the gear you have equipped" + Environment.NewLine
        );
    }
}
