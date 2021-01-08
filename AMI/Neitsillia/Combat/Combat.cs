using AMI.Methods;
using AMI.Neitsillia.Items;
using AMI.Neitsillia.Items.Perks.PerkLoad;
using AMI.Neitsillia.NPCSystems;
using AMI.Neitsillia.User.PlayerPartials;
using AMYPrototype;
using Discord;
using System;

namespace AMI.Neitsillia.Combat
{
    public class Combat
    {
        static AMIData.MongoDatabase Database => Program.data.database;

        internal static CombatResult GetTargetCR(CombatResult caster, CombatResult[] friends, CombatResult[] foes)
        {
            if (caster.abilityUsed == null) return caster;
            CombatResult[] selection = null;
            if (caster.target == "Default")
            {
                switch (caster.abilityUsed.type)
                {
                    case Ability.AType.Martial:
                    case Ability.AType.Elemental:
                    case Ability.AType.Tactical:
                    case Ability.AType.Enchantment:
                        foreach (var foe in foes)
                        {
                            if (foe.character.health > 0)
                                return foe;
                        }
                        return foes[0];
                    case Ability.AType.Defensive:
                        return caster;
                }

            }
            switch (caster.target[0])
            {
                case '0':
                    selection = foes;
                    break;
                case '1':
                    selection = friends;
                    break;
            }
            int index = Verify.MinMax(int.Parse(caster.target[2].ToString()), selection.Length - 1);
            return selection[index];
        }

        internal static bool IsMobDead(CombatResult[] crs)
        {
            foreach (var c in crs)
                if (c.character.health > 0)
                    return false;
            return true;
        }
        internal static bool ArePlayersDead(CombatResult[] crs)
        {
            foreach (var c in crs)
                if (c.character is Player && c.character.health > 0)
                    return false;
            return true;
        }

        //Instance

        internal CombatResult[] playerParty;
        internal CombatResult[] mobParty;

        //Constructors 
        internal Combat(Player arg1, Ability arg2, Player arg3, Ability arg4)//PvP
        {
            Random rng = new Random();
            playerParty = new CombatResult[1] { new CombatResult(arg1, arg2, rng, this, CombatResult.Team.P) };
            mobParty = new CombatResult[1] { new CombatResult(arg3, arg4, rng, this, CombatResult.Team.M) };
        }
        internal Combat(Player arg1, Ability arg2, NPC arg3)//PvE
        {
            Random rng = new Random();
            playerParty = new CombatResult[1] { new CombatResult(arg1, arg2, rng, this, CombatResult.Team.P) };
            mobParty = new CombatResult[1] 
            {
                NPCSetUp(arg3, new CharacterMotherClass[] { arg3 },
                new CharacterMotherClass[] { arg1 }, rng, CombatResult.Team.M)
            };
        }
        internal Combat(NPC arg1, NPC arg3)//EvE
        {
            Random rng = new Random();
            playerParty = new CombatResult[1] { new CombatResult(arg1, NPCCombat.MobAI(arg1), rng, this, CombatResult.Team.P) };
            mobParty = new CombatResult[1] { new CombatResult(arg3, NPCCombat.MobAI(arg3), rng, this, CombatResult.Team.M) };
        }
        internal Combat(NPC[] mobs, params CharacterMotherClass[] players)
        {
            //
            Random rng = new Random();
            playerParty = new CombatResult[players.Length];
            for (int i = 0; i < players.Length; i++)
            {
                if(players[i] is Player player)
                    playerParty[i] = new CombatResult(players[i], player.GetAbility(player.duel.abilityName, true),
                        rng, this, CombatResult.Team.P);
                else if (players[i] is NPC follower)
                    playerParty[i] = NPCSetUp(follower, players, mobs, rng, CombatResult.Team.P);
            }
            //
            mobParty = new CombatResult[mobs.Length];
            for (int i = 0; i < mobs.Length; i++)
                mobParty[i] = NPCSetUp(mobs[i], mobs, players, rng, CombatResult.Team.M);
        }



        CombatResult NPCSetUp(NPC self, CharacterMotherClass[] allies, CharacterMotherClass[] enemies, Random rng,
            CombatResult.Team t)
        {
            int state = self.HealthStatus(out string r, false);
            if (state > -1)
            {
                int[] info = NPCCombat.MobAI(self, allies, enemies);
                CombatResult cresult = new CombatResult(self, self.abilities[info[0]], rng, this, t)
                {
                    target = info[1] + "," + info[2]
                };
                return cresult;
            }
            CombatResult cr = new CombatResult(self, null, rng, this, t);
            if (state > -2 && state < 2)
            {
                if(self.ConsumeHealing())
                    cr.action = CombatResult.Action.Consume;
            }
            return cr;
        }
        

        internal void InitiateAll()
        {
            foreach (var cp in playerParty)
                cp.Initiate();
            foreach (var cp in mobParty)
                cp.Initiate();
        }

        internal void Turn()
        {
            foreach (CombatResult cp in playerParty)
                if(!cp.character.IsDead())
                    cp.ExecuteSendingTurn(GetTargetCR(cp, playerParty, mobParty));

            foreach (CombatResult cp in mobParty)
                if (!cp.character.IsDead())
                    cp.ExecuteSendingTurn(GetTargetCR(cp, mobParty, playerParty));

            foreach (CombatResult cp in playerParty)
                PerkLoad.CheckPerks(cp.character, Perk.Trigger.Health, cp.character);
            foreach (CombatResult cp in mobParty)
                PerkLoad.CheckPerks(cp.character, Perk.Trigger.Health, cp.character);
        }

