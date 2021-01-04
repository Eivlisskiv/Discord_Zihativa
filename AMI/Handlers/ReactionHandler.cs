using AMI.AMIData.Servers;
using AMI.Methods;
using AMI.Module;
using AMI.Neitsillia.User;
using AMI.Neitsillia.User.PlayerPartials;
using AMI.Neitsillia.User.UserInterface;
using AMYPrototype;
using AMYPrototype.Commands;
using Discord;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;

namespace AMI.Handlers
{
    class ReactionHandler
    {
        public static async Task ReactionAddedEvent(Cacheable<IUserMessage, ulong> cachedMessage, 
            ISocketMessageChannel channel, SocketReaction reaction)
        {
            try
            {
                if (Program.CurrentState != Program.State.Ready
                    || !reaction.User.IsSpecified
                    || reaction.User.Value.IsBot)
                    return;

                IUserMessage message = cachedMessage.Value ?? (reaction.Message.IsSpecified ? reaction.Message.Value : await cachedMessage.GetOrDownloadAsync());

                //Currently, the bot does not need to check reactions on messages that are not its own
                if (message == null || message.Author.Id != Program.clientCopy.CurrentUser.Id) return;

                GuildSettings gset = channel is IGuildChannel chan ?
                    gset = GuildSettings.Load(chan.Guild) : null;

                if (gset != null && gset.Ignore) return;

                if (!CommandHandler.RunUser(reaction.UserId)) return;

                _ = new ReactionHandler(message, reaction, gset).ExecuteAsync();

            }
            catch (Exception e)
            {
                Log.LogS(e);
                _ = UniqueChannels.Instance.SendToLog(e, "ReactionAdded Error", channel);
            }
        }

        readonly BotUser botUser;
        readonly SocketReaction reaction;

        readonly IUserMessage message;

        readonly GuildSettings guildSettings;

        System.Diagnostics.Stopwatch watch;

        public ReactionHandler(IUserMessage message, SocketReaction reaction, GuildSettings gset)
        {
            watch = System.Diagnostics.Stopwatch.StartNew();

            botUser = BotUser.Load(reaction.UserId);
            this.reaction = reaction;
            this.message = message;
            guildSettings = gset;
        }

        async Task ExecuteAsync()
        {
            if (!await IsUserUI()) 
                await PlayerUI();

            CommandHandler.running.Remove(reaction.UserId);
        }

        public async Task<bool> IsUserUI()
        {
            if (botUser?.ui?.msgId != reaction.MessageId) return false;

            MsgType? uiType = botUser.ui?.type;

            await botUser.ui.Click(reaction, message, null);

            watch.Stop();
            if (Program.isDev && watch.ElapsedMilliseconds > 500)
                _ = UniqueChannels.Instance.SendToLog($"{botUser._id} {uiType} {Environment.NewLine} Slow Operation {watch.ElapsedMilliseconds}ms");
            else Console.WriteLine($"{botUser._id} {uiType} => {watch.ElapsedMilliseconds}ms");

            return true;
        }

        private async Task PlayerUI()
        {
            try
            {
                Player player = Player.Load(botUser, Player.IgnoreException.All);
                if (player?.ui?.msgId != reaction.MessageId) return;
                try
                {
                    MsgType? uiType = player.ui?.type;
                    if (await player.LoadCheck(true, message.Channel, Player.IgnoreException.Transaction, Player.IgnoreException.MiniGames))
                        await player.ui.Click(reaction, message, player);

                    watch.Stop();
                    if (Program.isDev && watch.ElapsedMilliseconds > 500)
                        _ = UniqueChannels.Instance.SendToLog($"{player._id} | {uiType} {reaction.Emote} {Environment.NewLine} Slow Operation {watch.ElapsedMilliseconds}ms");
                    else Console.WriteLine($"{player._id} | {uiType} {reaction.Emote} => {watch.ElapsedMilliseconds}ms");

                }
                catch (Exception e) //Exception is not missing player
                {
                    if (!await NeitsilliaError.SpecialExceptions(e, message.Channel, player)) LogError(e);
                }
            }
            catch (Exception e)
            {
                if (!NeitsilliaError.Is(e, NeitsilliaError.NeitsilliaErrorType.CharacterDoesNotExist, out NeitsilliaError error))
                    LogError(e);
            }
        }

        void LogError(Exception e)
        {
            Log.LogS(e);
            _ = UniqueChannels.Instance.SendToLog(e, null, message.Channel);
        }
    }
}
