﻿using AMI.Methods;
using AMI.Neitsillia.Areas.Sandbox.Schematics;
using AMI.Neitsillia.Collections;
using AMI.Neitsillia.User.UserInterface;
using AMYPrototype;
using AMYPrototype.Commands;
using Discord;
using System;
using System.Collections.Generic;

namespace AMI.Neitsillia.Areas.Sandbox
{
    [MongoDB.Bson.Serialization.Attributes.BsonIgnoreExtraElements]
    public class SandboxTile
    {
        public enum TileType
        {
            Warehouse, Farm, Mine
        }

        public const int XP_PER_TIER = 7179;
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
                $"Tier: {tier}/{maxTier}" + Environment.NewLine +
                (HasXP(out long missing) ? "Upgrade Ready" : 
                $"{Utils.Display(missing)} XP left until next upgrade"), 
                null, color, ProductionField(), OtherControls()).Build();

        internal void Upgrade(Sandbox sandbox, TileSchematic ts)
        {
            ts.Upgrade(sandbox, this);
            xp -= XP_PER_TIER * (tier + 1);
            tier++;
        }

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
                (ready > 0 ? $"{EUI.collect} Collect {ready}" : $"{EUI.cancel} Cancel");
            }
            return DUtils.NewField("Production", description);
        }
            
        private EmbedFieldBuilder OtherControls()
            => DUtils.NewField("Controls", 
                $"{EUI.greaterthan} Upgrade" + Environment.NewLine +
                $"{EUI.explosive} Detroy Tile");

        internal void Start(ProductionRecipe recipe, int amount)
        {
            production = recipe;
            this.amount = amount;
            lastCollect = DateTime.UtcNow;
        }

        public int AmountReady => production == null ? 0 : Math.Min(amount, 
            (int)Math.Floor((DateTime.UtcNow - lastCollect).TotalHours / production.hours));

        public string Collect(Sandbox sandbox)
        {
            int ready = AmountReady;
            if (ready == 0) return "Nothing to collect";
            var spoils = production.spoils;

            if(spoils.count > 0)
                sandbox.storage.Add(new StackedItems(spoils.item, spoils.count * ready), -1);
            xp += production.xp * ready;
            sandbox.xp += production.xp * ready;

            amount -= ready;
            
            lastCollect = lastCollect.AddHours(ready * production.hours);
            if(amount <= 0) production = null;
            
            return $"Collected {spoils.count * ready}x {spoils.item} {GainProduction(ready)}";
        }

        string GainProduction(int chances)
        {
            string s = null;
            while(chances > 0)
            {
                if (Program.Chance(chances))
                {
                    string newRecipe = ProductionRecipes.AddRandom(this);
                    if (newRecipe != null)
                        s += Environment.NewLine + $"New production available: {newRecipe}";
                }
                chances -= 100;
            }
            return s;
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

        internal bool HasXP(out long missing) => (missing = (XP_PER_TIER * (tier + 1)) - xp) <= 0;
    }
}