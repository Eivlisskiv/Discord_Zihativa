using AMI.AMIData;
using AMI.AMIData.OtherCommands;
using AMI.Methods;
using AMI.Neitsillia.User;
using AMYPrototype;
using AMYPrototype.Commands;
using Discord;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using AMI.Neitsillia.NPCSystems.Companions;
using Neitsillia.Items.Item;
using AMI.Neitsillia.Items;
using AMI.Neitsillia.User.PlayerPartials;
using AMI.Neitsillia.Areas.AreaPartials;

namespace AMI.Neitsillia.Areas.Nests
{
    class Nest
    {
        public const bool disabled = true;

        static MongoDatabase Database => Program.data.database;

        static Dictionary<string, Nest> Nests = new Dictionary<string, Nest>();

        static DateTime lastSpawn = DateTime.UtcNow;
        const int MaxHours = 10;
        const int BaseHealth = 5;

        static readonly Dictionary<string, (string race, Color color)[]> availableSpawns =
        new Dictionary<string, (string race, Color color)[]>()
        {
            { "Neitsillia\\Casdam Ilse\\Central Casdam\\Muzoisu\\Muzoisu", new[]
            {
                ("Vhoizuku", Color.DarkerGrey),
            } },
            { "Neitsillia\\Casdam Ilse\\Central Casdam\\Amethyst Gardens\\Amethyst Gardens", new[]
            {
                ("Vhoizuku", Color.DarkerGrey),
            } },
            { "Neitsillia\\Casdam Ilse\\Central Casdam\\Peresa Forest\\Peresa Forest", new[]
            {
                ("Vhoizuku", Color.DarkerGrey),
            } },
        };

        internal static Nest GetNest(string id)
        {
            Nests.TryGetValue(id, out Nest nest);
            return nest;
        }

        internal static async Task SpawnNest()
        {
            (string areaId, (string, Color)[] races) = Utils.RandomElement(availableSpawns);
            Area parent = Area.LoadArea(areaId, null, AreaPath.Table.Area);

            if (Nests.ContainsKey(parent.GeneratePath(false))) return; //There is already a nest in that area.

            (string race, Color color) = Utils.RandomElement(races);

            Area area = await CreateNest(parent, race);
            Nest nest = new Nest(area, parent);
            nest.Save();

            EmbedBuilder notice = DUtils.BuildEmbed("A nest was spotted!", $"A traveler reported seeing a nest of {race} in {parent.name}",
                null, color, DUtils.NewField("Timed Event:", "Soon, the nest will have served its purpose and we'll have more trouble to deal with. Clear the nest before that happens."
                + Environment.NewLine + $"Time Left: {Timers.CoolDownToString(nest.expire, "Hatching")} | Ends: {nest.expire.ToShortTimeString()} [UTC]"));

            await GameMasterCommands.SendToSubscribed(null, notice.Build());

            lastSpawn = DateTime.UtcNow;
        }

        internal static async Task<Area> CreateNest(Area parent, string race)
        {
            Area nest = new Area(AreaType.Nest, $"{race} Nest", parent)
            {
                junctions = new List<NeitsilliaEngine.Junction>() { new NeitsilliaEngine.Junction(parent, 0, 0)},
            };

            Utils.GetFunction(typeof(Dungeons), race + "_Dungeon").Invoke(null, new object[] { nest });

            nest.description = $"A fresh nest of {race}. Destroy it before it develops any further.";

            if(parent.junctions.Find(x => x.filePath == nest.AreaId) == null)
                parent.junctions.Add(new NeitsilliaEngine.Junction(nest, 0, 0));
            nest.SetRates(0, 100, 0, 0);
            await nest.UploadToDatabase();
            await parent.UploadToDatabase();
            return nest;
        }

        internal static async Task SaveNests()
        {
            if (Nests == null) return;
            for (int i = 0; i < Nests.Count; i++)
            {
                KeyValuePair<string, Nest> entry = Nests.ElementAt(i);
                if (!await entry.Value.VerifyNest())
                {
                    entry.Value.Save();
                    i++;
                }
            }
        }

        internal static async Task LoadNests(bool verify = false)
        {
            List<Nest> nests = await Database.LoadRecordsAsync<Nest>(null);
            for (int i = 0; i < nests.Count; i++)
            {
                var n = nests[i];
                if (!await n.VerifyNest() &&
                    n.key != null && GetNest(n.key) == null)
                { 
                    Nests.Add(n.key, n);
                    i++;
                }
            }
        }
        
        internal static async Task NestChecks()
        {
            await VerifyNests();
            if(Nests.Count < 2 && Program.Chance((DateTime.UtcNow - lastSpawn).TotalHours / 5))
                await SpawnNest();
        }

        internal static async Task VerifyNests()
        {
            for (int i = 0; i < Nests.Count; i++)
            {
                KeyValuePair<string, Nest> entry = Nests.ElementAt(i);
                if (await entry.Value.VerifyNest()) i++;
            }
        }

