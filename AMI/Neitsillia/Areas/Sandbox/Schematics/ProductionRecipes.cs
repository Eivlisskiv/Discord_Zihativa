using AMI.Methods;
using System;
using System.Collections.Generic;

namespace AMI.Neitsillia.Areas.Sandbox.Schematics
{
    public static class ProductionRecipes
    {
        private static readonly AMIData.ReflectionCache reflectionCache = new AMIData.ReflectionCache(typeof(ProductionRecipes));

        static Random Rng => AMYPrototype.Program.rng;

        public static string AddRandom(SandboxTile tile)
        {
            string[][] recipes = reflectionCache.GetValue<string[][]>(tile.type.ToString());

            string[] tierRecipes = recipes[Math.Min(Rng.Next(0, tile.tier), recipes.Length - 1)];
            string s = Utils.RandomElement(tierRecipes);

            if (tile.productionOptions.Contains(s)) return null;
            tile.productionOptions.Add(s);
            return s;
        }

        public static void Add(SandboxTile tile, int tier, int index)
        {
            string[][] recipes = reflectionCache.GetValue<string[][]>(tile.type.ToString());

            if(recipes.Length > tier)
            {
                string[] tierRecipes = recipes[tier];
                if (tierRecipes.Length > index)
                    tile.productionOptions.Add(tierRecipes[index]);
            }
        }

        public static ProductionRecipe Get(SandboxTile.TileType type, string name)
        {
            Dictionary<string, ProductionRecipe> recipes = reflectionCache.GetValue<
                Dictionary<string, ProductionRecipe>>($"{type}_Recipes");
            return recipes[name];
        }

        public static readonly string[][] Farm =
        {
             new string[] { "Farm Healing Herbs", "Produce Vhochait", "Farm Amanita Velosa" },
             new string[] { "Farm Amanita Caesarea" },
             new string[] { "Farm Amanita Hemibapha" },
        };

        public static readonly Dictionary<string, ProductionRecipe> Farm_Recipes = new Dictionary<string, ProductionRecipe>()
        {
            { "Farm Healing Herbs", new ProductionRecipe(("Healing Herb", 3), 10, 1, 1, ("Healing Herb", 1)) },
            { "Farm Amanita Velosa", new ProductionRecipe(("Amanita Velosa", 2), 60, 10, 1, ("Amanita Velosa", 1)) },

            { "Produce Vhochait", new ProductionRecipe(("Vhochait", 1), 50, 10, 0.25, ("Healing Herb", 2), ("Goq Blood", 1)) },

            { "Farm Amanita Caesarea", new ProductionRecipe(("Amanita Caesarea", 2), 120, 50, 1, ("Amanita Caesarea", 1)) },
            { "Farm Amanita Hemibapha", new ProductionRecipe(("Amanita Hemibapha", 2), 240, 100, 1, ("Amanita Hemibapha", 1)) },

             
        };



        public static readonly string[][] Warehouse =
        {
             new string[] { "General Work" }
        };

        public static readonly Dictionary<string, ProductionRecipe> Warehouse_Recipes = new Dictionary<string, ProductionRecipe>()
        {
             { "General Work", new ProductionRecipe(("General Work", 0), 100, 100, 1) },
        };
     
    }
}
