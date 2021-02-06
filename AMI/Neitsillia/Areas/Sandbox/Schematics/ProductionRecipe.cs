using AMI.Methods;
using AMI.Module;
using AMI.Neitsillia.Collections;
using AMI.Neitsillia.Collections.Inventories;
using AMYPrototype.Commands;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AMI.Neitsillia.Areas.Sandbox.Schematics
{
    public class ProductionRecipe
    {
        public int cost;
        public List<StackedObject<string, int>> materials;

        public double hours;

        public StackedObject<string, int> spoils;
        public int xp;

        public ProductionRecipe((string, int) spoils, int cost, int xp, double hours, params (string, int)[] mats)
        {
            this.spoils = new StackedObject<string, int>(spoils);
            this.cost = cost;
            this.xp = xp;
            this.hours = hours;

            materials = mats.Select(mat => new StackedObject<string, int>(mat)).ToList();
        }

        internal double TimeRequired(int amount) => hours * amount;

        public override string ToString() => (spoils.count > 1 ? $"{spoils.count} " : "") + spoils.item;

        internal Discord.EmbedFieldBuilder ToField(int amount)
            => DUtils.NewField(ToString(),
                $"Coins Cost: {cost * amount}" + Environment.NewLine +
                $"Time: {hours * amount} Hours" + Environment.NewLine +
                $"XP: {xp * amount}" + Environment.NewLine +
                (materials.Count > 0 ? "Materials:" + Environment.NewLine +
                materials.Join(Environment.NewLine, s => $"{(s.count * amount)}x {s.item}") : null));

        internal void Consume(Sandbox sb, int amount)
        {
            long kcost = cost * amount;
            if (sb.treasury < kcost)
                throw NeitsilliaError.ReplyError($"Treasury is missing {kcost - sb.treasury} Kutsyei Coins for this production.");

            sb.treasury -= kcost;

            string missing = null;
            Utils.Map(materials, (mats, index) =>
            {
                string mat = mats.item;
                int mamount = mats.count * amount;
                InventoryQuery iq = new InventoryQuery(sb.storage, mat);
                if (!iq.Contains(mamount, out int contains))
                    missing += Environment.NewLine + $"{mamount - contains}x {mat}";
                else if (missing == null) //Don't bother removing them if there are already errors
                    iq.Remove(mamount);

                return false;
            });

            if (missing != null)
                throw NeitsilliaError.ReplyError("Missing materials in storage: " + missing);
        }
    }
}
