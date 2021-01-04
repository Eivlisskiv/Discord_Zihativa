using AMYPrototype.Commands;
using Discord;
using System;

namespace AMI.AMIData.HelpPages
{
    partial class Help
    {
        public Embed H_npc => Embed(Basics_NPC);

        EmbedFieldBuilder Basics_NPC => DUtils.NewField("NPC Basics",
            $"`{prefix}trade` **View the npc's inventory**" + Environment.NewLine
            + $"`{prefix}buy/sell` **Buy or sell items**" + Environment.NewLine
            + $"`{prefix}recruit` **Recruit the npc into your party**" + Environment.NewLine
        );
    }
}
