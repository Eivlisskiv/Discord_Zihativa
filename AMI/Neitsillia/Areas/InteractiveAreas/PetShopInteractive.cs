using AMI.Neitsillia.User.PlayerPartials;
using AMI.Neitsillia.User.UserInterface;
using AMYPrototype.Commands;
using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AMI.Neitsillia.Areas.InteractiveAreas
{
    public static class PetShopInteractive
    {        
        internal static async Task PetShopUi(Player player, ISocketMessageChannel chan)
        {
            string data = "";

            EmbedField[] fields =
            {
                DUtils.NewField("Services",
                $"{EUI.eggPocket} Upgrade Egg Pocket"
                    + Environment.NewLine + $"{EUI.pets} Sell Companions"
                    + Environment.NewLine + $"{EUI.pets} Modify Companion"
                    + Environment.NewLine + $"{EUI.pets} Evolve Companion").Build(),
                //new int[]{
                Commands.Areas.AvailableQuests(player, ref data, (0,3,0) )
            };

            await player.NewUI(
                await chan.SendMessageAsync(
                    embed: player.UserEmbedColor(DUtils.BuildEmbed("Beast Master Store", 
                    "Welcome to my store, Traveler! What service may I offer you?", 
                    fields: fields
                    )).Build())
                    , MsgType.PetShop, data);
        }
    }
}
