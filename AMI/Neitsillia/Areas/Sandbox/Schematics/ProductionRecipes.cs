using AMI.Methods;
using System.Collections.Generic;

namespace AMI.Neitsillia.Areas.Sandbox.Schematics
{
    public static class ProductionRecipes
    {
        private static readonly AMIData.ReflectionCache reflectionCache = new AMIData.ReflectionCache(typeof(ProductionRecipes));

        public static void AddRandom(SandboxTile tile)
        {
            string[][] recipes = reflectionCache.GetValue<string[][]>(tile.type.ToString());

            string[] tierRecipes = recipes[tile.tier];
            tile.productionOptions.Add(Utils.RandomElement(tierRecipes));
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

        public static readonly Dictionary<string, ProductionRecipe> Farm_Recipes = new Dictionary<string, ProductionRecipe>()
        {
             { "Healing Herb", new ProductionRecipe(("Healing Herb", 3), 10, 1, 0.5, ("Healing Herb", 1)) },
        };

        public static readonly string[][] Farm =
        {
             new string[] { "Healing Herb" }
        };
    }
}
