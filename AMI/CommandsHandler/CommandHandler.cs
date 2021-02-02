using AMI.AMIData.OtherCommands;
using AMI.Commands;
using AMI.Methods;
using AMI.Module;
using AMI.AMIData.Servers;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Reflection;
using System.Threading.Tasks;
using AMI.Handlers;
using System.Collections.Generic;
using System.Collections;

namespace AMYPrototype.Commands
{
    public class CommandHandler
    {
        public static Hashtable running = new Hashtable();
        private static CommandHandler _instance;
        internal static CommandService CommandService => _instance._command;

        private DiscordSocketClient _client;
        private CommandService _command;

        //internal static bool enabled = true;

        internal static readonly string defaultPrefix = "~";

        public CommandHandler(DiscordSocketClient client)
        {
            SetClient(client);

            _command = new CommandService();

            _command.AddModulesAsync(Assembly.GetEntryAssembly(), null);

            _command.CommandExecuted += OnCommandExecutedAsync;

        }

        public void SetClient(DiscordSocketClient client)
        {
            _client = client;

            _client.MessageReceived += HandleCommandAsync;

            _instance = this;
        }

        bool MessageIsApproved(SocketUserMessage msg, string prefix, out int argPosition)
        {
            argPosition = 0;
            if (msg.HasStringPrefix($"<@!{_client.CurrentUser.Id}> ", ref argPosition))
                  return true;

            if (!(msg.Channel is IGuildChannel gc) && msg.HasCharPrefix('~', ref argPosition))
                return true;

            switch (prefix)
            {
                case "~":
                case "|":
                case "*":
                case "_":
                case "`":
                    {
                        string content = msg.Content;
                        if (content.StartsWith(prefix + prefix) && content.EndsWith(prefix + prefix))
                            return false;
                        else if (msg.HasStringPrefix(prefix, ref argPosition))
                            return true;
                    }
                    break;
                default:
                    if (msg.HasStringPrefix(prefix, ref argPosition))
                        return true;
                    break;
            }

            return false;
        }

        public static bool RunUser(ulong id)
        {
            if (!running.ContainsKey(id))
            {
                running.Add(id, DateTime.UtcNow);
                return true;
            }
            else if (((DateTime)running[id]).AddMinutes(2) < DateTime.UtcNow)
            {
                running[id] = DateTime.UtcNow;
                return true;
            }

            Log.LogS($"Waiting for command execution to end for {id}");
            return false;
        }

