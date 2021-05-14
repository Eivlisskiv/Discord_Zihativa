using System;
using System.Threading.Tasks;
using AMI.Methods;
using Discord.WebSocket;
using AMI.AMIData;
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
        public static DiscordSocketClient clientCopy => DiscordBotHandler.Client;
        public static bool FirstBoot { get; private set; } = true;

        internal static bool isDev;
        internal static Tokens tokens;

        internal static Random rng = new Random(Guid.NewGuid().GetHashCode());
        internal static System.Reflection.Assembly assembly
            = System.Reflection.Assembly.GetExecutingAssembly();

        internal static ProgramData data;
        internal static DiscordBotList_Top dblAPI;

        public static void SetState(State newState)
        {
            if (CurrentState == newState) return;

            DiscordBotHandler.bot?.SetAvtivityCycling
                (newState == State.Ready);

            CurrentState = newState;
        }

        private static void Main(string[] args)
        {
            try
            {
                AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
                AppDomain.CurrentDomain.ProcessExit += OnExit;
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

        private Program()
        {
            Console.WriteLine(typeof(string).Assembly.ImageRuntimeVersion);
            tokens = Tokens.Load(@"./Settings/token.txt");
        }

        public async Task Start()
        {
            try
            {
                await DiscordBotHandler.Connect(Ready, tokens.discord);

                dblAPI = new DiscordBotList_Top(tokens.dbl);

                await Task.Delay(-1); 
            }
            catch(Exception e)
            {
                Log.LogS(e);
            }

            await Exit();
        }

        private async Task Ready()
        {
            var client = DiscordBotHandler.Client;

            Console.WriteLine($"{client.CurrentUser} is connected!");

            if (FirstBoot)
            {
                isDev = client.CurrentUser.Id == 535577053651664897;

                CheckMissingPerkEffects();

                data ??= new ProgramData();

                await new DatabaseCleaner(data.database).StartCleaning();

                await data.LoadStuff();

                await AppConnectionAndData();

                TaskHandler.Add("Refresh", 60 * 5, AppConnectionAndData);

                _ = WebServer.CreateHostW(isDev);
            }

            SetState(State.Ready);
            Log.LogS("Ready");
            FirstBoot = false;
        }

        internal async Task AppConnectionAndData()
        {
            var client = DiscordBotHandler.Client;

            rng = new Random(Guid.NewGuid().GetHashCode());

            await dblAPI.Connect();
            dblAPI.UpdateServerCount(client);

            if (data != null && data.activity != null)
                data.activity.Save();

            await Task.Delay(1);
        }

        internal static async Task Exit(string message = null)
        {
            Log.LogS(message ?? "Exiting");
            //Stop processes
            TaskHandler.Active = false;
            SetState(State.Exiting);

            //Inform Exit
            var bot = DiscordBotHandler.bot;
            _ = bot?.SetGameAsync(message ?? "Exiting...");

            //Tie Loose ends
            data?.activity?.Save();
            await AMI.Neitsillia.Areas.Nests.Nest.SaveNests();
            AMI.Neitsillia.NPCSystems.PopulationHandler.ReturnAll();

            //Exit
            await Task.Delay(5000); // A Little 5 seconds to make sure everything had the time to complete
            await bot.Stop();
            Environment.Exit(0);
        }

        internal static void OnExit(object o, EventArgs _)
        {
            if (CurrentState != State.Exiting)
            {
                Exit("Unplanned Exit").Wait();
            }
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
