using AMI.Neitsillia.Areas.AreaExtentions;
using AMI.Neitsillia.Areas.AreaPartials;
using AMI.Neitsillia.User.PlayerPartials;
using AMI.Neitsillia.User.UserInterface;
using AMYPrototype.Commands;
using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AMI.Neitsillia.Areas.InteractiveAreas
{
    public static class TavernInteractive
    {
        internal static async Task TavernUI(Player player, ISocketMessageChannel chan)
        {
            await player.NewUI(
                await chan.SendMessageAsync(
                    embed: DUtils.BuildEmbed("Tavern",
                    "Welcome, Traveler! What service may I offer you?",
                    null, player.userSettings.Color(), 
                    DUtils.NewField("Services", 
                    $"{EUI.bounties} Bounties"
                    + $"{Environment.NewLine} {EUI.sideQuest} Quests Board"
                    + $"{Environment.NewLine} {EUI.Dice(1)} Games"
                    )
                    ).Build())
                    , MsgType.Tavern);
        }

        internal static async Task GenerateBountyFile(Player player, Area area, int i, ISocketMessageChannel chan)
        {
            if (i >= area.junctions.Count)
                i = -1;

            EmbedFieldBuilder field = i < 0 ? GetAreaBounties(area.name, area.AreaId) : null;

            if (field == null)
                i++;

            for (; i < area.junctions.Count && field == null; i++)
            {
                field = GetAreaBounties(area.junctions[i].destination, area.junctions[i].filePath);
            }
            if (field != null)
            {
                EmbedBuilder em = new EmbedBuilder()
                {
                    Title = "Bounty Board",
                    Description = "Bounty are creature who've grown in power by defeating a player or were born from another bounty."
                    + Environment.NewLine + "Hunt down the bounty by finding it in it's area while being on or above their floor.",
                };
                em.AddField(field);
                await player.NewUI(await chan.SendMessageAsync(embed: em.Build()), MsgType.BountyBoard, i.ToString());
            }
            else
                await chan.SendMessageAsync("There are no bounties on this board");
        }
        static EmbedFieldBuilder GetAreaBounties(string name, string area)
        {
            var bounties = Population.Load(Population.Type.Bounties, area);
            if (bounties == null || bounties.Count <= 0)
                return null;

            string content = null;

            foreach (var b in bounties.population)
                    content += (b.displayName);

            return content != null ?
                new EmbedFieldBuilder()
                {
                    Name = $"**{name} Bounties [{bounties.Count}]**",
                    Value = content,
                }
            : null;
        }
    }
}
