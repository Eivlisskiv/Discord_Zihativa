using AMI.Module;
using Discord;
using Discord.WebSocket;
using System.Threading.Tasks;

namespace AMI.Neitsillia.User.UserInterface
{
    partial class UI
    {
        public async Task NPCInv(SocketReaction reaction, IUserMessage msg)
        {
            if (!int.TryParse(data, out int i)) i = 1;
            if (reaction.Emote.ToString() == EUI.prev)
                await ShopCommands.ViewNPCInv(player, reaction.Channel, i - 1, true);
            else if (reaction.Emote.ToString() == EUI.next)
                await ShopCommands.ViewNPCInv(player, reaction.Channel, i + 1, true);
        }

        public async Task NPC(SocketReaction reaction, IUserMessage msg)
        {
            if (reaction.Emote.ToString() == EUI.trade)
                await ShopCommands.ViewNPCInv(player, reaction.Channel, 1, true);
        }

        public async Task ConfirmTransaction(SocketReaction reaction, IUserMessage msg)
        {
            switch (reaction.Emote.ToString())
            {
                case EUI.ok:
                    await reaction.Channel.SendMessageAsync(
                        await Shopping.PendingTransaction.Accept(player, data));
                    await ShopCommands.ViewNPCInv(player, reaction.Channel, 0, true);
                    break;
                case EUI.cancel:
                    await reaction.Channel.SendMessageAsync(
                        Shopping.PendingTransaction.Cancel(player, data));

                    await ShopCommands.ViewNPCInv(player, reaction.Channel, 0, true);
                    break;
                default: return;
            }
            player.SaveFileMongo();
        }

        public async Task NPCRepair(SocketReaction reaction, IUserMessage msg)
        {
            switch (reaction.Emote.ToString())
            {
                case EUI.ok:
                    await ShopCommands.ConfirmNPCRepair(player, int.Parse(data), reaction.Channel);
                    break;
                case EUI.cancel: await TryDeleteMessage(); break;
            }
        }
    }
}
