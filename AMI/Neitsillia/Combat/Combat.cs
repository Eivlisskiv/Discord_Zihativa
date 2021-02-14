using AMI.Methods;
using AMI.Neitsillia.Items;
using AMI.Neitsillia.Items.Abilities;
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
    }
}
