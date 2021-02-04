using AMI.Methods;
using AMI.Neitsillia.Areas.AreaPartials;
using AMI.Neitsillia.Collections;
using AMI.Neitsillia.NPCSystems;
using Discord;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

namespace AMI.Neitsillia.Areas.Strongholds
{
    [BsonIgnoreExtraElements]
    public class OldSandBox
    {
        public ulong leader;
        //
        public int Tier { get; set; }
        public int size;
        //
        //public NPC Commander;
        //public List<NPC> Captains; //max 5
        //
        public Inventory stock = new Inventory();
        public long treasury;
        public long tax = 1;
        public DateTime NextTaxCollection;
        public List<Building> buildings = new List<Building>();

        public SandBoxStats stats = new SandBoxStats();
        public List<string> buildingBlueprints = new List<string>()
        { "Warehouse", "Herb Farm", "Metal Mines"};

        public OldSandBox(ulong arg, int argSize)
        {
            leader = arg;
            size = argSize;
            NextTaxCollection = DateTime.UtcNow.AddDays(1);
        }
        //public SandBox(Player arg)
        //{ leader = arg.id; lastCollect = DateTime.UtcNow; }
        public OldSandBox(){}
        //
        internal string[] Collect(Area area)
        {
            StrongholdProduction production = new StrongholdProduction();
            bool collectTax = false;
            if ((NextTaxCollection - DateTime.UtcNow).TotalMilliseconds <= 0)
            {
                collectTax = true;
                NextTaxCollection = DateTime.UtcNow.AddDays(1);
            }

            AreaExtentions.Population population = area.GetPopulation(AreaExtentions.Population.Type.Population);

            for (int i = 0; i < buildings.Count ||
                i < population.Count; i++)
            {
                if (i < buildings.Count)
                    production = buildings[i].Cycle(this, production);
                if (collectTax && i < population.Count)
                {
                    var npc = population[i];
                    long taxCollected = Verify.Max(tax, npc.KCoins);
                    production.KoinProfit += taxCollected;
                    npc.KCoins -= taxCollected;
                }
            }
            treasury += production.KoinProfit - production.KoinCost;
            return production.FinalResult();
        }
        internal int Rank()
        {
            return 0;
        }
        internal void Captured(ulong id)
        {
            throw new NotImplementedException();
        }

        internal EmbedBuilder GetEmbedInfo(Area area)
        {
            EmbedBuilder em = new EmbedBuilder();
            em.WithTitle($"Stronghold : {area.name}");
            em.WithDescription($"Owner: <@{leader}>" + Environment.NewLine
                + $"Treasury: {treasury} Kutsyei Coins" + Environment.NewLine
                + $"Storage: {stock.Count}/{stats.StorageSpace}" + Environment.NewLine
                );
            var population = area.GetPopulation(AreaExtentions.Population.Type.Population);

            if (population != null && population.Count > 0)
            {
                int[] populationByProfession = new int[Enum.GetNames(typeof(ReferenceData.Profession)).Length];
                foreach (NPC n in population.population)
                    populationByProfession[(int)n.profession]++;
                //
                string populationCount = null;
                for (int i = 0; i < populationByProfession.Length; i++)
                    if (populationByProfession[i] > 0)
                    {
                        string profesionName = Enum.GetName(typeof(ReferenceData.Profession), i);
                        populationCount += $"{populationByProfession[i]} {(populationByProfession[i] > 1 ? profesionName == "Child" ? "Children" : profesionName + "s" : profesionName)}" +
                              $" {Environment.NewLine}";
                    }
                //Create embed field
                em.AddField($"Population : {population.Count}", populationCount);
            }
            if(buildings.Count > 0)
            {
                string builds = null;
                foreach (var b in buildings)
                    builds += b.ToString() + Environment.NewLine;
                em.AddField($"Buildings : {buildings.Count}", builds);
            }
            return em;
        }

        internal string Build(string bn, int tier)
        {
            Building b = Building.Load(bn, tier);
            buildings.Add(b);
            UpdateStats();
            return $"{b.Name} Tier {b.Tier} Was Built: {b.description}";
        }

        internal string UpgradeBuilding(int index, int tier)
        {
            Building b = Building.Load(buildings[index].Name, tier);
            buildings[index] = b;
            UpdateStats();
            return $"{b.Name} Tier {b.Tier} Was Upgraded: {b.description}";
        }

        internal void UpdateStats()
        {
            stats.SetDefaults();
            foreach (Building b in buildings)
                if(b.stats != null)
                    stats.Add(b.stats);
        }

        internal EmbedBuilder SchematicsList()
        {
            EmbedBuilder em = new EmbedBuilder();
            em.WithTitle("Stronghold Schematics:");
            em.WithDescription($"example: `~sh build Warehouse` to build a warehouse");
            string list = null;
            for (int i = 0; i < buildingBlueprints.Count; i++)
                list += $"{i}|{buildingBlueprints[i]}{Environment.NewLine}";
            em.AddField("Available", list);
            return em;

        }

        internal EmbedBuilder BuildingList()
        {
            EmbedBuilder em = new EmbedBuilder();
            em.WithTitle("Stronghold Buildings:");
            //em.WithDescription($"example: ``~sh upgrade`` to upgrade building in slot 1");
            string list = null;
            for (int i = 0; i < buildings.Count; i++)
                list += $"{i+1}|{buildings[i]}{Environment.NewLine}";
            if (list == null)
                list = "No Buildings";
            em.AddField("Built Buildings", list);
            return em;

        }
    }
}
