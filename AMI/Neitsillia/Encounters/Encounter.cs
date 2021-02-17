using AMI.AMIData;
using AMI.Neitsillia.Areas.AreaExtentions;
using AMI.Neitsillia.Collections;
using AMI.Neitsillia.Items.Perks.PerkLoad;
using AMI.Neitsillia.NPCSystems;
using AMI.Neitsillia.User.PlayerPartials;
using AMI.Neitsillia.User.UserInterface;
using AMYPrototype;
using Discord;
using Neitsillia.Items.Item;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace AMI.Neitsillia.Encounters
{
    [MongoDB.Bson.Serialization.Attributes.BsonIgnoreExtraElements]
    public class Encounter
    {
        [MongoDB.Bson.Serialization.Attributes.BsonId]
        public string _id;
        public Names Name;
        public string encounterEvent; //description
        public string footerInfo;

        private readonly Player player;
        //Encounter Stuff
        public string data;
        //Passive
        public Inventory loot = new Inventory();
        public long xpToGain;
        public long koinsToGain;

        //NPCs
        public NPC npc;
        public NPC[] mobs;

        public int turn;

        public Puzzle puzzle;

        public enum Names
        {
            Exploration,
            Floor, Loot, NPC, Adventure, //Passives
            Mob, Bounty, PVP, Dungeon, FloorJump, //Combat
            Puzzle,
        };

        public Encounter(Names aname, Player aplayer, string aData = null)
        {
            Name = aname;
            player = aplayer;
            data = aData;
            Load(Name);
            if (player.Party == null) _id = player._id;
            else _id = player.Party.EncounterKey;
        }
        [JsonConstructor]
        public Encounter(bool json)
        {}
        public Encounter(string v, Player player, string aData = null)
        {
            Name = (Names)Enum.Parse(typeof(Names), v, true);
            this.player = player;
            data = aData;
            Load(Name);
            _id = player.Party?.EncounterKey ?? player._id;
        }

        internal static Encounter LoadDB(string id)
        {
            return Program.data.database.
                LoadRecord("Encounter", MongoDatabase.FilterEqual<Encounter, string>("_id", id));
        }
        internal void Save()
        {
            Program.data.database.
                UpdateRecord("Encounter", "_id", _id, this);
        }
        internal void Delete()
        {
            Program.data.database.
                DeleteRecord<Encounter>("Encounter", _id).Wait();
        }
        public bool Load(Names name)
        {
            Name = name;
            if (Passives())
            { return true; }
            else if (Combat())
            {
                if(player != null) player.duel = new User.DuelData(null);

                turn = 1;
                return true;
            }
            return false;
        }
        private bool Passives()
        {
            switch(Name)
            {
                case Names.Floor:
                    {
                        if (player.AreaInfo.floor < player.Area.floors)
                        {
                            encounterEvent = "You've discovered a pathway to a new floor of this area";
                            footerInfo = $"'~Enter Floor' OR {EUI.enterFloor} to proceed to the next floor";
                            return true;
                        }
                        else return Load(Names.Dungeon);
                        
                    }
                case Names.Dungeon:
                    {
                        encounterEvent = "You've discovered a Dungeon which seem to offer a worthy opponent " +
                            "at the end of its floors. Entering would lead to a point of no return";
                        footerInfo = $"'~Enter Dungeon' OR {EUI.enterFloor} to proceed into the dungeon";
                        return true;
                    }
                case Names.Loot:
                case Names.Adventure:
                    {
                        loot = new Inventory();
                        return true;
                    }
                case Names.NPC:
                    {
                        var popu = player.Area.GetPopulation(Areas.AreaExtentions.Population.Type.Population);
                        var bounty = player.Area.GetPopulation(Areas.AreaExtentions.Population.Type.Bounties);

                        if ((popu == null || popu.Count <= 0) && (bounty == null || bounty.Count <= 0)) return EncounterNothing();
                        else
                        {
                            int topRoll = 0;
                            if (popu != null) topRoll += popu.Count;
                            if (bounty != null) topRoll += bounty.Count;
                            //
                            int roll = Program.rng.Next(topRoll);
                            if (popu != null && roll < popu.Count)
                            {
                                if (EncounterNPC(popu))
                                    return true;
                                else if (bounty != null && bounty.Count > 0
                                    && EncounterBounty(bounty) != null)
                                    return true;
                                else
                                    return EncounterNothing();
                            }
                            else if (EncounterBounty(bounty) != null)
                                return true;
                        }
                        return true;
                    }
                case Names.Puzzle:
                    {
                        string[] piz = data?.Split(';') ?? new string[]{ "~Random", "1", "~Random"};

                        if (piz[0] == "~Random")  puzzle = Puzzle.Random();
                        else puzzle = Puzzle.Load(piz[0]);

                        if (piz.Length > 1 && int.TryParse(piz[1], out int t))
                        {
                            puzzle.rewardType = (Puzzle.Reward)t;
                            if(piz.Length > 2) puzzle.reward = piz[2];
                        }

                        encounterEvent = puzzle.description;
                        puzzle.level = player?.level ?? 0;
                    }
                    break;
            }
            return false;
        }
        bool EncounterNPC(Population popu)
        {
            npc = popu.Random();
            if (npc == null) return false;
            encounterEvent = $"You've encountered {npc.displayName}";
            footerInfo = "";
            return true;
        }
        NPC EncounterBounty(Population bounties)
        {
            NPC temp = bounties.Random();
            encounterEvent = "You've encountered a **dangerous bounty** : "
                + temp.displayName;
            footerInfo = "";
            player.duel = new User.DuelData(null);
            Name = Names.Bounty;
            mobs = new NPC[] { temp };
            return temp;
        }
        public bool EncounterNothing()
        {
            encounterEvent = "You've not found anything, the calm patrolling allowed your body to recover from some exhaustion";
            footerInfo = "You've regained 10% Health and 25% Stamina";
            if (player.Party != null)
            {
                foreach (var m in player.Party.members)
                {
                    Player temp = player.userid == m.id ? player : m.LoadPlayer();
                    temp.Healing(Convert.ToInt32(player.Health() * 0.1));
                    temp.StaminaE(Convert.ToInt32(player.Stamina() * 0.25));
                    temp.SaveFileMongo();
                }
            }
            else
            {
                player.Healing(Convert.ToInt32(player.Health() * 0.1));
                player.StaminaE(Convert.ToInt32(player.Stamina() * 0.25));
            }
            Name = Names.Exploration;
            return true;
        }

        bool Combat()
        {
            switch(Name)
            {
                case Names.Mob:
                case Names.Bounty:
                case Names.PVP:
                    return true;
                case Names.FloorJump:
                    {
                        encounterEvent = "Defeat the enemy to advance at the end of its floors.";
                        //footerInfo = $"'~Enter Dungeon' OR {EUI.enterFloor} to proceed into the dungeon";
                        return true;
                    }
            }
            return false;
        }

        public void StartCombat()
        {
            List<CharacterMotherClass> playerTeam = new List<CharacterMotherClass>();

            if (player.Party == null)
            {
                PerkLoad.CheckPerks(player, Items.Perk.Trigger.StartFight, player, mobs);
                playerTeam.Add(player);
            }
            else
            {
                foreach (var m in player.Party.members)
                {
                    Player p = m.id == player.userid ? player : m.LoadPlayer();

                    PerkLoad.CheckPerks(p, Items.Perk.Trigger.StartFight, p, mobs);
                    playerTeam.Add(p);
                    p.SaveFileMongo();
                }
                foreach (var n in player.Party.NPCMembers)
                {
                    PerkLoad.CheckPerks(n, Items.Perk.Trigger.StartFight, n, mobs);
                }
                playerTeam.AddRange(player.Party.NPCMembers);
                _ = player.Party.SaveData();
            }

            foreach (var m in mobs)
            {
                PerkLoad.CheckPerks(m, Items.Perk.Trigger.StartFight, m, playerTeam.ToArray());
            }
        }

        public bool AddLoot(Item item) => loot.Add(item, 1, -1);

        public bool AddLoot(StackedItems i) => loot.Add(i.item, i.count, -1);

        public bool AddLoot(Inventory inv) => loot.Add(inv, -1);

        public EmbedBuilder GetEmbed(EmbedBuilder embed = null)
        {
            if(embed == null) embed = new EmbedBuilder();
            switch(Name)
            {
                case Names.Puzzle:
                    embed.Title = $"{puzzle.name} Puzzle";
                    puzzle.Solve_Puzzle(null, 0, out embed);
                    break;
                case Names.Loot:
                    {
                        int page = 0;
                        embed = loot.ToEmbed(ref page, "Loot", -1, player?.equipment);
                    }break;
                default:
                    {
                        embed.Title = Name.ToString();
                        embed.Description = encounterEvent;
                        embed.WithFooter(footerInfo);
                        if (npc != null)
                            embed = npc.NPCInfo(embed, true, false, false, false);
                        if (mobs != null)
                        {
                            int i = 0;
                            foreach (var m in mobs)
                            {
                                embed.AddField(
                                m.displayName + $" | m{i}",
                                "Level: " + m.level + Environment.NewLine +
                                "Rank: " + m.Rank() + Environment.NewLine +
                                "Health: " + m.health + '/' + m.Health() + Environment.NewLine
                                );
                                i++;
                            }
                        }
                    }
                    break;
            }
            return embed.WithColor(player?.userSettings?.Color ?? Color.DarkBlue);
        }
        public void TurnIntoLoot(Player player)
        {
            player.KCoins += koinsToGain;
            player.XpGain(xpToGain);
            koinsToGain = 0;
            xpToGain = 0;
            Name = Names.Loot;
        }
        //
        internal string EncounterTitle()
        {
            if (IsCombatEncounter())
                return "Combat";
            else if (Name == Names.NPC)
                return npc.displayName;
            return Name.ToString();
        }
        public bool IsCombatEncounter()
        {
            return (Name == Names.Bounty || Name == Names.Mob ||
                Name == Names.PVP || Name == Names.FloorJump);
        }
        public bool IsPassiveEncounter()
        {
            return (Name == Names.Floor || Name == Names.Loot || Name == Names.NPC
                || Name == Names.Exploration || Name == Names.Adventure);
        }
        public bool IsNPC()
        {
            return (Name == Names.NPC && npc != null);
        }

        internal static void NewDuel(Player player, Player enemy)
        {
            player.EndEncounter();
            enemy.EndEncounter();
            enemy.EncounterKey = new DataBaseRelation<string, Encounter>(enemy._id,
                new Encounter(Names.PVP, enemy));
            player.duel = new User.DuelData($"{enemy.userid}/{enemy.name}");
            player.EncounterKey = new DataBaseRelation<string, Encounter>(player._id,
                new Encounter(Names.PVP, player));
            enemy.duel = new User.DuelData($"{player.userid}/{player.name}");
            player.SaveFileMongo();
            enemy.SaveFileMongo();
        }
    }
}
