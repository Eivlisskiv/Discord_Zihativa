using AMI.Neitsillia.User.PlayerPartials;
using Discord;
using Discord.WebSocket;
using System.Threading.Tasks;

namespace AMI.Neitsillia.User.UserInterface
{
    partial class UI
    {
        public async Task PartyInvite(SocketReaction reaction, IUserMessage msg)
        {
            if (reaction.Emote.ToString() == EUI.cancel) { await TryDeleteMessage(); return; }
            if (await player.LoadCheck(true, reaction.Channel, Player.IgnoreException.None))
            {
                Player inviter = Player.Load(data, Player.IgnoreException.None);
                if (inviter.areaPath.path == player.areaPath.path &&
                    inviter.areaPath.floor == player.areaPath.floor)
                {
                    await inviter.Party.Add(player);
                    player.PartyKey = new AMIData.DataBaseRelation<string, NeitsilliaCommands.Party>
                        (inviter.Party._id, inviter.Party);
                    player.SaveFileMongo();
                    await EditMessage(embed: player.Party.EmbedInfo());
                }
                else await EditMessage($"{player.name} must be in the same area to join. Floor {player.areaPath.floor} of {inviter.Area.name} {inviter.Area.parent}.");
            }
        }
    }
}
