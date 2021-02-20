using AMI.AMIData;
using AMI.Methods;
using AMI.Neitsillia.Encounters;
using AMI.Neitsillia.Items.Quests;
using AMI.Neitsillia.NPCSystems;
using AMI.Neitsillia.User.PlayerPartials;
using AMYPrototype;
using Discord;
using Neitsillia.Items.Item;
using System;
using System.Threading.Tasks;

namespace AMI.Neitsillia.Areas.AreaPartials
{
    partial class Area
    {
        public bool IsExplorable => (0 < (eLootRate + eQuestRate + ePassiveRate + eMobRate));

        public async Task<EmbedBuilder> ExploreArea(Player player)
        {
            player.EndEncounter();
            EmbedBuilder explore = new EmbedBuilder();
            explore.WithTitle(name + " Exploration");

            return type switch
            {
                AreaType.Dungeon => await ExploreDungeon(player, explore),
                AreaType.Arena => ExploreArena(player, explore),
                AreaType.Nest => ExploreNest(player, explore),
                _ => await ExploreRegularArea(player, explore),
            };
        }

        private async Task<EmbedBuilder> ExploreDungeon(Player player, EmbedBuilder explore)
        {
            int.TryParse((player.Party != null ? player.Party.areaKey : player.AreaInfo).data, out int i);
            if (Program.Chance(i * 13))
            {
                player.NewEncounter(new Encounter(Encounter.Names.Floor, player));
                explore = player.Encounter.GetEmbed(explore);
                await player.AreaData("0");
                return explore;
            }
            await player.AreaData((i + 1).ToString());
            return LocationMob(explore, player, 0);
        }

        EmbedBuilder ExploreNest(Player player, EmbedBuilder embed)
        {
            Random rng = Program.rng;
            int topLevel = player.level;

            if (!player.IsSolo)
            {
                foreach (var m in player.Party.members)
                {
                    Player p = m.id == player.userid ? player : m.LoadPlayer();
                    if (p.level > topLevel) topLevel = p.level;
                }
            }

            NPC[] mob = new NPC[(player.IsSolo ? 1 : player.Party.MemberCount) + 1];
            int evolves = 0;//player.IsSolo ? 0 : player.Party.MemberCount - 1;
            for (int k = 0; k < mob.Length; k++)
            {
                int rtier = ArrayM.IndexWithRates(mobs.Length, rng);
                mob[k] = NPC.GenerateNPC(topLevel, mobs[rtier][ArrayM.IndexWithRates(mobs[rtier].Count, rng)]);

                if (evolves > 0 && Program.Chance((20 * evolves) + (k * 20)))
                {
                    mob[k].Evolve(2, skaviCat: name);
                    evolves--;
                }

                embed.AddField(
                 mob[k].displayName + $" | m{k}",
                    "Level: " + mob[k].level + Environment.NewLine +
                    "Health: " + mob[k].health + '/' + mob[k].Health() + Environment.NewLine
                );
            }
            player.NewEncounter(new Encounter("Mob", player)
            { mobs = mob });

            embed.WithDescription("You've encountered a group of creatures");

            return embed;
        }

