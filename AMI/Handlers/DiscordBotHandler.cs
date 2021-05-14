using AMI.AMIData.Servers;
using AMI.Methods;
using AMYPrototype;
using AMYPrototype.Commands;
using Discord;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;

namespace AMI.Handlers
{
    public class DiscordBotHandler
    {
        
        public static DiscordBotHandler bot;
        public static DiscordSocketClient Client;

        private static Func<Task> onReady;
        private static string _token;
        private BotActivityHandler botActivity;

        public static async Task<DiscordBotHandler> Connect(Func<Task> ready, string token)
        {
            onReady = ready;
            _token = token;
            return await Connect();
        }

        private static async Task<DiscordBotHandler> Connect()
        {
            if (bot != null)
            {
                try
                {
                    await bot.Stop();
                }
                catch (Exception e)
                {
                    Log.LogS(e);
                }
            }

            bot = new DiscordBotHandler();
            await bot.Start(_token);
            return bot;
        }

        private static async Task WaitReconnect(int minutes)
        {
            Log.LogS("Watching for reconnect");
            for (int i = 0; i < minutes; i++)
            {
                await Task.Delay(60000);
                //If the bot is for some reason null
                if (bot == null)
                {
                    await Connect();
                    return;
                }

                //If it managed to recconnect within {i} minutes
                var state = bot._client.ConnectionState;
                if (state == ConnectionState.Connected || state == ConnectionState.Connecting)
                {
                    Log.LogS("Bot reconnected itself");
                    return;
                }
                Log.LogS($"{minutes - i} minutes until force reconnect.");
            }
            //Otherwise, bot was too slow to reconnect. Reset
            Log.LogS("Bot reset due to failling reconnect");

            try { await Connect(); } catch(Exception e) { Log.LogS(e); }
        }

        private readonly DiscordSocketClient _client;
        private CommandHandler _handler;
        private DiscordBotHandler()
        {
            _client = new DiscordSocketClient();

            _client.Log += LogAsync;
            _client.Ready += OnReady;
            _client.JoinedGuild += OnJoinedGuild;
            _client.LeftGuild += OnLeftGuild;
            _client.ReactionAdded += ReactionHandler.ReactionAddedEvent;

            _client.Disconnected += OnDisconnect;
        }

        public async Task Start(string token)
        {
            await _client.LoginAsync(TokenType.Bot, token);

            await _client.StartAsync();
            await _client.SetGameAsync("Booting...");

            Client = _client;

            if (_handler == null) _handler = new CommandHandler(_client);
            else _handler.SetClient(_client);
        }

        public async Task Logout()
           => await _client.LogoutAsync();

        public async Task Stop()
        {
            _client?.SetStatusAsync(UserStatus.Offline);
            await Logout();
            try { await _client?.StopAsync(); } catch (Exception) { }
        }

        public void SetAvtivityCycling(bool set)
        {
            BotActivityHandler.cycle = set;
            if (set) botActivity.CycleActivity();
        }

        public async Task SetGameAsync(string name, ActivityType type = ActivityType.Playing)
           => await _client?.SetGameAsync(name, type: type);

        private async Task OnReady()
        {
            if (botActivity == null) botActivity = new BotActivityHandler(_client);
            else botActivity.SetClient(_client);

            await onReady();
        }

        private async Task OnJoinedGuild(SocketGuild guild)
        {
            await guild.Owner.SendMessageAsync("Thank you for adding " +
                $"{_client.CurrentUser.Mention} to {guild.Name}. " +
                $"We greatly suggest that you visit our support server for help: " +
                await AMIData.OtherCommands.Other.GetSupportInvite(), 
                embed: new AMIData.HelpPages.Help().H_server);
        }

        private async Task OnLeftGuild(SocketGuild arg)
        {
            await Program.data.database.database.GetCollection<GuildSettings>
                ("Guilds").DeleteOneAsync($"{{_id:{arg.Id}}}");
        }

        private Task LogAsync(LogMessage log)
        {
            if (log.Exception != null)
                Log.LogS(log.Exception, "LogAsync => " +
                    $"{log.Source} {log.Message} {Environment.NewLine}");
            else Log.LogS($"{log.Source}: {log.Message}");

            //if(reconnect && log.Message == "Disconnecting")
            //_ = WaitReconnect(5);

            return Task.CompletedTask;
        }

        private Task OnDisconnect(Exception e)
        {
            Log.LogS(e);
            return Task.CompletedTask;
        }
    }
}
