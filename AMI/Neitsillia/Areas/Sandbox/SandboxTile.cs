using AMI.Methods;
using AMI.Neitsillia.Areas.Sandbox.Schematics;
using AMI.Neitsillia.Collections;
using AMI.Neitsillia.User.PlayerPartials;
using AMI.Neitsillia.User.UserInterface;
using AMYPrototype.Commands;
using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AMI.Neitsillia.Areas.Sandbox
{
    [MongoDB.Bson.Serialization.Attributes.BsonIgnoreExtraElements]
    public class SandboxTile
    {
        public enum TileType
        {
            Warehouse, Mine, Farm
        }

        public string Name => $"{type} {NumbersM.GetLevelMark(tier)}";

        public TileType type;
        public int tier;

        public DateTime lastCollect;

        public ProductionRecipe production;
        public int amount;

        public List<string> productionOptions;
        public long xp;

        public SandboxTile(bool json) { }
        public SandboxTile(TileType type) 
        {
            this.type = type;
            tier = 0;
            productionOptions = new List<string>();
            ProductionRecipes.Add(this, 0, 0);
        }

        internal Embed ToEmbed(int maxTier = 0, Color color = default)
            => DUtils.BuildEmbed(Name,
                $"Type: {type}" + Environment.NewLine +
                $"Tier: {tier}/{maxTier}", null, color, ProductionField(), OtherControls()).Build();

        private EmbedFieldBuilder ProductionField()
        {
            string description;
            if(production == null)
                description = (productionOptions.Count == 0 ?
                "No productions available" : $"{EUI.produce} Start a production");
            else
            {
                var readyWhen = (lastCollect.AddHours(
                    production.TimeRequired(amount)) - DateTime.UtcNow);

                double hours = Math.Max(0, Math.Floor(readyWhen.TotalHours));
                double minutes = Math.Max(0, readyWhen.Minutes);
                int ready = AmountReady;
                description = $"Producing {amount}x ({production})" + Environment.NewLine +
                $"Time left: {hours:00}:{minutes:00}" + Environment.NewLine +
                (ready > 0 ? $"{EUI.storage} Collect {ready}" : $"{EUI.cancel} Cancel");
            }
            return DUtils.NewField("Production", description);
        }
            
        private EmbedFieldBuilder OtherControls()
            => DUtils.NewField("Controls", 
                $"{EUI.greaterthan} Upgrade (Unavailable)" + Environment.NewLine +
                $"{EUI.explosive} Detroy Tile");

        internal void Start(ProductionRecipe recipe, int amount)
        {
            production = recipe;
            this.amount = amount;
            lastCollect = DateTime.UtcNow;
        }

        public int AmountReady => production == null ? 0 :
            (int)Math.Floor((DateTime.UtcNow - lastCollect).TotalHours / production.hours);

        public string Collect(Sandbox sandbox)
        {
            int ready = AmountReady;
            if (ready == 0) return "Nothing to collect";
            var spoils = production.spoils;

            sandbox.storage.Add(new StackedItems(spoils.item, spoils.count * ready), -1);
            xp += production.xp * ready;
            sandbox.xp += production.xp * ready;

            amount -= ready;
            if(amount <= 0) production = null;
            return $"Collected {spoils.count * ready}x {spoils.item}";
        }

        public string Cancel(Sandbox sandbox)
        {
            sandbox.treasury += production.cost * amount;
            string result = $"Returned {production.cost * amount} Coins" + Environment.NewLine;
            Utils.Map(production.materials, (stack, i) => 
            {
                sandbox.storage.Add(new StackedItems(stack.item, stack.count * amount), -1);
                result += $"Returned {stack.count * amount}x {stack.item}" + Environment.NewLine;
                return true;
            });
            amount = 0;
            production = null;
            return result;
        }
    }
}