        async Task<EmbedBuilder> ExploreRegularArea(Player player, EmbedBuilder explore)
        {
            int x = Program.rng.Next(eLootRate + eMobRate + ePassiveRate + 1);

            int.TryParse((player.Party != null ? player.Party.areaKey : player.AreaInfo).data, out int floorChances);

            if (player.AreaInfo.floor < floors)
            {
                if ((floorChances * 5) + 10 >= Program.rng.Next(101))
                {
                    player.NewEncounter(new Encounter(Encounter.Names.Floor, player));
                    explore = player.Encounter.GetEmbed(explore);
                    await player.AreaData("0");
                    return explore;
                }

                await player.AreaData((floorChances + 1).ToString());
            }

            try
            {
                if (x <= eLootRate)
                    return !ValidTable(loot) || Program.Chance(35) ?
                        LocationRessource(player, explore)
                        : LocationLoot(player, explore);
                else if (x <= eMobRate + (eLootRate) && ValidTable(mobs))
                    return LocationMob(explore, player);
                else if (ePassiveRate > 0)
                    return LocationPassive(explore, player, 
                        passiveEncounter == null || passiveEncounter.Length == 0 || ValidTable(passives));  
            }
            catch (Exception e)
            {
                _ = Handlers.UniqueChannels.Instance.SendToLog(e, $"{player.userid} Exploring floor {player.AreaInfo.floor} of {player.Area.name}", null);

                if (eQuestRate > 0 && player.quests.Count < 15)
                {
                    Quest found = Quest.Load(new int[] {3,0,
                        Program.rng.Next(QuestLoad.QuestList[3][0].Length) });

                    if (player.Party != null)
                    {
                        foreach (var p in player.Party.members)
                        {
                            Player partyPlayer = Player.Load(p.Path, Player.IgnoreException.All);
                            if (partyPlayer.quests.Count < 15)
                            {
                                partyPlayer.quests.Add(found);
                                partyPlayer.SaveFileMongo();
                            }
                        }
                    }
                    else
                    {
                        player.quests.Add(found);
                        player.SaveFileMongo();
                    }
                    explore.WithTitle("New Quest");
                    explore.WithDescription("You've picked up a new quest.");
                    explore.AddField(found.AsEmbedField());
                    return explore;
                }
            }

            player.NewEncounter(Encounter.Names.Exploration, true);
            player.Encounter.EncounterNothing();
            explore = player.Encounter.GetEmbed();
            return explore;
        }

        private EmbedBuilder LocationRessource(Player player, EmbedBuilder explore)
        {
            Encounter encounter = new Encounter(Encounter.Names.Ressource, player);
            player.NewEncounter(encounter);
            player.SaveFileMongo();
            return encounter.GetEmbed(explore);
        }

        private EmbedBuilder LocationLoot(Player player, EmbedBuilder explore)
        {
            Random rng = Program.rng;
            //Create Encounter
            Encounter encounter = new Encounter(Encounter.Names.Loot, player);
            explore.Color = player.userSettings.Color;
            explore.Description = "While exploring " + name + " you've discovered loot.";

            //Get Loot Count
            int perBonus = NumbersM.CeilParse<int>(player.stats.GetPER() * Collections.Stats.LootPerPerc);

            //Get the level to scale gear to
            int level = GetAreaFloorLevel(rng, player.AreaInfo.floor);
            //Roll through loot table
            LootTables<string> tb = new LootTables<string>(loot, rng);
            tb.GetItems(player.AreaInfo.floor/floors, perBonus, (name, t1, t2) =>
            {
                try
                {
                    //gets the item in that tier, creates a new item and adds it to the loot event collection
                    Item item = Item.LoadItem(name.Trim());
                    item.Scale(level * 5);
                    encounter.AddLoot(item);
                }catch(Exception e)
                {
                    _ = Handlers.UniqueChannels.Instance.SendToLog(e, t1 == -1 || t2 == -1 ? "Loot index array failed" : $"Failed to load loot {name} {t1} {t2}");
                }
            });

            //Coins
            int minCoins = Verify.Min(level, 2) * 6;
            int coinslooted = rng.Next(NumbersM.CeilParse<int>(minCoins * 0.75), NumbersM.CeilParse<int>(minCoins * 1.25));
            if (player.Party != null)
            {
                coinslooted /= player.Party.members.Count;
                foreach (var m in player.Party.members)
                {
                    Player partyPlayer = Player.Load(m.Path);
                    partyPlayer.KCoins += coinslooted;
                    partyPlayer.SaveFileMongo(false);
                }
            }
            else player.KCoins += coinslooted;
            if (coinslooted > 0)
                explore.AddField("Kutsyei Coins", "+" + coinslooted);
            //Directly add coins

            //Items
            string lootList = null;
            for (int i = 0; i < encounter.loot.Count; i++)
                if (encounter.loot.inv[i] != null)
                    lootList += i + "| " + encounter.loot.inv[i] + Environment.NewLine;
            if (lootList != null && lootList != "")
                explore.AddField("Items", lootList);
            //
            explore.WithFooter("Coins are automatically collected");
            player.NewEncounter(encounter);
            player.SaveFileMongo();
            return explore;
        }