        internal static async Task NestInfos(Discord.WebSocket.ISocketMessageChannel chan)
        {
            EmbedBuilder e = DUtils.BuildEmbed("Active Nests", null, null, Color.DarkRed);
            foreach (KeyValuePair<string, Nest> entry in Nests)
            {
                Nest nest = entry.Value;
                e.AddField(DUtils.NewField($"{nest.name} in {nest.parentName}",
                    $"Hunters: {nest.scores.Count} | Health: {nest.healthpoints} | Time Left: {Timers.CoolDownToString(nest.expire, "Hatching")}"));
            }

            await chan.SendMessageAsync(embed: e.Build());
        }

        public string _id;
        public string key;
        public string name;
        public string parentId;
        public string parentName;
        public DateTime expire;

        public string race;

        public Dictionary<string, int> scores = new Dictionary<string, int>();
        public List<string> rewarded = new List<string>();
        public long healthpoints;

        private Nest(Area nest, Area parent, int hours = -1)
        {
            _id = nest.AreaId;
            name = nest.name;
            parentId = parent.AreaId;
            parentName = parent.name;
            key = nest.GeneratePath(false);

            hours = hours < 3 ? Program.rng.Next(MaxHours) + 3 : hours;

            expire = DateTime.UtcNow.AddHours(hours);

            healthpoints = BaseHealth * hours;

            Nests.Add(key, this);
        }

        internal void Save()
        {
            Database.UpdateRecord(null, null, this);
        }

        internal async Task Delete()
        {
            Area parent = Area.LoadArea(parentId);
            Area nest = Area.LoadArea(_id);
            int i = parent.junctions.FindIndex(x => x.filePath == _id);
            if (i != -1)
                parent.junctions.RemoveAt(i);

            await parent.UploadToDatabase();
            
            await Database.DeleteRecord<Area>("Area", _id);
            await Database.DeleteRecord<Nest>(null, _id);

            if (Nests.ContainsKey(key))
                Nests.Remove(key);
        }

        internal async Task Vicotry(Player player)
        {

            if (scores.TryGetValue(player._id, out int score)) scores[player._id]++;
            else scores.Add(player._id, 1);

            if(!rewarded.Contains(player._id) && Program.Chance(2 * score))
            {
                EmbedBuilder reward = DUtils.BuildEmbed("You've picked up something.", null, null, player.userSettings.Color());
                bool red = false;
                if(player.EggPocket != null && player.EggPocket.egg == null)
                {
                    Egg egg = Egg.Generate(0);
                    await player.EggPocket.EquippEgg(egg, player);
                    reward.Description = "You found a live Egg! The egg was placed in your Egg Pocket, use the `EggPocket` command to inspect it.";
                    red = true;
                }
                else if (Program.Chance(60)) //Item drop
                {
                    Item drop = null;
                    int amount = 1;

                    //
                    if(Program.Chance(40))
                        drop = race != null ? SkaviDrops.DropSchematic(race) : SkaviDrops.FromArea(parentName);
                    else if (Program.Chance(20))
                        drop = Item.CreateRune(player.level / 100);
                    else
                    {
                        drop = Item.CreateRepairKit(player.level / 50);
                        amount = 10;
                    }

                    //

                    int s = player.InventorySize();
                    if (drop != null && player.inventory.CanContain(drop, amount, s))
                    {
                        player.inventory.Add(drop, amount, s);
                        reward.Description = $"Collected {amount}x {drop}.";
                        red = true;
                    }
                }

                if(!red)
                {
                    player.KCoins += 25000;
                    reward.Description = "You found a large cash of Kutsyei Coins! [+ 25000 Kuts]";
                }
                await player.SendMessageToDM(embed: reward);
                rewarded.Add(player._id);
            }

            healthpoints--;

            await VerifyNest();
        }

        internal async Task<bool> VerifyNest()
        {
            if(healthpoints <= 0)
            {
                //Send All Rewards;

                EmbedBuilder embed = DUtils.BuildEmbed($"The {name} in {parentName} was exterminated.",
                    "Good job, Hunters, the nest was successfully exterminated.",
                    null, Color.Green, DUtils.NewField("Top Hunters", GetTop(5)));

                Console.WriteLine("embed built");

                if(!disabled)
                    await GameMasterCommands.SendToSubscribed("Nest Notification", embed.Build());
                await Delete();

                return false;
            }

            if(expire < DateTime.UtcNow)
            {
                EmbedBuilder embed = DUtils.BuildEmbed($"The {name} in {parentName} survived.", "We failed to exterminate the nest.",
                            null, Color.Red, DUtils.NewField("Nest Health", $"The nest survived with {healthpoints} HP. Win fights in the nest to lower its HP.")
                            ,DUtils.NewField("Top Hunters", GetTop(5))
                            );

                if (!disabled)
                    await GameMasterCommands.SendToSubscribed("Nest Notification", embed.Build());

                await Delete();

                return false;
            }

            return true;
        }

        string GetTop(int t)
        {
            if (scores == null || scores.Count == 0) return "The nest did not suffer from any attack.";
            var sorted = scores.ToList();
            sorted.Sort((x, y) => y.Value.CompareTo(x.Value));

            t = Math.Min(t, sorted.Count());

            string r = null;
            for(int i = 0; i < t; i++)
            {
                var e = sorted.ElementAt(i);
                string[] d = e.Key.Split('\\');
                r += $"<@{d[0]}> {d[1]} : {e.Value}" + Environment.NewLine;
            }

            return r;
        }
    }
}
