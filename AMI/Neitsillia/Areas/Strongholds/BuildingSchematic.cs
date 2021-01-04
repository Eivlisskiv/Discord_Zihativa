using AMI.Methods;
using AMI.Module;
using AMI.Neitsillia.Collections;
using Discord;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace AMI.Neitsillia.Areas.Strongholds
{
    class BuildingSchematic
    {
        internal string Name;
        internal int tier;
        internal long kCost;
        internal List<StackedObject<string, int>> materials;

        internal string HasFunds(SandBox sb)
        {
            if (sb.treasury < kCost)
                return $"Treasury is missing {kCost - sb.treasury} Kutsyei Coins";
            foreach (StackedObject<string, int> so in materials)
            {
                int i = sb.stock.FindIndex(so.item);
                if (i <= -1)
                    return $"Stock has no {so.item}. Requires {so.count}";
                else if (sb.stock.GetCount(i) < so.count)
                    return $"Stock is missing {so.count - sb.stock.GetCount(i)}x " +
                        $"{so.item}";
            }
            return null;
        }
        internal string ConsumeSchematic(SandBox sb)
        {
            string result = null;
            if ((result = HasFunds(sb)) != null)
                return result;
            sb.treasury -= kCost;
            foreach (StackedObject<string, int> so in materials)
                sb.stock.Remove(sb.stock.FindIndex(so.item), so.count);
            return null;
        }
        internal EmbedBuilder Embed(string bn, int tier)
            => Embed(Building.Load(bn, tier));
        internal EmbedBuilder Embed(Building b)
        {
            EmbedBuilder em = new EmbedBuilder();
            em.WithTitle($"{b.Name} Tier {b.Tier}");
            em.WithDescription(b.description);
            em.AddField("Schematic", this.ToString());
            return em;
        }
        public override string ToString()
        {
            string s = kCost + " Kutsyei Coins" + Environment.NewLine;
            foreach (var mat in materials)
                s += mat.ToString() + Environment.NewLine;
            return s;
        }
        internal static BuildingSchematic GetSchem(string name, int tier)
        {
            MethodInfo mi = Utils.GetFunction(typeof(BuildingSchematic), name, true);
            if (mi == null)
                throw NeitsilliaError.ReplyError("Building Schematic not found");
            return (BuildingSchematic)mi.Invoke(null, new object[] { name, tier });
        }
        //-//-//-//-//-//-//-//-//-//-//-//-//-//-//-//-//-//-//-//-//-//-//-//-//-//-//-//-//-//-//
        //-//STATS//-//
        public static BuildingSchematic Warehouse(string name, int nextTier)
        {
            BuildingSchematic bs = null;
            switch (nextTier)
            {
                case 0:
                    bs = new BuildingSchematic()
                    {
                        kCost = 1000,
                        materials = new List<StackedObject<string, int>>()
                        {
                        new StackedObject<string, int>("Wood", 500),
                        new StackedObject<string, int>("Metal Scrap", 100),
                        },
                    }; break;
                default:
                    bs = new BuildingSchematic()
                    {
                        kCost = 500 * nextTier,
                        materials = new List<StackedObject<string, int>>()
                        {
                            new StackedObject<string, int>("Wood", 50 * nextTier),
                            new StackedObject<string, int>("Metal Scrap", 20 * nextTier),
                            new StackedObject<string, int>("Polished Metal", 10 * nextTier),
                        },
                    }; break;
            }
            bs.Name = name;
            bs.tier = nextTier;
            return bs;
        }
        //-//PRODUCTION//-//
        public static BuildingSchematic MetalMines(string name, int nextTier)
        {
            BuildingSchematic bs = null;
            if(nextTier == 0)
                bs = new BuildingSchematic()
                {
                    kCost = 5000,
                    materials = new List<StackedObject<string, int>>()
                        {
                        new StackedObject<string, int>("Wood", 200),
                        new StackedObject<string, int>("Metal Scrap", 300),
                        },
                };
            else if (nextTier < 5)
                bs = new BuildingSchematic()
                {
                    kCost = 700 * nextTier,
                    materials = new List<StackedObject<string, int>>()
                        {
                            new StackedObject<string, int>("Wood", 10 * nextTier),
                            new StackedObject<string, int>("Metal Scrap", 500 * nextTier),
                            new StackedObject<string, int>("Polished Metal", 25 * nextTier),
                        },
                };
            bs.Name = name;
            bs.tier = nextTier;
            return bs;
        }
        //-//AREA//-//
    }
}