        internal EmbedBuilder ForceBountyEncounter(Player player)
        {
            player.EndEncounter();
            EmbedBuilder explore = new EmbedBuilder();
            explore.WithTitle(name + " Exploration");
            return LocationMob(explore, player, 100);
        }
        internal EmbedBuilder LocationMob(EmbedBuilder explore, Player player, int bountychances = 10)
        {
            Random rng = new Random();
            bool mobIsBounty = rng.Next(101) <= bountychances;
            NPC[] mob = null;
            if (mobIsBounty && GetPopulation(AreaExtentions.Population.Type.Bounties) != null && _bounties.Count > 0)
            {
                mob = new NPC[] { _bounties.Random() };
                player.NewEncounter(new Encounter(Encounter.Names.Bounty, player)
                { mobs = mob });
            }
            else mobIsBounty = false;
            if (!mobIsBounty)
            {
                int partyCount = player.Party?.MemberCount ?? 1;
                int rngMobNum = (player.level < 10 ? 0 : 15) + (partyCount * 7);

                int mobCount = Verify.MinMax(NumbersM.NParse<int>(rng.Next(rngMobNum / 4, rngMobNum + 1) / 10.00), 6, 1);
                mob = new NPC[mobCount];

                for (int i = 0; i < mob.Length; i++)
                    mob[i] = GetAMob(rng, player.AreaInfo.floor);

                if (Program.Chance(15)) Utils.RandomElement(mob).Evolve();

                player.NewEncounter(new Encounter(Encounter.Names.Mob, player)
                { mobs = mob });
            }

            if (mob.Length == 1)
            {
                explore.WithDescription("You've encountered a " + (mobIsBounty ? " **dangerous bounty**" : mob[0].name));
                explore.AddField(mob[0].displayName + " ",
                "Level: " + mob[0].level + Environment.NewLine +
                //"Rank: " + mob[0].Rank() + Environment.NewLine +
                "Health: " + mob[0].health + '/' + mob[0].Health() + Environment.NewLine
                );
            }
            else
            {
                explore.WithDescription("You've encountered a group of creatures");

                if (mob.Length > 3)
                    mob[rng.Next(mob.Length)].Evolve(player.Party.MemberCount, skaviCat: name);

                int i = 0;
                foreach (var m in mob)
                {
                    explore.AddField(
                    m.displayName + $" | m{i}",
                    "Level: " + m.level + Environment.NewLine +
                    //"Rank: " + m.Rank() + Environment.NewLine +
                    "Health: " + m.health + '/' + m.Health() + Environment.NewLine
                    );
                    i++;
                }

            }
            player.SaveFileMongo();
            return explore;
        }

        private EmbedBuilder LocationPassive(EmbedBuilder explore, Player player, bool valid)
        {
            player.NewEncounter(
                 eMobRate > 0 && ValidTable(mobs) && Program.Chance(eMobRate / 3.5) ? new Encounter(Encounter.Names.Puzzle, player, 
                 $"~Random;2;{Utils.RandomElement(Utils.RandomElement(mobs))}") :
                 eLootRate > 0 && (!valid || Program.Chance(eLootRate / 3.5)) ? new Encounter(Encounter.Names.Puzzle, player, "~Random;1;~Random") :
                new Encounter(Utils.RandomElement(passiveEncounter ?? Utils.RandomElement(passives)) ?? (type == AreaType.Town ? "NPC" : null), player));
            explore = player.Encounter.GetEmbed(explore);
            return explore;
        }
    }
}
