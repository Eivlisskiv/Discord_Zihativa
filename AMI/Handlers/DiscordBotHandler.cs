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
        public static DiscordSocketClient Client;
        public static DiscordBotHandler bot;

        private static Func<Task> onReady;
        private static string _token;

        public static async Task<DiscordBotHandler> Connect(Func<Task> ready, string token)
        {
            onReady ??= ready;
            _token ??= token;
            if(bot != null)
            {
                try
                {
                    await bot.Stop();

                }catch(Exception e)
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
            for(int i = 0; i < minutes; i++)
            {
                await Task.Delay(60000);
                //If the bot is for some reason null
                if (bot == null)
                {
                    await Connect(null, null);
                    return;
                }

                //If it managed to recconnect within {i} minutes
                var state = bot._client.ConnectionState;
                if (state == ConnectionState.Connected || state == ConnectionState.Connecting)
                    return;
            }
            //Otherwise, bot was too slow to reconnect. Reset
            Log.LogS("Bot reset due to failling reconnect");
            await Connect(null, null);
        }

        private readonly DiscordSocketClient _client;
        private CommandHandler _handler;
        private DiscordBotHandler()
        {
            _client = new DiscordSocketClient();

            _client.Log += LogAsync;
            _client.Ready += onReady;
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

        public async Task SetGameAsync(string name, ActivityType type = ActivityType.Playing)
           => await _client?.SetGameAsync(name, type: type);

        private async Task OnJoinedGuild(SocketGuild guild)
        {
            await guild.Owner.SendMessageAsync("Thank you for adding " +
                $"{_client.CurrentUser.Mention} to {guild.Name}", 
                embed: new AMI.AMIData.HelpPages.Help().H_server);
        }

        private async Task OnLeftGuild(SocketGuild arg)
        {
            await Program.data.database.database.GetCollection<GuildSettings>
                ("Guilds").DeleteOneAsync($"{{_id:{arg.Id}}}");
        }

        private async Task LogAsync(LogMessage log)
        {
            if (log.Exception != null)
                Log.LogS(log.Exception, "LogAsync => " +
                    $"{log.Source} {log.Message} {Environment.NewLine}");
            else Log.LogS(log.ToString());
        }

        private async Task OnDisconnect(Exception e)
        {
            Log.LogS(e);
            _ = WaitReconnect(5);
        }
    }
}
