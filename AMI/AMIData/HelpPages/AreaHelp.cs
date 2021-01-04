using AMYPrototype.Commands;
using Discord;
using System;

namespace AMI.AMIData.HelpPages
{
    partial class Help
    {
        public Embed H_area => Embed(Basics_Area, Advanced_Area);

        EmbedFieldBuilder Basics_Area => DUtils.NewField("Area Basics",
            $"`{prefix}explore` **Explore your current area (Manual activities)**" + Environment.NewLine
            + $"`{prefix}adventure` **Start and adventure in your area (Automatic activities)**" + Environment.NewLine
            + $"`{prefix}rest` **Restore health and stamina over time**" + Environment.NewLine
            + $"`{prefix}travel` **View areas to travel to**" + Environment.NewLine
        );

        EmbedFieldBuilder Advanced_Area => DUtils.NewField("Area Advanced",
            $"`{prefix}service` **View available services for the current area**" + Environment.NewLine
            + $"`{prefix}area info` **Information on the current area**" + Environment.NewLine
            + $"`{prefix}jump floor` **Quickly traverse floors (4h cooldown)**" + Environment.NewLine
            + $"`{prefix}deliver` **Deliver quest items**" + Environment.NewLine
        );
    }
}
