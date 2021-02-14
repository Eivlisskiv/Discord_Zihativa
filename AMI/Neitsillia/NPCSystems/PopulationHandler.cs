using AMI.Methods;
using AMI.Neitsillia.Areas.AreaExtentions;
using AMI.Neitsillia.Areas.AreaPartials;
using AMYPrototype;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace AMI.Neitsillia.NPCSystems
{
    class PopulationHandler
    {
        public static bool Active = true;
        static int _perSecond = 300;

        static Dictionary<string, PopulationHandler> populations =
            new Dictionary<string, PopulationHandler>();

        static Dictionary<string, PopulationHandler> bounties =
            new Dictionary<string, PopulationHandler>();

        static IDisposable task;
        static int populationCycle;
        static int bountyCycle;

        public static int SetPerSecond(int i) => (_perSecond = Math.Max(300, i));

        public static void Start()
        {
            if (task == null) task = Observable.Interval(TimeSpan.FromSeconds(300)).Subscribe(async x => await IntervalRun());
                   //: Task.Run(async () => await TaskRun());
        }

        public static async Task TaskRun()
        {
            while(Active)
            {
                try
                {
                    await HandlePopulation();

                    await Task.Delay(_perSecond * 1000);

                    await HandlerBounty();

                    await Task.Delay(_perSecond * 1000);
                }
                catch (Exception e)
                {
                    _ = Handlers.UniqueChannels.Instance.SendToLog(e, "Population Handler \n");
                }
            }
        }

        public static async Task IntervalRun()
        {
            try
            {
                await HandlePopulation();
                await HandlerBounty();
            }
            catch (Exception e)
            {
                _ = Handlers.UniqueChannels.Instance.SendToLog(e, "Population Handler \n");
            }
        }

        public static async Task HandlePopulation()
        {
            if (populations.Count > 0)
            {
                if (populationCycle >= populations.Count) populationCycle = 0;

                await populations.ElementAt(populationCycle).Value.ActOne();

                populationCycle++;
            }
        }

        public static async Task HandlerBounty()
        {
            if (bounties.Count > 0)
            {
                if (bountyCycle >= bounties.Count) bountyCycle = 0;

                await bounties.ElementAt(bountyCycle).Value.ActOne();

                bountyCycle++;
            }
        }

        public static void ReturnAll()
        {
            foreach(var kv in populations)
                kv.Value.ReturnPopulation();
            foreach (var kv in bounties)
                kv.Value.ReturnPopulation();
        }

        static string VerifyAreaId(Area area) =>
            (area.type == Areas.AreaType.Dungeon || area.type == Areas.AreaType.Arena || area.type == Areas.AreaType.Nest) ?
            area.GeneratePath(false) + area.parent : area.AreaId;

        public static void Add(Area area, NPC npc) => AddPriv(VerifyAreaId(area), npc);

        public static void Add(string id, NPC npc) => AddPriv(VerifyAreaId(Area.LoadArea(id)), npc);    

        static void AddPriv(string area, NPC npc)
        {
            Population.Type type = npc.profession == ReferenceData.Profession.Creature ? Population.Type.Bounties : Population.Type.Population;

            var dict = type == Population.Type.Bounties ? bounties : populations;

            if (dict.ContainsKey(area)) dict[area].Add(npc);
            else dict.Add(area, new PopulationHandler(area, type, npc));
        }

        //Instance

        readonly string areaId;
        readonly Population.Type type;

        List<NPC> population = new List<NPC>();

        private PopulationHandler(string id, Population.Type t, NPC npc)
        {
            areaId = id;
            type = t;

            population = new List<NPC>(new[] {npc});
        }

        void Add(NPC n) => population.Add(n);

        void ReturnPopulation()
        {
            Population popu = Population.Load(type, areaId);

            popu.population.AddRange(population);

            popu.Save();

            population.Clear();
        }

        async Task ActOne()
        {
            if (population.Count == 0) return;

            int i = Program.rng.Next(population.Count);

            Area area = Area.LoadArea(areaId);

            if (area == null)
            {
                if (population[i].profession != ReferenceData.Profession.Creature)
                    population[i].Respawn();
                population.RemoveAt(i);
                return;
            }

            if (!await population[i].Act(area, Program.isDev ? _perSecond * 60 : _perSecond))
                population.RemoveAt(i);
            else
            {
                var areaPopulation = Population.Load(type, areaId);
                if (IsReturnToArea(population[i], areaPopulation.Count, area.level))
                {
                    await Handlers.UniqueChannels.Instance.SendMessage("Population", $"[{DateTime.UtcNow.TimeOfDay:hh\\:mm}] **{population[i].displayName}** is now available in {area.name}");
                    areaPopulation.Add(population[i]);
                    population.RemoveAt(i);
                }
            }
        }

        bool IsReturnToArea(NPC n, int currentCount, int level)
         =>  type switch
            {
                Population.Type.Population => Program.Chance(population.Count + (30 - currentCount)),
                Population.Type.Bounties => n.level >= level,
                _ => true,
            };
    }
}
