using AMI.Methods;
using AMI.Module;
using AMI.Neitsillia.Areas;
using AMI.Neitsillia.NeitsilliaCommands;
using AMI.Neitsillia.NPCSystems;
using AMI.Neitsillia.User.PlayerPartials;
using AMI.Neitsillia.User.UserInterface;
using AMYPrototype.Commands;
using Discord;
using Discord.Commands;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

namespace AMI.Neitsillia.Commands
{
	public partial class Areas
    {
        const int HP_SECONDS_PER_PERCENT = 216;
        const int SP_SECONDS_PER_PERCENT = 154;

        [Command("Rest")]
        [Alias("Sleep")]
        [Summary("Rest to regain health and stamina over time.")]
        public async Task Rest()
            => await RestStat(Context.GetPlayer(Player.IgnoreException.Resting), Context.Channel);
        internal static async Task RestStat(Player player, IMessageChannel chan)
        {
            if (player.IsEncounter("Combat"))
                DUtils.DeleteMessage(await chan.SendMessageAsync("You may not rest while in combat"));
            else if (player.Party != null && player.Party.GetLeaderID() != player.userid)
                DUtils.DeleteMessage(await chan.SendMessageAsync("Only the leader can initiate or end party rests."));
            else
            {
                EmbedBuilder rest = new EmbedBuilder();
                if (player.IsResting)
                {
                    DateTime restDateTime = player.userTimers.restTime;
                    if ((player.ui != null && (player.ui.type == MsgType.Rest || player.ui.type == MsgType.EndRest)
                        && player.ui.data != null && player.ui.data != ""))
                    {
                        string jsonTime = player.ui.data;
                        restDateTime = JsonConvert.DeserializeObject<DateTime>(jsonTime);
                    }
                    TimeSpan restTime = (DateTime.UtcNow - restDateTime);
                    rest.Title = $"{player.Party?.partyName ?? player.name} Resting in {player.Area.name}";
                    rest.WithDescription($"Rest Time: {restTime.Hours}:{restTime.Minutes}:{restTime.Seconds} {Environment.NewLine}" +
                        $"End Rest?");
                    await player.NewUI(await chan.SendMessageAsync(embed: rest.Build()), MsgType.EndRest);
                }
                else
                {
                    if (player.Area.type == AreaType.Arena)
                    {
                        await chan.SendMessageAsync("You may not rest in this location type: " + player.Area.type);
                        return;
                    }

                    rest.Title = $"{player.Party?.partyName ?? player.name} Started Resting in {player.Area.name}";

                    rest.WithDescription("Attempting to use actions commands with this character will result in requesting to stop resting");

                    if (player.Party != null)
                    {
                        foreach (PartyMember m in player.Party.members)
                        {
                            Player p = m.id == player.userid ? player : m.LoadPlayer();

                            p.userTimers.restTime = DateTime.UtcNow;

                            p.SaveFileMongo();

                            rest.AddField(PlayerRestField(p));
                        }
                        foreach (NPCSystems.NPC n in player.Party.NPCMembers)
                        {
                            rest.AddField(PlayerRestField(n));
                        }
                    }
                    else
                    {
                        player.userTimers.restTime = DateTime.UtcNow;

                        rest.AddField(PlayerRestField(player));
                    }

                    await player.NewUI(await chan.SendMessageAsync(embed: rest.Build()), MsgType.Rest);
                }
            }
        }

        private static EmbedFieldBuilder PlayerRestField(CharacterMotherClass player)
            => DUtils.NewField(player.name, $"Full health in {RestoreTimeLeft(player, true)}"
                + Environment.NewLine + $"Fill stamina in {RestoreTimeLeft(player, false)}", true);

        private static string RestoreTimeLeft(CharacterMotherClass player, bool isHP)
        {
            double restores = RestorePerSeconds(player.stats.GetDEX(), isHP);
            double totalSeconds;
            if (isHP) 
            {
                long mhp = player.Health();
                double hpps = mhp * restores;
                totalSeconds = (mhp - player.health) / hpps;
            }
            else
            {
                int shp = player.Stamina();
                double spps = shp * restores;
                totalSeconds = (shp - player.stamina) / spps;
            }

            double vagueMinutes = totalSeconds / 60.00;
            double hours = Math.Floor(vagueMinutes / 60);
            double minutes = Math.Floor(vagueMinutes - (hours * 60));
            return (hours > 0 ? $"{hours}h " : "") +
                (minutes > 0 ? $"{minutes}m " : "") +
                $"{Math.Floor(totalSeconds - (Math.Floor(vagueMinutes) * 60))}s ";
        }

        private static double RestorePerSeconds(int dex, bool isHP)
            => (0.01 + (dex * Collections.Stats.restSpeed)) / (isHP ? HP_SECONDS_PER_PERCENT : SP_SECONDS_PER_PERCENT);

        internal static async Task EndRest(Player player, IMessageChannel chan)
        {
            if (player.IsResting)
            {
                EmbedBuilder rest = new EmbedBuilder();
                DateTime restDateTime = player.userTimers.restTime;
                if ((player.ui != null && (player.ui.type == MsgType.Rest || player.ui.type == MsgType.EndRest)
                && player.ui.data != null && player.ui.data != ""))
                {
                    string jsonTime = player.ui.data;
                    restDateTime = JsonConvert.DeserializeObject<DateTime>(jsonTime);
                }
                TimeSpan restTime = (DateTime.UtcNow - restDateTime);

                if (player.Party != null)
                {
                    foreach (PartyMember m in player.Party.members)
                    {
                        Player p = m.id == player.userid ? player : m.LoadPlayer();
                        rest.AddField(RestRecover(p, restTime.TotalSeconds));

                        p.userTimers.EndRest();
                        p.SaveFileMongo();
                    }
                    foreach (NPCSystems.NPC n in player.Party.NPCMembers)
                    {
                        rest.AddField(RestRecover(n, restTime.TotalSeconds));
                    }

                    await player.Party.SaveData();
                }
                else
                {
                    rest.AddField(RestRecover(player, restTime.TotalSeconds));
                    player.userTimers.EndRest();
                }

                await player.NewUI(await chan.SendMessageAsync(embed: rest.Build()), MsgType.Main);
            }
            else await GameCommands.ShortStatsDisplay(player, chan);
        }

        private static EmbedFieldBuilder RestRecover(CharacterMotherClass character, double totalSeconds)
        {
            long maxHealth = character.Health();
            int maxStamina = character.Stamina();

            int dex = character.stats.GetDEX();

            double hpps = RestorePerSeconds(dex, true);
            long hpRestored = Math.Min(maxHealth - character.health, 
                NumbersM.FloorParse<long>(maxHealth * hpps * totalSeconds));
            character.health += hpRestored;

            double spps = RestorePerSeconds(dex, false);
            int spRestored = Math.Min(maxStamina - character.stamina,
                NumbersM.FloorParse<int>(maxStamina * spps * totalSeconds));
            character.stamina += spRestored;

            return DUtils.NewField(character.name, 
                $"Restored {hpRestored} Health and {spRestored} Stamina", true);
        }
    }
}
