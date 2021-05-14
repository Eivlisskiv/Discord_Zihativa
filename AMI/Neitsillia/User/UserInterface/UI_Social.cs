using AMI.Neitsillia.NeitsilliaCommands;
using AMI.Neitsillia.Social.Mail;
using AMI.Neitsillia.User.PlayerPartials;
using Discord;
using Discord.WebSocket;
using System.Threading.Tasks;

namespace AMI.Neitsillia.User.UserInterface
{
    partial class UI
    {
        static void InitO_Social()
        {
            OptionsLoad.Add(MsgType.Inbox, ui => {
                string[] ds = ui.data.Split(';');
                string[] ids = ds[1].Split(',');

                ui.options = new System.Collections.Generic.List<string>() { EUI.prev };
                for (int i = 0; i < ids.Length; i++)
                    ui.options.Add(EUI.GetNum(i + 1));
                ui.options.Add(EUI.next);
            });
        }
        public async Task Inbox(SocketReaction reaction, IUserMessage msg)
        {
            string emote = reaction.Emote.ToString();
            string[] ds = data.Split(';');
            int.TryParse(ds[0], out int page);
            int i = EUI.GetNum(emote);
            if(i > -1)
            {
                string[] ids = ds[1].Split(',');
                Mail mail = await Mail.Load(ids[i - 1]);
                if (mail == null) return;
                var user = BotUser.Load(reaction.UserId);
                await mail.Collect(Player.Load(user, Player.IgnoreException.All), reaction.Channel);
                await SocialCommands.ViewInbox(user, page, reaction.Channel, true);
            }
            else
            {
                switch (emote)
                {
                    case EUI.prev:
                        await SocialCommands.ViewInbox(BotUser.Load(reaction.UserId), page - 1, reaction.Channel, true);
                        break;
                    case EUI.next:
                        await SocialCommands.ViewInbox(BotUser.Load(reaction.UserId), page + 1, reaction.Channel, true);
                        break;
                }
            }

        }

        public async Task PartyInvite(SocketReaction reaction, IUserMessage msg)
        {
            if (reaction.Emote.ToString() == EUI.cancel) { await TryDeleteMessage(); return; }
            if (await player.LoadCheck(true, reaction.Channel, Player.IgnoreException.None))
            {
                Player inviter = Player.Load(data, Player.IgnoreException.None);
                if (inviter.AreaInfo.path == player.AreaInfo.path &&
                    inviter.AreaInfo.floor == player.AreaInfo.floor)
                {
                    await inviter.Party.Add(player);
                    await EditMessage(embed: player.Party.EmbedInfo(null));
                }
                else await EditMessage($"{player.name} must be in the same area to join. Floor {inviter.AreaInfo.floor} of {inviter.Area.name} {inviter.Area.parent}.");
            }
        }
    }
}