        internal void FillFightInfo(EmbedBuilder fight)
        {
            string playerPartyInfo = null;
            int i = 0;
            foreach (var cp in playerParty)
            {
                playerPartyInfo += cp.GetResultInfo("p"+i) +
                      Environment.NewLine;
                i++;
            }
            i = 0;
            string mobPartyInfo = null;
            foreach (var cp in mobParty)
            {
                mobPartyInfo += cp.GetResultInfo("m"+i) +
                      Environment.NewLine;
                i++;
            }
            fight.AddField("Player Party", playerPartyInfo, true);
            fight.AddField("Enemy Party", mobPartyInfo, true);
        }

        internal string GetResultInfo(CombatResult[] party, string s)
        {
            string result = "";
            int i = 0;
            foreach (var m in party)
            { result += m.GetResultInfo(s+i) + Environment.NewLine; i++; }
            return result;
        }

        /*
        internal async Task<MsgType> FightReward(Party party, Encounter currentencounter, Area currentArea, EmbedBuilder result, MsgType msgType)
        {
            Player partyLeader = ((Player)playerParty[0].character);

            bool endDungeon = (currentArea.type == AreaType.Dungeon || currentArea.type == AreaType.Arena) && partyLeader.areaPath.floor >= currentArea.floors;
            bool allPlayersDead = ArePlayersDead(playerParty);
            bool allMobsDead = IsMobDead(mobParty);
            if (allPlayersDead)
            {
                NPC mob = (NPC)Utils.RandomElement(mobParty).character;
                string lostInfo = allMobsDead ? "No one is left standing to claim victory." : "You have been defeated." + Environment.NewLine;

                if (endDungeon) await Database.DeleteRecord<Area>("Dungeons", partyLeader.areaPath.path, "AreaId");

                foreach (var cb in playerParty)
                {
                    PerkLoad.CheckPerks(cb.character, Perk.Trigger.EndFight, cb.character);
                    if (cb.character is Player player)
                    {
                        if(!allMobsDead) lostInfo += CombatEndHandler.DefeatCost(player, mob, 0.5) + Environment.NewLine;

                        await player.Respawn(false);

                        if (endDungeon) player.areaPath = ParentAreaPath(currentArea, partyLeader.areaPath.floor);
                        player.SaveFileMongo();
                    }
                    else if (cb.character is NPC n)
                        FollowerCheck(n, party, allPlayersDead);
                }
                result.AddField("Defeat", lostInfo);
                msgType = MsgType.Main;
                if (!allMobsDead && currentencounter.Name != Encounter.Names.Bounty && currentArea.type != AreaType.Dungeon)
                    PopulationHandler.Add(currentArea, mob);
            }
            else if (allMobsDead)
            {
                if (endDungeon)
                {
                    await Database.DeleteRecord<Area>("Area", partyLeader.areaPath.path, "AreaId");
                    await Database.DeleteRecord<Area>("Dungeons", partyLeader.areaPath.path, "AreaId");
                }

                //Get Loot into Encounter
                Encounter enc = new Encounter("Loot", partyLeader);

                (string[] kills, long koinsToGain, long xpToGain) = GetKillRewards(enc);

                var invLoot = enc.loot.inv;

                string lootDisplay = null;
                if (party != null)
                {
                    koinsToGain = NumbersM.CeilParse<long>(koinsToGain / (party.MemberCount + 0.00));
                    lootDisplay += $"+{koinsToGain} Kutsyei Coins Per Party Member." + Environment.NewLine;
                }
                else
                    lootDisplay += $"+{koinsToGain} Kutsyei Coins " + Environment.NewLine;

                string specialCurrencyReward = currentencounter.Name == Encounter.Names.Bounty ? AMIData.Events.OngoingEvent.Ongoing.BountyReward : null;
                if (specialCurrencyReward != null) lootDisplay += $"+1 {specialCurrencyReward}" + Environment.NewLine;

                foreach (var cb in playerParty)
                {
                    PerkLoad.CheckPerks(cb.character, Perk.Trigger.EndFight, cb.character);
                    
                    bool hasDied = cb.character.IsDead();
                    if(!hasDied) cb.character.KCoins += koinsToGain;

                    if (cb.character is Player player)
                    {
                        
                    }
                    else if (cb.character is NPC follower)
                    {
                        lootDisplay += $" |-|{(follower.IsPet() ? follower.displayName : follower.name)} +{Utils.Display(follower.XPGain(xpToGain, follower.level))} XP {Environment.NewLine}";

                        //NPC looting
                        if(follower.IsPet())
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
                if(party != null)  enc.Save();
                currentencounter = null;

                FinalizeResultEmbed(invLoot, result, lootDisplay);
                msgType = MsgType.Loot;
            }
            else
            {
                //Continue combat
                msgType = MsgType.Combat;
                foreach (var cb in playerParty)
                    if (cb.character is Player player)
                        player.SaveFileMongo();
            }
            if (party != null)
            {
                if(endDungeon) party.areaKey = ParentAreaPath(currentArea, partyLeader.areaPath.floor);
                await party.SaveData();
                if(currentencounter != null && !allPlayersDead)
                    currentencounter.Save();
            }
            return msgType;
        }

        AreaPath ParentAreaPath(Area area, int floor = 0)
            => new AreaPath()
            {
                path = area.GeneratePath(false) + area.parent,
                name = area.parent,
                floor = floor
            };

        internal void FollowerCheck(NPC n, Party party, bool allPlayersDead)
        {
            int s = n.HealthStatus(out string status);
            /// >= 0 Is alive
            /// -1 is Down, -2 Fainted, -3 Unconscious, -4 Dead, -5 Vaporized
            if (s < -1 || allPlayersDead)
            {
                if (n.IsPet())
                {
                    if(party != null && s <= -4)
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

        */




    }
}
