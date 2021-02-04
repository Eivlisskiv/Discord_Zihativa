using AMI.Methods;
using AMI.Module;
using AMI.Neitsillia.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace AMI.Neitsillia.Areas.Sandbox
{
    public static class TileSchematics
    {        
        internal static TileSchematic GetSchem(SandboxTile.TileType type, int tier)
        {
            MethodInfo mi = Utils.GetFunction(typeof(TileSchematics), type.ToString(), true);
            if (mi == null)
                throw NeitsilliaError.ReplyError("Building Schematic not found");
            TileSchematic schem = (TileSchematic)mi.Invoke(null, new object[] { type, tier });
            return schem;
        }

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
                            ("Wood", 50 * nextTier),
                            ("Metal Scrap", 20 * nextTier),
                            ("Polished Metal", 10 * nextTier),
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
                            ("Metal Scrap", 100),
                        },
                },
                _ => new TileSchematic(type, nextTier)
                {
                    kCost = 500 * nextTier,
                    materials = new (string, int)[]
                        {
                            ("Wood", 50 * nextTier),
                            ("Metal Scrap", 20 * nextTier),
                            ("Polished Metal", 10 * nextTier),
                        },
                },
            };
        }
    }
}
