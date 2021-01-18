using AMI.Methods;
using AMI.Neitsillia.Areas;
using AMI.Neitsillia.Areas.AreaPartials;
using AMI.Neitsillia.Areas.Nests;
using AMI.Neitsillia.Collections;
using AMI.Neitsillia.Encounters;
using AMI.Neitsillia.Items;
using AMI.Neitsillia.Items.Perks.PerkLoad;
using AMI.Neitsillia.NeitsilliaCommands;
using AMI.Neitsillia.NPCSystems;
using AMI.Neitsillia.User.PlayerPartials;
using AMI.Neitsillia.User.UserInterface;
using AMYPrototype;
using Discord;
using Neitsillia.Items.Item;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AMI.Neitsillia.Combat
{
    class CombatEndHandler
    {
        static AMIData.MongoDatabase Database => Program.data.database;
        static Random Rng => Program.rng;

        Party party;
        Encounter currentEncounter;
        Area currentArea;

        Player partyLeader;

        bool endDungeon;
        bool allPlayersDead;
        bool allMobsDead;

        CombatResult[] playerParty;
        CombatResult[] mobParty;

        public CombatEndHandler(Combat combat, Party party, Encounter currentEncounter, 
            Area currentArea)
        {
            this.party = party;
            this.currentEncounter = currentEncounter;
            this.currentArea = currentArea;

            playerParty = combat.playerParty;
            mobParty = combat.mobParty;

            partyLeader = ((Player)playerParty[0].character);

            endDungeon = (currentArea.type == AreaType.Dungeon || currentArea.type == AreaType.Arena) && partyLeader.areaPath.floor >= currentArea.floors;
            allPlayersDead = Combat.ArePlayersDead(playerParty);
            allMobsDead = Combat.IsMobDead(mobParty);
        }

        AreaPath ParentAreaPath(Area area, int floor = 0)
            => new AreaPath()
            {
                path = area.GeneratePath(false) + area.parent,
                name = area.parent,
                floor = floor
            };

        public async Task<MsgType> Handle(EmbedBuilder result, MsgType msgType)
        {
            if (allPlayersDead)
                msgType = await HandleAllPlayersDead(result);
            else if (allMobsDead)
                msgType = await HandleAllMobsDead(result);
            else
            {
                msgType = MsgType.Combat;
                foreach (var cb in playerParty)
                    if (cb.character is Player player)
                        player.SaveFileMongo();
            }

            if (party != null)
            {
                if (endDungeon) party.areaKey = ParentAreaPath(currentArea, partyLeader.areaPath.floor);
                await party.SaveData();
                if (currentEncounter != null && !allPlayersDead)
                    currentEncounter.Save();
            }
            return msgType;
        }

        private async Task<MsgType> HandleAllPlayersDead(EmbedBuilder result)
        {
            NPC mob = (NPC)Utils.RandomElement(mobParty).character;
            string lostInfo = allMobsDead ? "No one is left standing to claim victory." : "You have been defeated." + Environment.NewLine;

            if (endDungeon) await Database.DeleteRecord<Area>("Dungeons", partyLeader.areaPath.path, "AreaId");

            foreach (var cb in playerParty)
            {
                PerkLoad.CheckPerks(cb.character, Perk.Trigger.EndFight, cb.character);
                if (cb.character is Player player)
                {
                    if (!allMobsDead) lostInfo += DefeatCost(player, mob, 0.5) + Environment.NewLine;

                    await player.Respawn(false);

                    if (endDungeon) player.areaPath = ParentAreaPath(currentArea, partyLeader.areaPath.floor);
                    player.SaveFileMongo();
                }
                else if (cb.character is NPC n)
                    FollowerCheck(n, party, allPlayersDead);
            }
            result.AddField("Defeat", lostInfo);

            if (!allMobsDead && currentEncounter.Name != Encounter.Names.Bounty && currentArea.type != AreaType.Dungeon)
                PopulationHandler.Add(currentArea, mob);
            return MsgType.Main;
        }

        public static string DefeatCost(Player player, NPC mob, double intensity = 1)
        {
            long xpDropped = NumbersM.NParse<long>(player.XPDrop(mob.level) * intensity);
            player.experience -= xpDropped;
            long coinsLost = Verify.Max(NumbersM.NParse<long>(((mob.Rank() + mob.level) * 2) * intensity), player.KCoins);
            player.KCoins -= coinsLost;
            //Log.CombatData(mob, player, xpDropped);
            mob.XPGain(xpDropped);
            return $"{player.name} lost {xpDropped} XP And {coinsLost} Kuts";
        }

        void FollowerCheck(NPC n, Party party, bool allPlayersDead)
        {
            int s = n.HealthStatus(out string status);
            /// >= 0 Is alive
            /// -1 is Down, -2 Fainted, -3 Unconscious, -4 Dead, -5 Vaporized
            if (s < -1 || allPlayersDead)
            {
                if (n.IsPet())
                {
                    if (party != null && s <= -4)
                    {
                        party.Remove(party.NPCMembers.FindIndex
                            (x => x.origin == n.origin), null);
                    }
                    else
                    {
                        n.health = n.Health();
                        n.stamina = n.Stamina();
                    }

                }
                else
                {
                    if (party != null)
                        party.NPCMembers.RemoveAt(party.NPCMembers.FindIndex
                            (x => x.displayName == n.displayName));
                    if (s > -4)
                        n.Respawn();
                }
            }
            else if (s >= -1)
            {
                long fullHealthCost = Verify.Max(
                    NumbersM.NParse<long>(100 - (((n.health + 0.00) / n.Health()) * 100))
                    , n.KCoins);
                if (fullHealthCost > 0)
                {
                    n.KCoins -= fullHealthCost;
                    n.health += NumbersM.CeilParse<long>(n.Health() * (fullHealthCost / 100.00));
                }
                n.stamina = n.Stamina();
                n.SelfGear();
            }
        }

        private async Task<MsgType> HandleAllMobsDead(EmbedBuilder result)
        {
            if (endDungeon)
            {
                await Database.DeleteRecord<Area>("Area", partyLeader.areaPath.path, "AreaId");
                await Database.DeleteRecord<Area>("Dungeons", partyLeader.areaPath.path, "AreaId");
            }

            //Get Loot into Encounter
            Encounter enc = new Encounter("Loot", partyLeader);

            (string[] kills, long koinsToGain, long xpToGain) = GetKillRewards(enc);

            OtherLoot(enc);

            var invLoot = enc.loot.inv;

            string lootDisplay = null;
            if (party != null)
            {
                koinsToGain = NumbersM.CeilParse<long>(koinsToGain / (double)party.MemberCount);
                lootDisplay += $"+{koinsToGain} Kutsyei Coins Per Party Member." + Environment.NewLine;
            }
            else lootDisplay += $"+{koinsToGain} Kutsyei Coins " + Environment.NewLine;

            lootDisplay += EventRewards(enc, out (string name, int amount) specialCurrencyReward);

            foreach (var cb in playerParty)
            {
                PerkLoad.CheckPerks(cb.character, Perk.Trigger.EndFight, cb.character);

                bool hasDied = cb.character.IsDead();
                if (!hasDied) cb.character.KCoins += koinsToGain;

                if (cb.character is Player player)
                    lootDisplay += await PlayerOnAllMobsDead(player, specialCurrencyReward, xpToGain, kills, enc);
                else if (cb.character is NPC follower)
                {
                    lootDisplay += $" |-|{(follower.IsPet() ? follower.displayName : follower.name)} +{Utils.Display(follower.XPGain(xpToGain, follower.level))} XP {Environment.NewLine}";

                    //NPC looting
                    if (follower.IsPet())
                        follower.AddItemToInv(Item.RandomItem(follower.level, 1), follower.level);
                    else if (enc.loot.Count > 4)
                    {
                        int randomLoot = Program.rng.Next(enc.loot.Count);
                        int amount = Program.rng.Next(1, enc.loot.GetCount(randomLoot) + 1);
                        follower.AddItemToInv(enc.loot.GetItem(randomLoot), amount, true);
                        enc.loot.Remove(randomLoot, amount);
                    }
                    else follower.AddItemToInv(Item.RandomItem(follower.level));
                    //
                    FollowerCheck(follower, party, allPlayersDead);
                }

            }

            //Manage Encounters
            if (party != null) enc.Save();
            currentEncounter = null;

            FinalizeResultEmbed(invLoot, result, lootDisplay);
            return MsgType.Loot;
        }

        (string[], long, long) GetKillRewards(Encounter enc)
        {
            long koinsToGain = 0;
            long xpToGain = 0;
            string[] kills = new string[mobParty.Length];
            int killsIndex = 0;
            foreach (var mobcb in mobParty)
            {
                NPC mob = (NPC)mobcb.character;
                enc.AddLoot(mob.inventory);
                if (mob.KCoins > 0)
                    koinsToGain += mob.KCoins;
                xpToGain += mob.XPDrop(0);

                kills[killsIndex] = $"{mob.name};{mob.race};{mob.level}";
                killsIndex++;
            }

            return (kills, koinsToGain, xpToGain);
        }

        void OtherLoot(Encounter enc)
        {
            bool top = partyLeader.areaPath.floor >= currentArea.floors;
            if(currentArea.type == AreaType.Arena && top)
            {
                if (currentArea.loot != null && Program.Chance(currentArea.eLootRate))
                {
                    int t = ArrayM.IndexWithRates(currentArea.loot.Length, Rng);
                    enc.AddLoot(Item.LoadItem(currentArea.loot[t][ArrayM.IndexWithRates(currentArea.loot[t].Count, Rng)]));
                }
                else if (Program.Chance(currentArea.eLootRate))
                {
                    Item reward = Item.RandomItem(currentArea.level, Rng.Next(6, 12));
                    enc.AddLoot(reward);
                }
            }
        }

        string EventRewards(Encounter enc, out (string name, int amount) eventReward)
        {
            int amount = 0;
            AMIData.Events.OngoingEvent cevent = AMIData.Events.OngoingEvent.Ongoing;

            if (currentEncounter.Name == Encounter.Names.Bounty && cevent.BountyReward != null)
                amount += 1;

            switch (currentArea.type)
            {
                case AreaType.Arena:
                    if (cevent.eventinfo.IsRewardSource(AMIData.Events.EventInfo.RewardSources.Arena))
                        amount++;
                    break;

            }

            eventReward = amount > 0 ? (cevent.Currency, amount) : (null, 0);
            return eventReward.name != null ? $"**+{eventReward.amount} {eventReward.name}**" + Environment.NewLine : null;
        }

        async Task<string> PlayerOnAllMobsDead(Player player, (string name, int amount) specialCurrencyReward, long xpToGain, string[] kills, Encounter enc)
        {
            bool hasDied = player.health <= 0;
            string lootDisplay = null;
            if (specialCurrencyReward.name != null) player.Currency.Mod(specialCurrencyReward.name, specialCurrencyReward.amount);

            if (currentEncounter.Name == Encounter.Names.FloorJump)
                lootDisplay += FloorJumpReward(player, currentEncounter.data);
            else if (!hasDied)
            {
                //XP
                long xpGained = hasDied ? 0 : player.XPGain(xpToGain, player.level);
                lootDisplay += $" |-| {player.name} +{Utils.Display(xpGained)} XP {Environment.NewLine}";

                if (currentArea.type == AreaType.Nest)
                    await NestReward(player, currentArea);
            }

            if (hasDied)
            {
                if (party != null)
                {
                    await party.Remove(player);
                    player.PartyKey = null;
                }
                else player.EndEncounter();
                await player.Respawn(true);

            }
            else //Is still alive
            {
                CompletedObjectives(player, kills);

                if (endDungeon)//Complete dungeon
                {
                    player.areaPath = ParentAreaPath(currentArea, partyLeader.areaPath.floor);
                    player.Quest_Trigger(Items.Quests.Quest.QuestTrigger.ClearDungeon);
                }
                //Give the loot encounter
                player.Encounter = enc;
            }

            player.SaveFileMongo(party == null);
            return lootDisplay;
        }

        string FloorJumpReward(Player player, string floor)
        {
            int f = int.Parse(floor);
            player.areaPath.floor += f;

            player.Quest_Trigger(Items.Quests.Quest.QuestTrigger.EnterFloor,
            $"{player.areaPath.path};{player.areaPath.floor}");
            player.EggPocket_Trigger(NPCSystems.Companions.Egg.EggChallenge.Exploration);

            return $"{player.name} has advanced {f} floors." + Environment.NewLine;
        }

        async Task NestReward(Player player, Area area)
        {
            Nest nest = Nest.GetNest(area.GeneratePath(false));
            if (nest == null) return;

            await nest.Vicotry(player);
        }

        void CompletedObjectives(Player player, string[] kills)
        {
            foreach (string k in kills)
                player.Quest_Trigger(Items.Quests.Quest.QuestTrigger.Kill, k);

            player.EggPocket_Trigger(NPCSystems.Companions.Egg.EggChallenge.Combat);
        }

        void FinalizeResultEmbed(List<StackedItems> invLoot, EmbedBuilder result, string lootDisplay)
        {
            string itemsLootDisplay = null;
            int i = 0;
            for (; i < 10 && i < invLoot.Count; i++)
                if (invLoot[i] != null)
                    itemsLootDisplay += $"{(i + 1)}| {invLoot[i]} {Environment.NewLine}";

            if (i > invLoot.Count) itemsLootDisplay += $"{invLoot.Count - i} More Items..." + Environment.NewLine;

            result.AddField("Victory", "You've defeated your opponent.");
            if (lootDisplay != null)
                result.AddField("Loot", itemsLootDisplay + lootDisplay);
            result.WithFooter("Coins and XP are automatically collected");
        }
    }
}
