using AMYPrototype.Commands;
using Discord;
using System;

namespace AMI.AMIData.HelpPages
{
    partial class Help
    {
        public Embed H_social => Embed(Party);

        EmbedFieldBuilder Party => DUtils.NewField("Party",
            $"`{prefix}create party` **Creates a party with the given name**" + Environment.NewLine
            + $"`{prefix}pinv` **Send a party invite to another player**" + Environment.NewLine
            + $"`{prefix}leave party` **Leave the current party. Leaving while in combat will cost the player coins and xp.**" + Environment.NewLine
        );

        EmbedFieldBuilder Scial => DUtils.NewField("Social",
            $"`{prefix}offer` **Offer to sell items to another player**" + Environment.NewLine
            + $"`{prefix}received offers` **View all received offers**" + Environment.NewLine
            + $"`{prefix}sent offers` **View all sent offers**" + Environment.NewLine
        );
    }
}
