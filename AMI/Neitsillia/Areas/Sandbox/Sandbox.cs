using AMI.Neitsillia.Areas.Sandbox.Schematics;
using AMI.Neitsillia.Collections;
using System.Collections.Generic;
using System.Linq;

namespace AMI.Neitsillia.Areas.Sandbox
{
    public class Sandbox
    {
        const int MAX_TIER_HOUSE = 3;
        const int MAX_TIER_STRONGHOLD = 8;

        public int tier;

        public int StorageSize => 20 + GetWarehouseSize();
        public Inventory storage;

        public long treasury;
        public long xp;

        public List<SandboxTile> tiles;

        public Sandbox()
        {
            tier = 1;
            storage = new Inventory();
            tiles = new List<SandboxTile>();
        }

        public int GetWarehouseSize()
           => tiles.Select(tile => tile.type == SandboxTile.TileType.Warehouse ? ((tile.tier + 1) * 5) : 0).Sum();

        public SandboxTile Build(SandboxTile.TileType type)
        {
            TileSchematic ts = TileSchematics.GetSchem(type, 0);
            SandboxTile tile = ts.Build(this);
            tiles.Add(tile);

            return tile;
        }

        public SandboxTile Upgrade(SandboxTile tile)
        {
            TileSchematic ts = TileSchematics.GetSchem(tile.type, tile.tier + 1);
            ts.Upgrade(this, tile);
            return tile;
        }
    }
}
