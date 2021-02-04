using AMI.Neitsillia.Collections;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;

namespace AMI.Neitsillia.Areas.Sandbox
{
    public class Sandbox
    {
        const int MAX_TIER_HOUSE = 3;
        const int MAX_TIER_STRONGHOLD = 8;

        public int tier;

        public int StorageSize => (tier + 1) * 20;
        public Inventory storage;

        public long treasury;

        public List<SandboxTile> tiles;

        public Sandbox()
        {
            tier = 0;
            storage = new Inventory();
            tiles = new List<SandboxTile>();
        }

        public SandboxTile Build(SandboxTile.TileType type)
        {
            TileSchematic ts = TileSchematics.GetSchem(type, 0);
            SandboxTile tile = ts.Build(this);
            tiles.Add(tile);

            return tile;
        }
    }
}
