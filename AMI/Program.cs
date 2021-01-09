using System;
using System.Threading.Tasks;
using AMYPrototype.Commands;
using AMI.Methods;
using System.Threading;
using Discord.WebSocket;
using Discord;
using AMI.AMIData.Servers;
using AMI.AMIData;
using System.Collections.Generic;
using AMI.Handlers;
using AMI.AMIData.Webhooks;

namespace AMYPrototype
{
    public class Program
    {
        public enum State { Booting, Ready, Exiting, Paused, Updating }
        public static State CurrentState
        {
            get;
            private set;
        }
        public static bool FirstBoot { get; private set; } = true;

        internal static bool isDev;
        internal static Tokens tokens;

        internal static Random rng = new Random(Guid.NewGuid().GetHashCode());
        internal static System.Reflection.Assembly assembly
            = System.Reflection.Assembly.GetExecutingAssembly();

        internal static DiscordSocketClient clientCopy;

        internal static ProgramData data;
        internal static DiscordBotList_Top dblAPI;

        private static BotActivityHandler botActivity;

        public static void SetState(State newState)
        {
            if (CurrentState == newState) return;

            if(newState == State.Ready)
            {
                BotActivityHandler.cycle = true;
                botActivity.CycleActivity();
            }
            else BotActivityHandler.cycle = false;

            CurrentState = newState;
        }

        private DiscordSocketClient _client;
        private CommandHandler _handler;

        Dictionary<string, Thread> threads = new Dictionary<string, Thread>();

        private static void Main(string[] args)
        {
            try
            {
                AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
                new Program().Start().GetAwaiter().GetResult();
            }
            catch (Exception e)
            {
                Log.LogS(e, "Main");
                _ = UniqueChannels.Instance.SendToLog(e);
                _ = Exit();
            }
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Log.LogS((Exception)e.ExceptionObject, "Last line");
            if (e.IsTerminating)
            {
                _ = UniqueChannels.Instance.SendToLog((Exception)e.ExceptionObject);
                _ = Exit();
            }
        }

        public Program()
        {
            Console.WriteLine(typeof(string).Assembly.ImageRuntimeVersion);
            tokens = Tokens.Load(@"./Settings/token.txt");
        }

        async Task CreateClient()
        {
            _client = new DiscordSocketClient();

            _client.Log += LogAsync;
            _client.Ready += Ready;
            _client.JoinedGuild += OnJoinedGuild;
            _client.LeftGuild += OnLeftGuild;
            _client.ReactionAdded += ReactionHandler.ReactionAddedEvent;

            await _client.LoginAsync(TokenType.Bot, tokens.discord);

            await _client.StartAsync();
            await _client.SetGameAsync("Booting...");

            clientCopy = _client;

            if(_handler == null) _handler = new CommandHandler(_client);
            else _handler.SetClient(_client);
        }

        async Task Connect()
        {
            try
            {
                await CreateClient();
            }
            catch(Exception e)
            {
                Log.LogS(e, "Connect");
                await _client.LogoutAsync();
                _ = Connect();
            }
        }

        public async Task Start()
        {
            await Connect();

            dblAPI = new DiscordBotList_Top(tokens.dbl);

            await Task.Delay(-1);
        }

        private async Task Ready()
        {
            Console.WriteLine($"{_client.CurrentUser} is connected!");

            if (FirstBoot)
            {
                isDev = _client.CurrentUser.Id == 535577053651664897;

                CheckMissingPerkEffects();

                data ??= new ProgramData();

                await new DatabaseCleaner(data.database).StartCleaning();

                await data.LoadStuff();

                await AppConnectionAndData();

                if (botActivity == null) botActivity = new BotActivityHandler(_client);
                else botActivity.SetClient(_client);

                TaskHandler.Add("Refresh", 60 * 5, AppConnectionAndData);

                _ = (tokens.platform == Tokens.Platforms.Linux ?
                    WebServer.CreateKestrelHost(isDev) :
                    WebServer.CreateHostW());
            }

            SetState(State.Ready);
            Log.LogS("Ready");
            FirstBoot = false;
        }

        private Task LogAsync(LogMessage log)
        {
            if (log.Exception != null)
                Log.LogS(log.Exception, $"LogAsync => {log.Source} {log.Message} {Environment.NewLine}");
            else Log.LogS(log.ToString());

            return Task.CompletedTask;
        }

        internal async Task AppConnectionAndData()
        {
            rng = new Random(Guid.NewGuid().GetHashCode());

            _ = dblAPI.Connect();
            dblAPI.UpdateServerCount(_client);

            clientCopy = _client;
            if (data != null && data.activity != null)
                data.activity.Save();

            await Task.Delay(1);
        }

        private Task OnJoinedGuild(SocketGuild arg)
        {
            clientCopy = _client;
            arg.Owner.SendMessageAsync($"Thank you for adding {_client.CurrentUser.Mention} to {arg.Name}", embed: new AMI.AMIData.HelpPages.Help().H_server);
            return Task.CompletedTask;
        }

        private async Task OnLeftGuild(SocketGuild arg)
        {
            await data.database.database.GetCollection<GuildSettings>("Guilds").DeleteOneAsync($"{{_id:{arg.Id}}}");
        }

        internal static async Task Exit(string message = null)
        {
            Log.LogS("Exiting");
            //Stop processes
            TaskHandler.Active = false;
            SetState(State.Exiting);

            //Inform Exit
            _ = clientCopy?.SetGameAsync(message ?? "Exiting...", type: ActivityType.Playing);

            //Tie Loose ends
            data?.activity?.Save();
            await AMI.Neitsillia.Areas.Nests.Nest.SaveNests();
            AMI.Neitsillia.NPCSystems.PopulationHandler.ReturnAll();

            _ = clientCopy?.SetStatusAsync(UserStatus.Offline);
            //Exit
            await Task.Delay(5000); // A Little 5 seconds to make sure everything had the time to complete
            try { await clientCopy?.StopAsync(); } catch(Exception) { }
            Environment.Exit(0);
        }

        internal static bool Chance(int i) 
            => i > 0 && rng.Next(101) <= i;

        internal static bool Chance(double i) 
            => i > 0 && rng.Next(100) + rng.NextDouble() <= i;

        internal static int RandomInterval(int i, double m)
            => rng.Next(NumbersM.NParse<int>(i * (1 - m)), NumbersM.NParse<int>(i * (1 + m)));


        #region Boot Verifications
        void CheckMissingPerkEffects()
        {
            string message = null;
            var methodes = typeof(AMI.Neitsillia.Items.Perks.PerkLoad.PerkLoad).GetMethods();
            var perk = new AMI.Neitsillia.Items.Perk();
            foreach (var m in methodes)
            {
                try
                {
                    Utils.GetFunction(typeof(AMI.Neitsillia.Items.Perk), m.Name);
                }
                catch (Exception e)
                {
                    message += e.Message + Environment.NewLine;
                }
            }
            Log.LogS(message);
        }
        #endregion
    }
}
