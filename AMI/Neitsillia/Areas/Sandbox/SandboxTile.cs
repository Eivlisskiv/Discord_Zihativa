using AMI.Methods;
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
    public class SandboxTile
    {
        public enum TileType
        {
            Warehouse, Mine, Farm
        }

        public string Name => $"{type} {NumbersM.GetLevelMark(tier)}";

        public TileType type;
        public int tier;

        public DateTime readyWhen;
        public string production;
        public int amount;

        public List<string> productionOptions;

        public SandboxTile(bool json) { }
        public SandboxTile(TileType type) 
        {
            this.type = type;
            tier = 1;
            productionOptions = new List<string>();
        }

        internal Embed ToEmbed(int maxTier = 0, Color color = default)
            => DUtils.BuildEmbed(Name,
                $"Type: {type}" + Environment.NewLine +
                $"Tier: {tier}/{maxTier}", null, color, ProductionField()).Build();

        private EmbedFieldBuilder ProductionField()
            => DUtils.NewField("Production",
                production == null ? (productionOptions.Count == 0 ? 
                "{How to add production options}" : productionOptions.Join(
                    Environment.NewLine, (s, i) => $"{EUI.GetNum(i + 1)} {s}")) :
                $"Producing {amount}x {production}" + Environment.NewLine + 
                $"Time left: {(readyWhen - DateTime.UtcNow):hh:mm:ss}");
    }
}