        private async Task HandleCommandAsync(SocketMessage s)
        {
            if (Program.CurrentState == Program.State.Booting || Program.CurrentState == Program.State.Exiting || s.Author.IsBot) return;

            GuildSettings guildSet = GuildSettings.Load(s);

            if (!(s is SocketUserMessage msg) || (guildSet != null && guildSet.Ignore)) return;

            CustomSocketCommandContext context = new CustomSocketCommandContext(_client, msg)
            { guildSettings = guildSet};

            string prefix = guildSet == null ? "" : guildSet.prefix == null || guildSet.prefix.Length < 1 ? defaultPrefix : guildSet.prefix;

            if (MessageIsApproved(msg, prefix, out int argPosition) && await GameMaster.VerifyChannel(context, guildSet))
            {
                switch (Program.CurrentState)
                {
                    case Program.State.Paused:
                        if (s.Author.Id != 201875246091993088)
                        {
                            DUtils.DeleteMessage(await context.Channel.SendMessageAsync("Server under maintenance, please refer to the support server for more information."));
                            return;
                        }
                        break;
                    case Program.State.Ready: break;
                    case Program.State.Updating:
                        DUtils.DeleteMessage(await context.Channel.SendMessageAsync("Leaving for tea break soon. [Incoming Update]"));
                        break;
                }

                if (!RunUser(s.Author.Id)) return;

                _ = context.BotUser;
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _command.ExecuteAsync(context, argPosition, null);
                    }
                    catch (Exception e)
                    {
                        Log.LogS(e);
                    }
                });

            }
        }

        public async Task OnCommandExecutedAsync(Optional<CommandInfo> command, ICommandContext ctx, IResult result)
        {
            try
            {
                CustomSocketCommandContext context = (CustomSocketCommandContext)ctx;
                if (!result.IsSuccess && command.IsSpecified)
                    await BabResult(command.Value, context, result);
                else
                {
                    if (context.guildSettings != null)
                    {
                        context.guildSettings.activityScore++;
                        context.guildSettings.SaveSettings();
                    }

                    if (Program.data != null && Program.data.activity != null)
                    {
                        Program.data.activity.Activity(ctx.User.Id);
                    }
                }
            }
            catch(Exception e)
            {
                Log.LogS(e);
            }
            
            running.Remove(ctx.User.Id);
        }

        async Task BabResult(CommandInfo method, CustomSocketCommandContext context, IResult result)
        {
            if (await CommandErrorType(method, context, result)) return;

            var exception = ((ExecuteResult)result).Exception;

            if (!NeitsilliaError.Is(exception, NeitsilliaError.NeitsilliaErrorType.CharacterDoesNotExist))
            {
                string prefix = context.Prefix;
                await context.Channel.SendMessageAsync(
                      $"Character was not found, please load a character `{prefix}load charnamehere` from your characters list `{prefix}List Characters`" +
                      $" OR create a new character `{prefix}new char charnamehere`");
            }
            else if (exception is NeitsilliaError replyerror && replyerror.ErrorType == NeitsilliaError.NeitsilliaErrorType.ReplyError)
                await context.Channel.SendMessageAsync(replyerror.ExtraMessage);

            else if (exception is Discord.Net.HttpException httpException)
            {
                switch (httpException.HttpCode)
                {
                    case System.Net.HttpStatusCode.Forbidden:
                        {
                            string requiredPerms = null;

                            var client = (IGuildUser)await context.Channel.GetUserAsync(Program.clientCopy.CurrentUser.Id);
                            var chanPerms = client.GetPermissions((IGuildChannel)context.Channel);
                            if (!chanPerms.Has(ChannelPermission.EmbedLinks))
                                requiredPerms += "Embed Links" + Environment.NewLine;
                            if (!chanPerms.Has(ChannelPermission.AddReactions))
                                requiredPerms += "Add Reactions" + Environment.NewLine;
                            if (!chanPerms.Has(ChannelPermission.ReadMessageHistory))
                                requiredPerms += "Read Message History" + Environment.NewLine;
                            if (!chanPerms.Has(ChannelPermission.AttachFiles))
                                requiredPerms += "Attach Files" + Environment.NewLine;
                            if (!chanPerms.Has(ChannelPermission.UseExternalEmojis))
                                requiredPerms += "Use External Emojis" + Environment.NewLine;

                            if (requiredPerms != null)
                                requiredPerms = " | Required Permissions: " + Environment.NewLine + requiredPerms;
                            else
                                requiredPerms = " | Unknown permission missing";

                            await context.Channel.SendMessageAsync(httpException.Reason + requiredPerms);
                        }
                        break;
                    case System.Net.HttpStatusCode.BadRequest:
                        {
                            await UniqueChannels.Instance.SendToLog(httpException.ToString());

                        }break;
                    default:
                        await context.Channel.SendMessageAsync(httpException.ToString());
                        break;
                }
            }
            else
            {
                bool log = true;
                try
                {
                    log = !await NeitsilliaError.SpecialExceptions(exception, context.Channel, context.BotUser);

                }catch(Exception e)
                {
                    string info = result.ErrorReason;
                    try { info += " ||" + e.StackTrace; }
                    catch (Exception b)
                    {
                        Log.LogS(b.Message + " => " + b.StackTrace);
                    }
                    var er = $"Exception Type: {NeitsilliaError.GetType(e)}" + Environment.NewLine +
                        $" =>  {info} ";
                    Log.LogS(er);
                    await AMI.Handlers.UniqueChannels.Instance.SendToLog(e, null, context.Channel);
                }
                if (log)
                {
                    string info = result.ErrorReason;
                    try { info += " ||" + exception.StackTrace; }
                    catch (Exception e)
                    {
                        Log.LogS(e.Message + " => " + e.StackTrace);
                    }
                    string er = $"Exception Type: {NeitsilliaError.GetType(exception)}" + Environment.NewLine +
                        $" Guild: {(context.Guild != null ? context.Guild.Id.ToString() : "DMs")} | Channel: {context.Channel.Id}" +
                        $" | ''{context.Message.Content}''  =>  {info} ";
                    Log.LogS(er);
                    await UniqueChannels.Instance.SendToLog(er);
                }
            }
        }
        
        internal static async Task<bool> CommandErrorType(CommandInfo method, CustomSocketCommandContext context, IResult result)
        {
            switch(result.Error)
            {
                case CommandError.Exception: return false;
                case CommandError.UnknownCommand: await context.Channel.SendMessageAsync("Hmm?"); return true;
                case CommandError.ParseFailed:
                case CommandError.BadArgCount:
                    {
                        await context.Channel.SendMessageAsync($"`{result.ErrorReason}` {Environment.NewLine}" +
                            $"Type `{context.Prefix}chelp {method.Name}` for more details",
                            embed:new AMI.CommandsHandler.CommandInfoEmbed(method, true).Embed);
                    }
                    return true;
                default:
                    await context.Channel.SendMessageAsync($"Muo, something went wrong. `{result.ErrorReason}`");
                    Log.LogS(result.ErrorReason);
                    return true;
            }
        }

        internal static string GetPrefix(ulong guildID)
        {
            return GuildSettings.Load(guildID)?.prefix ?? "~";
        }

        internal static string GetPrefix(ISocketMessageChannel chan)
        {
            return chan is IGuildChannel gChan ? GetPrefix(gChan.GuildId) : "";
        }

        internal static string GetPrefix(IGuild guild)
        {
            return guild == null ? "" : GetPrefix(guild.Id);
        }
    }
}

