using AMI.Methods;
using AMI.Module;
using AMI.Neitsillia.Areas.Sandbox.Schematics;
using AMI.Neitsillia.Collections;
using AMI.Neitsillia.User.PlayerPartials;
using AMI.Neitsillia.User.UserInterface;
using AMYPrototype.Commands;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AMI.Neitsillia.Areas.Sandbox
{
    public class Sandbox
    {
        public const int MAX_TIER_HOUSE = 3;
        public const int MAX_TIER_STRONGHOLD = 8;

        static int XP_PER_TIER => SandboxTile.XP_PER_TIER * MAX_TIER_HOUSE;

        public int tier;

        public int StorageSize => 20 + GetWarehouseSize();
        public Inventory storage;

        public long treasury;
        public long xp;

        public List<SandboxTile> tiles;

        public int UpgradeCost => (XP_PER_TIER / 5) * (tier + 1);
        

        public Sandbox()
        {
            tier = 1;
            storage = new Inventory();
            tiles = new List<SandboxTile>();
        }

        private long XPRequired => (XP_PER_TIER * (tier + 1));
        public bool CanUpgrade(int maxTier) => tier < maxTier && XPRequired <= xp;
        internal bool HasXP(out long missing) => (missing = XPRequired - xp) <= 0;

        public int GetWarehouseSize()
           => tiles.Select(tile => tile.type == SandboxTile.TileType.Warehouse ? ((tile.tier + 1) * 5) : 0).Sum();

        public SandboxTile Build(SandboxTile.TileType type)
        {
            TileSchematic ts = TileSchematics.GetSchem(type, 0);
            SandboxTile tile = ts.Build(this);
            tiles.Add(tile);

            return tile;
        }

        public SandboxTile UpgradeTile(SandboxTile tile)
        {
            TileSchematic ts = TileSchematics.GetSchem(tile.type, tile.tier + 1);
            tile.Upgrade(this, ts);
            return tile;
        }

        public void Upgrade(int maxTier)
        {
            int cost = UpgradeCost;
            if (tier >= maxTier) throw NeitsilliaError.ReplyError($"You may not upgrade beyond tier {maxTier}.");
            if(treasury < cost) throw NeitsilliaError.ReplyError($"You are missing {Utils.Display(cost - treasury)} Coins to upgrade.");
            if (!HasXP(out long missing)) throw NeitsilliaError.ReplyError($"Missing {Utils.Display(missing)} xp before upgrade is available.");

            treasury -= cost;
            xp -= XP_PER_TIER * (tier + 1);
            tier++;
        }

        public Discord.Embed ToEmbed(string source, Player player)
            => DUtils.BuildEmbed($"{player.name}'s {source}, from {player.AreaInfo.name}",
                $"{EUI.info} Commands" + Environment.NewLine +
                $"`{source} Funds {{action}} {{amount}}` {treasury} Kutsyei Coins" + Environment.NewLine +
                $"{EUI.storage} `{source} Storage {{action}}` {storage.Count}/{StorageSize}" + Environment.NewLine +
                $"{EUI.building} `{source} Build {{building name}}` {tiles.Count}/{tier}" + Environment.NewLine +
                $"{EUI.greaterthan} Upgrade " + (HasXP(out long miss) ? "Ready" : $"Missing {Utils.Display(miss)} XP"),
                null, player.userSettings.Color).Build();
    }
}
