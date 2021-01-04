using AMI.Methods;
using AMI.Module;
using AMI.Neitsillia.Collections;
using Neitsillia.Items.Item;
using System;
using System.Reflection;

namespace AMI.Neitsillia.Areas.Strongholds
{
    class Building
    {
        public static readonly string[][] AvailableBuildingSchematics = new string[][]
        {
            new string[] //Mines
            {
                "Metal Mines"
            }
        };

        public enum BuildingType
        {
            Warehouse, Mine,
            Farm
        }
        public enum BuildingRole
        {
            Stats, //Has no products, costs hourly, affects sandbox stats
            Production, //Has products, costs or profits hourly
            Area, //An enterable area with it's own features
        }
        //
        public string Name;
        public BuildingType type;
        public BuildingRole role;
        public int Tier;
        public int MaxTier;
        //
        public ObjectInventory<string, int> availableProducts;
        public int selectedProduct;
        public long profit;
        public long cost;
        public string description;

        public DateTime Ready;
        public int HoursToReady;

        public string path;

        public SandBoxStats stats;

        internal Building(string name, BuildingType atype, BuildingRole arole)
        {
            Name = name;
            type = atype;
            role = arole;
            switch(arole)
            {
                case BuildingRole.Production:
                    availableProducts = new ObjectInventory<string, int>();
                    Ready = DateTime.UtcNow;
                    break;
                case BuildingRole.Stats:
                    stats = new SandBoxStats();
                    break;
            }
        }
        //
        internal StrongholdProduction Cycle(SandBox sandbox, 
            StrongholdProduction sp)
        {
            if (role != BuildingRole.Production || DateTime.UtcNow < Ready)
                return sp;
            StackedItems product = Produce();
            if (product != null &&
                sandbox.stock.Add(product, sandbox.stats.StorageSpace))
            {
                sp.results.Add($"{this} Produced: {product}");
                sp.KoinProfit += profit;
                sp.KoinCost += cost;
                Ready = DateTime.UtcNow.AddHours(HoursToReady);
            }
            else
                sp.results.Add($"{this}'s Produced {product} could not be collected. Storage full");
            return sp;
        }
        internal StackedItems Produce()
        {
            if (availableProducts == null || availableProducts.Count < 1
                || selectedProduct <= -1)
                return null;
            return new StackedItems(
                Item.LoadItem(
                    availableProducts[selectedProduct].item), 
                availableProducts[selectedProduct].count);
        }
        //
        public override string ToString()
        {
            return $"{Name} [{Tier}] " + 
                (role == BuildingRole.Production ? 
                User.Timers.CoolDownToString(Ready) : 
                null);
        }
        public void RefreshBuilsdingDescription()
        {
            if(role == BuildingRole.Production && selectedProduct > -1)
                description = $"Produces {availableProducts[selectedProduct].count}x" +
                    $" {availableProducts[selectedProduct].item} Per {HoursToReady} Hours";
        }

        internal static Building Load(string bn, int tier)
        {
            MethodInfo mi = Utils.GetFunction(typeof(Building),bn, true);
            string argument = null;
            //Scrap Metal:Mines
            if (mi == null)
                throw NeitsilliaError.ReplyError("Building not found");
            Building building = (Building)mi.Invoke(null, new object[] { bn, argument, tier});
            building.Name = bn;
            return building;
        }
        //-//-//-//-//-//-//-//-//-//-//-//-//-//-//-//-//-//-//-//-//-//-//-//-//-//-//-//-//-//-//
        
        public static Building Warehouse(string name, string argument, int tier) => 
        new Building(name, BuildingType.Warehouse, BuildingRole.Stats)
        {
            Tier = tier,
            MaxTier = 5,
            description = $"Increase storage size by {(tier + 1) * 10}",
            stats = new SandBoxStats()
            {
                StorageSpace = (tier + 1) * 10,
                MaximumPopulation = 0,
            }
        };
        //-//PRODUCTION//-//
        //MINES//
        public static Building MetalMines(string name, int tier)
        {
            var b = new Building(name, BuildingType.Mine, BuildingRole.Production)
            {
                Tier = tier,
                MaxTier = 5,
                HoursToReady = 1,
            };
            b.availableProducts.Add("Scrap Metal", (tier + 1) * 10);
            b.RefreshBuilsdingDescription();
            return b;
        }
        //FARMS//
        public static Building HerbFarm(string name, int tier)
        {
            var b = new Building(name, BuildingType.Farm, BuildingRole.Production)
            {
                Tier = tier,
                MaxTier = 5,
                HoursToReady = 1,
            };
            b.availableProducts.Add("Healing Herb", (tier + 1) * 5);
            b.RefreshBuilsdingDescription();
            return b;
        }
        //-//AREA//-//
    }
}
