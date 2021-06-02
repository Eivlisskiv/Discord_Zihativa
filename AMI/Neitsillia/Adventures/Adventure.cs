using AMI.Methods;
using AMI.Module;
using AMI.Neitsillia.Encounters;
using AMI.Neitsillia.User.PlayerPartials;
using AMI.Neitsillia.User.UserInterface;
using AMYPrototype;
using AMYPrototype.Commands;
using Discord;
using Discord.WebSocket;
using AMI.Neitsillia.Items.ItemPartials;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AMI.Neitsillia.Adventures
{
    [MongoDB.Bson.Serialization.Attributes.BsonIgnoreExtraElements]
    public class Adventure
    {
        Random Rng => Program.rng;

        public static AdventureQuest[] currentQuests = GenerateNewQuests();

        public static AdventureQuest[] GenerateNewQuests()
        {
            return new AdventureQuest[]
            {
                AdventureQuest.Generate(1),
                AdventureQuest.Generate(2),
                AdventureQuest.Generate(3),
                AdventureQuest.Generate(4),
                AdventureQuest.Generate(5),
                AdventureQuest.Generate(6),
            };
        }

        public enum Intensity { Cautious = 110, Daring = 85, Reckless = 40, Deathwish = 20 }

        const int MultiplierReference = (int)Intensity.Cautious;
        const double Percentage = 200.00;

        public static async Task SelectType(Player player, IMessageChannel chan)
        {
            EmbedBuilder embed = DUtils.BuildEmbed($"{player.name}'s Planned Adventure",
                "Adventures are automatic with no turn based combat or other player controls with the exception of aborting the adventure." +
                Environment.NewLine + " Success depends on the character's stats, ignoring abilities and perks."
                + Environment.NewLine + "**If defeated, all loot, coins and xp collected during the adventure will be lost**",
                null, player.userSettings.Color,

                DUtils.NewField($"{EUI.ok} Free Roam", 
                "Free roam adventures have no objective or time limit. The character automatically explores until recalled or defeated."),
                DUtils.NewField($"{EUI.sideQuest} Quest",
                "Adventure Quests have targeted loot and an objective time.")
                );

            await player.NewUI(await chan.SendMessageAsync(embed: embed.Build()), MsgType.Adventure);
        }

        public static async Task SelectQuest(Player player, IMessageChannel chan)
        {
            EmbedBuilder embed = DUtils.BuildEmbed($"{player.name}'s Planned Adventure",
                "Select a quest...",
                null, player.userSettings.Color, currentQuests.Select((q, i) => q.ToField(i + 1, player.Area.level)));

            await player.NewUI(await chan.SendMessageAsync(embed: embed.Build()), MsgType.Adventure, "Q");
        }

        public static async Task SelectIntensity(Player player, ISocketMessageChannel chan, int quest)
        {
            Intensity[] diffs = (Intensity[])Enum.GetValues(typeof(Intensity));
            EmbedFieldBuilder[] fields = new EmbedFieldBuilder[diffs.Length];
            for(int i = 0; i < fields.Length; i++)
            {
                fields[i] = DUtils.NewField($"{EUI.GetNum(i + 1)} {diffs[i]}",
                    $"Encounter rate: ~{(int)diffs[i]} minutes" + Environment.NewLine
                    + $"{1 + (MultiplierReference - (int)diffs[i])/Percentage}x Damage Taken"
                );
            }
            EmbedBuilder embed = DUtils.BuildEmbed($"{player.name}'s Planned Adventure",
                "Select the adventure's difficulty." +  Environment.NewLine +
                "**If defeated, all loot, coins and xp collected during the adventure will be lost**",
                null, player.userSettings.Color, fields);

            await player.EditUI("Select a difficulty", embed.Build(), chan, MsgType.Adventure, $"{quest}");
        }

        public static async Task StartAdventure(Player player, IMessageChannel chan, Intensity intensity, AdventureQuest quest = null)
        {
            player.Adventure = new Adventure(player._id, intensity, quest);
            await player.Adventure.Display(player, chan, true);
        }

        //Instance
        public string _id;

        public Intensity intensity;
        public List<string> activityStream;

        public DateTime start;
        public DateTime lastCheck;

        public AdventureQuest quest;

        public int loot;
        public long coins;
        public long xp;

        private Player player;

        public Adventure(string id, Intensity intensity, AdventureQuest quest = null)
        {
            this.quest = quest;
            _id = id; this.intensity = intensity;

            activityStream = new List<string>();
            switch(intensity)
            {
                case Intensity.Cautious: activityStream.Add(Utils.RandomElement(AutoEncounters.startCautious)); break;
                case Intensity.Daring: activityStream.Add(Utils.RandomElement(AutoEncounters.startDaring)); break;
                case Intensity.Reckless: activityStream.Add(Utils.RandomElement(AutoEncounters.startReckless)); break;
                case Intensity.Deathwish: activityStream.Add(Utils.RandomElement(AutoEncounters.startDeathwish)); break;
            }
                
            start = DateTime.UtcNow;
            lastCheck = start;
        }

        public async Task Display(Player player, IMessageChannel chan, bool edit)
        {
            this.player = player;
            var looted = SinceLastCheck(out bool ended);

            if (player.health > 0 && !ended)
                await player.EnUI(edit, null, ToEmbed(looted, true).Build(), chan, MsgType.Adventure);
            else await End(player, chan, edit);
        }

        EmbedBuilder ToEmbed((int nloot, long nxp, long koins)? looted, bool addLooted = false)
        {
            string activity = activityStream.Count > 0 ? string.Join(Environment.NewLine, activityStream) : $"...";
            string title = $"{player.name} {(quest?.Action ?? "Free Roaming")} in {player.AreaInfo.name}";
            EmbedBuilder embed =  DUtils.BuildEmbed(title,
                $"{EUI.health} {Utils.Display(player.health)}/{Utils.Display(player.Health())} | " + 
                $"{EUI.stamina} {Utils.Display(player.stamina)}/{Utils.Display(player.Stamina())}" + Environment.NewLine    
                + $"Has been adventuring for {User.Timers.CoolDownToString(DateTime.UtcNow - start)}" 
                + Environment.NewLine + quest?.TimeLeft(start)
                , null, player.userSettings.Color,
                    DUtils.NewField("Loot",
                        $"Unidentified Loot: {loot} {Plus(looted?.nloot)}" + Environment.NewLine +
                        $"Kutsyei Coins:     {coins} {Plus(looted?.koins)}" + Environment.NewLine +
                        $"Experience Points: {xp} {Plus(looted?.nxp)}", true),
                    DUtils.NewField("Quest", quest?.Info(player.Area.level) ?? "Free roaming", true),
                    DUtils.NewField("Journal", "```" + Environment.NewLine + activity + "```")
                );
            if (addLooted)
            {
                loot += looted?.nloot ?? 0;
                coins += looted?.koins ?? 0;
                xp += looted?.nxp ?? 0;
            }
            return embed;
        }

        string Plus(int? i) => i == null || i <= 0 ? null : $"+ {i}";
        string Plus(long? i) => i == null || i <= 0 ? null : $"+ {i}";

        (int nloot, long nxp, long ncoins)? SinceLastCheck(out bool ended)
        {
            DateTime lastStop = DateTime.UtcNow;
            ended = false;
            if(quest != null)
            {
                DateTime end = start.AddHours(quest.hoursTime);
                if (end < lastStop)
                {
                    lastStop = end;
                    ended = true;
                }
            }  

            double minutes = (lastStop - lastCheck).TotalMinutes;

            if (!ended && minutes < (int)intensity) return null;

            int cycles = NumbersM.FloorParse<int>(minutes / (int)intensity);

            int totalCycles = NumbersM.FloorParse<int>((lastCheck - start).TotalMinutes / (int)intensity);

            (int, long, long) looted = (0, 0, 0);
            int mult = quest?.difficulty ?? 1;
            for (int i = 0; i < cycles && player.health > 0; i++)
                activityStream.Add($"{TimeSpanString(new TimeSpan(0, (totalCycles++) * (int)intensity, 0))} {GetEncounter(mult, ref looted) ?? "..."}");

            if (player.health <= 0) activityStream.Add($"{player.name} was defeated");
            else lastCheck = lastStop;

            if (activityStream.Count > 10) activityStream.RemoveRange(0, activityStream.Count - 10);

            return looted;
        }

        string TimeSpanString(TimeSpan span)
        {
            int hours = (int)span.TotalHours;
            int minutes = (int)(span.TotalMinutes - (hours * 60));
            return $"[{(hours < 10 ? $"0{hours}" : hours.ToString())}:{(minutes < 10 ? $"0{minutes}" : minutes.ToString())}]";
        }

        public async Task End(Player player, IMessageChannel chan, bool edit)
        {
            await player.AdventureKey.Delete();
            player.AdventureKey = null;

            player.ui = null;

            if (player.health > 0)
            {
                await AdventureSuccess(player, chan);
                return;
            }

            await player.Respawn(false);
            
            await chan.SendMessageAsync($"{player.name} has fallen during their adventure, losing all loot.",
                embed: ToEmbed((-loot, -xp, -coins)).Build());
            player.SaveFileMongo();
        }

        private async Task AdventureSuccess(Player player, IMessageChannel chan)
        {
            await chan.SendMessageAsync($"{player.name}'s Adventure ended. Experience points and Coins were automatically collected");

            player.KCoins += coins;
            player.XpGain(xp);

            if (loot > 0 || quest != null)
            {
                Encounter enc = player.NewEncounter(Encounter.Names.Loot, true);

                int level = player.Area.level;
                if (quest != null) enc.loot.Add(quest.Loot(level), -1);
                for (int i = 0; i < loot; i++)
                {
                    Item item = Item.RandomItem(level * 5);
                    enc.loot.Add(item, item.type == Item.IType.Healing ? Rng.Next(3, 6) : 1, -1);
                }

                await InventoryCommands.Inventory.ViewLoot(player, chan, 0, false);
                return;
            }
            
            await GameCommands.StatsDisplay(player, chan);
        }

        string GetEncounter(int mult, ref (int l, long x, long c) looted)
        {
            if (player.stamina < Math.Max(player.Area.level / 2, 1))
            {
                player.stamina = player.Stamina();
                return Utils.RandomElement(AutoEncounters.basicResting);
            }
            else player.stamina -= Math.Max(player.Area.level / 2, 1);

            int x = Rng.Next(101);
            if (x <= player.Area.eLootRate)
            {
                looted.l += Program.Chance((mult - 1) * 5) ? 2 : 1;
                return string.Format(Utils.RandomElement(AutoEncounters.basicLoot),
                    ""
                );
            }
            else if (x <= player.Area.eLootRate + player.Area.ePassiveRate)
            {
                long nc = player.Area.level * Program.rng.Next(5, 11) * mult;
                looted.c += nc;
                return string.Format(Utils.RandomElement(AutoEncounters.coinsLoot),
                    nc
                );
            }

            int i = ArrayM.IndexWithRates(player.Area.mobs.Length, Rng);
            int k = ArrayM.IndexWithRates(player.Area.mobs[i].Count, Rng);

            int c = Rng.Next(-50, 50) + player.stats.GetSTR() + player.level - (player.Area.level*2);

            //perfect win
            if (c > 25)
            {
                looted.x += Math.Max(player.Area.level, 1) * (1 + i + k) * 10 * mult;
                return string.Format(Utils.RandomElement(AutoEncounters.perfectWin), player.Area.mobs[i][k]);
            }
            else
            {
                int dmg;
                int xpMult;
                string[] table;
                if (c > 0)//fight won
                {
                    xpMult = 5;
                    dmg = Program.RandomInterval(Math.Max(player.Area.level, 1) * Math.Max(i + k, 1) / 2, 0.25);
                    table = AutoEncounters.hurtWin;
                }
                else //fight lost
                {
                    if (Program.Chance(player.stats.GetDEX())) //Escaped
                    {
                        xpMult = 2;
                        dmg = Program.RandomInterval(Math.Max(player.Area.level, 1) * Math.Max(i + k, 1) / 2, 0.3);
                        table = AutoEncounters.escapeMob;
                    }
                    else
                    {
                        dmg = Program.RandomInterval(Math.Max(player.Area.level, 1) * Math.Max(i + k, 1), 0.5);
                        table = AutoEncounters.defeatedByMob;
                        xpMult = 0;
                    }
                }

                dmg = NumbersM.CeilParse<int>(Math.Max(dmg, 1) * (1 + (MultiplierReference - (int)intensity) / Percentage)) * mult;
                player.health -= dmg;

                looted.x += (Math.Max(player.Area.level, 1) * (1 + i + k) * xpMult * mult);
                if (Program.Chance((mult * 5) + intensity switch { 
                    Intensity.Cautious => 35,
                    Intensity.Daring => 40,
                    Intensity.Reckless => 45,
                    Intensity.Deathwish => 50,
                    _ => 0
                })) looted.l++;
                return string.Format(Utils.RandomElement(table), player.Area.mobs[i][k]) + $" (-{dmg} HP)";
            }
        }
    }
}
