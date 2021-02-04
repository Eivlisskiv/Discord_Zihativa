using AMI.Methods;
using AMI.Module;
using AMI.Neitsillia.Collections;
using AMI.Neitsillia.Collections.Inventories;
using AMI.Neitsillia.User.UserInterface;
using System;
using System.Collections.Generic;

namespace AMI.Neitsillia.Areas.Sandbox
{
    public class TileSchematic
    {
        internal SandboxTile.TileType type;
        internal string name;
        internal int tier;
        internal long kCost;
        internal (string, int)[] materials;

        public TileSchematic(SandboxTile.TileType type, int tier)
        {
            this.type = type;
            this.tier = tier;
        }

        private void ConsumeFunds(Sandbox sb)
        {
            if (sb.treasury < kCost)
                throw NeitsilliaError.ReplyError(
                    $"Treasury is missing {kCost - sb.treasury} Kutsyei Coins");

            sb.treasury -= kCost;
        }

        internal SandboxTile Build(Sandbox sb)
        {
            ConsumeFunds(sb);
            ConsumeSchematic(sb.storage);

            return new SandboxTile(type);
        }

        public bool Upgrade(Sandbox sb, SandboxTile tile)
        {
            return false;
        }

        internal void ConsumeSchematic(Inventory inventory)
        {
            string missing = null;
            Utils.Map(materials, (mats, index) =>
            {
                (string mat, int amount) = mats;
                InventoryQuery iq = new InventoryQuery(inventory, mat);
                if (!iq.Contains(amount, out int contains))
                    missing += Environment.NewLine + $"{amount - contains}x {mat}";
                else if (missing == null) //Don't bother removing them if there are already errors
                    iq.Remove(amount);
            });

            if (missing != null)
                throw NeitsilliaError.ReplyError("Missing materials in storage: " + missing);
        }

        public override string ToString() => name ??= $"{type.ToString().Replace('_', ' ')} {NumbersM.GetLevelMark(tier)}";

        internal Discord.Embed ToEmbed(Discord.Color color = default)
        {
            return AMYPrototype.Commands.DUtils.BuildEmbed(
                ToString(), $"Kutsyei Coins cost: {kCost}" + Environment.NewLine
                + "Materials: " + Environment.NewLine + materials.Join(Environment.NewLine, m => $"{m.Item2}x {m.Item1}"),
                null, color).Build();
        }
    }
}
