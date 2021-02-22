using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AMI.Handlers
{
    class TaskHandler
    {
        public static bool Active = true; 
        static Dictionary<string, TaskHandler> tasks = new Dictionary<string, TaskHandler>();

        public static TaskHandler Add(string key, int minutesdelay, Func<Task> func)
        {
            if (tasks.TryGetValue(key, out TaskHandler t))
            {
                t.func = func;
                t.delay = minutesdelay * 60000;
            }
            else
            {
                t = new TaskHandler(key, minutesdelay, func);
                tasks.Add(key, t);
            }
            return t;
        }

        public static void Initiate()
        {
            if (!Neitsillia.Areas.Nests.Nest.DISABLED)
                Add("Nests", 15, Neitsillia.Areas.Nests.Nest.NestChecks);

            Add("ArenaFights", 60 * 48, Neitsillia.Areas.Arenas.ArenaGlobalData.RefreshAllQuests);

            Add("AdventureQuests", 720, async () => {
                Neitsillia.Adventures.Adventure.currentQuests = Neitsillia.Adventures.Adventure.GenerateNewQuests();
            });
        }

        public string name;
        public Task task;
        public int delay;
        public bool active;
        public Func<Task> func;

        public TaskHandler(string name, int delay, Func<Task> func)
        {
            this.name = name;
            this.delay = delay * 60000;
            this.func = func;
            Init();
            Start();
        }

        public void Init()
        {
            task = new Task(async () => {
                while (active && Active)
                {
                    Console.WriteLine("Active Task Handler: " + name);

                    try
                    {
                        await func();
                    }
                    catch (Exception e)
                    {
                        string s = $"Error during {name} Task";
                        Methods.Log.LogS(s);
                        Methods.Log.LogS(e);
                        _ = Handlers.UniqueChannels.Instance.SendToLog(e, s);
                    }

                    await Task.Delay(delay);
                }

                Console.WriteLine("Ended Task Handler: " + name);
            });
        }

        public void Start()
        {
            active = true;
            if (task.Status != TaskStatus.Running) task.Start();
        }


        public void Stop() => active = false;
    }
}
