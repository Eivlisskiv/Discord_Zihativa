﻿using AMI.AMIData.Servers;
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
        public static void LogReactionError(UI ui, Exception e, IEmote emote, IMessageChannel chan)
        {
            Log.LogS(e);
            _ = UniqueChannels.Instance.SendToLog(e, $"ReactionAdded type {ui?.type}, emote: {emote}, data: {ui.data ?? "null"}", chan);
        }

        public static Task ReactionAddedEvent(Cacheable<IUserMessage, ulong> cachedMessage,
            Cacheable<IMessageChannel, ulong> cachedChannel, SocketReaction reaction)
        {
            IMessageChannel channel = null;
            Task.Run(async () =>
            {
                try
                {

                    if (Program.CurrentState != Program.State.Ready) return;

                    var user = reaction.User.IsSpecified ? reaction.User.Value 
                    : await Program.clientCopy.GetUserAsync(reaction.UserId);
                    if(user?.IsBot ?? true) return;

                    IUserMessage message = cachedMessage.Value ?? (reaction.Message.IsSpecified ? reaction.Message.Value : await cachedMessage.GetOrDownloadAsync());

                    //Currently, the bot does not need to check reactions on messages that are not its own
                    if (message == null || message.Author.Id != Program.clientCopy.CurrentUser.Id) return;

                    channel = cachedChannel.Value ?? reaction.Channel ?? message.Channel;

                    GuildSettings gset = channel is IGuildChannel chan ?
                        gset = GuildSettings.Load(chan.Guild) : null;

                    if (gset != null && gset.Ignore) return;

                    if (!CommandHandler.RunUser(reaction.UserId)) return;

                    ReactionHandler handler = new ReactionHandler(message, reaction, gset);
                    if (!await handler.IsUserUI()) await handler.PlayerUI();
                }
                catch (Exception e)
                {
                    Log.LogS(e);
                    if(channel != null)
                        _ = UniqueChannels.Instance.SendToLog(e, "ReactionAdded Error", channel);
                }

                CommandHandler.running.Remove(reaction.UserId);
            });
            
            return Task.CompletedTask;
        }

        readonly BotUser botUser;
        readonly SocketReaction reaction;

        readonly IUserMessage message;

        readonly GuildSettings guildSettings;

        public ReactionHandler(IUserMessage message, SocketReaction reaction, GuildSettings gset)
        {
            botUser = BotUser.Load(reaction.UserId);
            this.reaction = reaction;
            this.message = message;
            guildSettings = gset;
        }

		public async Task<bool> IsUserUI()
        {
            if (botUser?.ui?.msgId != reaction.MessageId) return false;

            try
            {
                await botUser.ui.Click(reaction, message, null);
            }
            catch(Exception e)
            {
                if (!await NeitsilliaError.SpecialExceptions(e, message.Channel, botUser)) LogReactionError(botUser.ui, e, reaction.Emote, message.Channel);
            }
            

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
                }
                catch (Exception e) //Exception is not missing player
                {
                    if (!await NeitsilliaError.SpecialExceptions(e, message.Channel, player)) LogReactionError(player.ui, e, reaction.Emote, message.Channel);
                }
            }
            catch (Exception e)
            {
                if (!NeitsilliaError.Is(e, NeitsilliaError.NeitsilliaErrorType.CharacterDoesNotExist, out _))
                    LogReactionError(botUser.ui, e, reaction.Emote, message.Channel);
            }
        }
    }
}
