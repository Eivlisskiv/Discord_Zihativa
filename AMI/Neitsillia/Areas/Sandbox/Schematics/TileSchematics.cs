namespace AMI.Neitsillia.Areas.Sandbox.Schematics
{
    public static class TileSchematics
    {
        private static readonly AMIData.ReflectionCache reflectionCache = new AMIData.ReflectionCache(typeof(TileSchematics));
        internal static TileSchematic GetSchem(SandboxTile.TileType type, int tier)
            => reflectionCache.Run<TileSchematic>(type.ToString(), type, tier);

        public static TileSchematic Warehouse(SandboxTile.TileType type, int nextTier)
        {
            return nextTier switch
            {
                0 => new TileSchematic(type, nextTier)
                {
                    kCost = 1000,
                    materials = new (string, int)[]
                        {
                            ("Wood", 500),
                            ("Metal Scrap", 100),
                        },
                },
                _ => new TileSchematic(type, nextTier)
                {
                    kCost = 500 * nextTier,
                    materials = new (string, int)[]
                        {
                            ("Wood", 200 * nextTier),
                            ("Metal Scrap", 100 * nextTier),
                            ("Polished Metal", 20 * nextTier),
                        },
                },
            };
        }

        public static TileSchematic Farm(SandboxTile.TileType type, int nextTier)
        {
            return nextTier switch
            {
                0 => new TileSchematic(type, nextTier)
                {
                    kCost = 1000,
                    materials = new (string, int)[]
                        {
                            ("Wood", 500),
                            ("Leather", 50),
                            ("Cloth", 100),
                            ("String", 100),
                        },
                },
                _ => new TileSchematic(type, nextTier)
                {
                    kCost = 500 * nextTier,
                    materials = new (string, int)[]
                        {
                            ("Wood", 250 * nextTier),
                            ("Leather", 25 * nextTier),
                            ("Cloth", 50 * nextTier),
                            ("String", 50 * nextTier),
                            ("Polished Metal", 5 * nextTier),
                        },
                },
            };
        }
    }
}
