using AMI.Methods;
using AMI.Neitsillia.Items;
using AMI.Neitsillia.Items.Perks.PerkLoad;
using AMI.Neitsillia.NPCSystems;
using AMI.Neitsillia.User.PlayerPartials;
using System;
using System.Collections.Generic;

namespace AMI.Neitsillia.Combat
{
    public class CombatResult
    {
        internal string Name => character is NPC n && n.IsPet() ? n.displayName : character.name; 
        internal enum Action { Cast, Missed, Escape, FailedEscape, Consume, Exhausted, Paralyzed }
        internal enum Team { P, M }
        //
        internal Team charTeam;
        internal Combat combat;
        internal Random rng;
        internal string position;
        internal string target = "Default";
        internal CharacterMotherClass character;
        //
        internal Ability abilityUsed;
        internal int bonusStaminaDrain;
        //
        internal int baseHitChance;
        internal int additiveBonusHitChance;
        internal int multiplicativeBonusHitChance;
        internal bool sentHit;

        internal bool SentHit => sentHit && action == Action.Cast;
        //
        internal Action action = Action.Cast;
        private string[] results = new string[2];
        //
        internal int bonusDodgeChances;
        //
        internal bool isStealth;
        internal bool isBaseCritical;
        internal bool isCritical;
        internal double CriticalMultiplier;
        //
        internal double bonusAdditiveCritChance;
        internal double bonusMutiplicativeCritChance;
        //
        internal double bonusAdditiveCritMultiplier;
        internal double bonusMutiplicativeCritMultiplier;
        //
        internal long[] baseDamage = new long[ReferenceData.DmgType.Length];
        internal long[] bonusDamage = new long[ReferenceData.DmgType.Length];
        internal double[] damageMultiplier = new double[ReferenceData.DmgType.Length];
        internal long[] selfDamageTaken = new long[ReferenceData.DmgType.Length];
        //
        internal long totalUnresistedDamage = 0;
        internal int[] baseResistance = new int[ReferenceData.DmgType.Length];
        internal int[] bonusResistance = new int[ReferenceData.DmgType.Length];
        internal double[] resistanceMultiplier = new double[ReferenceData.DmgType.Length];
        //
        internal List<string> perkProcs = new List<string>();
        //
        internal CombatResult(CharacterMotherClass arg1, Ability arg2, Random arg3, Combat c, Team t)
        {
            combat = c;
            character = arg1; abilityUsed = arg2; rng = arg3;
            if (abilityUsed == null && character is Player player)
                switch (player.duel.abilityName)
                {
                    case "~Consumed":
                    case "~consumed":
                        action = Action.Consume; break;
                    case "~Run": action = Action.FailedEscape;  break;
                }
            for (int i = 0; i < ReferenceData.DmgType.Length; i++)
            {
                baseDamage[i] = arg1.Damage(i);
                baseResistance[i] = arg1.Resistance(i);
            }
        }
        internal void CalculateHitChanceModifier()
        {
            if (abilityUsed != null)
                baseHitChance = abilityUsed.Agility(character.Agility(), character.Efficiency());
            else baseHitChance = -100;
        }
        internal void CalculateBaseCrtiChance()
        {
            double critChance = abilityUsed.CritChance(character.CritChance(), character.stats.Efficiency());
            int x = rng.Next(101);
            if (critChance >= 100)
                isBaseCritical = true;
            else if (x < critChance)
                isBaseCritical = true;
        }
        double GetCritChance()
        {
            return (abilityUsed.CritChance(character.CritChance(), character.stats.Efficiency())
                * (1 + multiplicativeBonusHitChance)) + bonusAdditiveCritChance;
        }
        internal void CalculateCriticalMultiplier()
        {
            if (abilityUsed != null)
            {
                double critChance = GetCritChance();
                double critDamage = abilityUsed.CritMult(character.CritChance(), character.stats.Efficiency());
                CriticalMultiplier = 0;
                int x = rng.Next(101);
                while (critChance > 0 && critDamage > 0 && (critChance > 100 || x < critChance))
                {
                    CriticalMultiplier += (critDamage / 100.00);
                    critChance -= 100.00;
                    critDamage -= 50;
                    x = rng.Next(100);
                }
                if (CriticalMultiplier > 0)
                    isCritical = true;
                if (isStealth)
                    CriticalMultiplier += 1;
            }
        }
        internal void CalculateBaseAbilityEffect()
        {
            if (abilityUsed != null)
            {
                for (int i = 0; i < baseDamage.Length; i++)
                    baseDamage[i] = abilityUsed.Damage(i, character.Damage(i), character.stats.Efficiency());
            }
        }
        internal void Initiate()
        {
            if (character is Player)
                target = ((Player)character).duel.target;
            PerkLoad.CheckPerks(character, Perk.Trigger.Turn, this);
            CalculateHitChanceModifier();
            CalculateBaseAbilityEffect();
        }

        internal void ExecuteSendingTurn(CombatResult target)
        {
            character.StaminaE(Collections.Stats.passiveSp * character.stats.GetDEX());
            if (abilityUsed != null && action == Action.Cast)
            {
                PerkLoad.CheckPerks(character, Perk.Trigger.BeforeOffense, this, target);
                PerkLoad.CheckPerks(target.character, Perk.Trigger.BeforeDefense, target, this);
                //
                CalculateCriticalMultiplier();
                switch (abilityUsed.type)
                {
                    case Ability.AType.Martial:
                    case Ability.AType.Elemental:
                    case Ability.AType.Enchantment:
                    case Ability.AType.Tactical:
                        sentHit = IsHit(-(target.character.Agility() + target.bonusDodgeChances));
                        break;
                    case Ability.AType.Defensive:
                        sentHit = IsHit(target.character.Agility() +
                            target.bonusDodgeChances);
                        break;
                }
                //
                int spDrain = -(abilityUsed.StaminaDrain() + bonusStaminaDrain);
                if (character.stamina + spDrain < 0)
                {
                    action = Action.Exhausted;
                    character.stamina = -(spDrain) / 2;
                }
                character.StaminaE(spDrain);
                //
                PerkLoad.CheckPerks(character, Perk.Trigger.Offense, this, target);
                PerkLoad.CheckPerks(target.character, Perk.Trigger.Defense, target, this);

                abilityUsed.InvokeEffect(this, target);
                if (SentHit)
                {
                    switch (abilityUsed.type)
                    {
                        case Ability.AType.Martial:
                        case Ability.AType.Elemental:
                        case Ability.AType.Enchantment:
                            {
                                for (int i = 0; i < baseDamage.Length; i++)
                                    totalUnresistedDamage += NPCCombat.ElementalResistance(
                                        target.GetTotalResistance(i),
                                        GetTotalDamage(i));

                                results[0] = $"Dealt" +
                                $" {target.character.TakeDamage(GetDamageDealt(totalUnresistedDamage))}" +
                                $" => {target.Name}";

                                if (abilityUsed.type != Ability.AType.Elemental && character.equipment.weapon != null)
                                    IMethods.Condition(character.equipment.weapon, character.stats.endurance);
                                IMethods.AllArmorCND(target.character);
                            }
                            break;
                        default: results[0] = $"{abilityUsed.name} => {target.Name}";break;
                    }
                    if (isCritical)
                        results[0] += " [CRIT]";

                    long axp = target.character.XPDrop((abilityUsed.level * abilityUsed.tier) + (character.level / 10));
                    abilityUsed.GainXP(axp, 1);
                    if (abilityUsed.type == Ability.AType.Elemental) character.specter?.GainXP(axp);
                }
                if (!sentHit && action == Action.Cast) action = Action.Missed;
            }
        }
        bool IsHit(int modifier)
        {
            int hitChance = Verify.MinMax(baseHitChance + modifier, ReferenceData.maximumAgilityDifference);
            hitChance += rng.Next(0, 101);
            return (hitChance >= (100 - ReferenceData.hitChance));
        }
        internal long GetTotalDamage(int i) 
            => Math.Max(NumbersM.FloorParse<long>(Math.Abs(baseDamage[i]) 
                * (float)damageMultiplier[i])
               + baseDamage[i] + bonusDamage[i], 0);

        internal int GetTotalResistance(int i)
            =>  NumbersM.FloorParse<int>(Math.Abs(baseResistance[i])
                * resistanceMultiplier[i])
               + baseResistance[i] + bonusResistance[i];

        internal long GetDamageDealt(long totalDamage)
        {
            return Math.Max(NumbersM.NParse<long>(totalDamage * (GetTotalCriticalMultiplier() + 1)), 0);
        }
        internal double GetTotalCriticalMultiplier()
        {
            if (isCritical)
                return (CriticalMultiplier * (bonusMutiplicativeCritChance + 1)) + bonusAdditiveCritMultiplier;
            return 0;
        }

        internal string GetResultInfo(string targetIndex)
        {
            string r = $"**{Name}** | `{targetIndex}` |" +
                Environment.NewLine +
                GetAction();
            character.HealthStatus(out string temp, false);
            r += temp + Environment.NewLine;
            if (character is Player)
                r += character.StaminaStatus() + Environment.NewLine;
            if (perkProcs.Count > 0)
                r += $"`➤ {string.Join($"{Environment.NewLine}➤ ", perkProcs)}`" + Environment.NewLine;
            r += results[1];

            string eqCND = character.equipment.VerifyCND();
            if (character is Player && eqCND != null)
                r += Environment.NewLine + eqCND;
            return r;
        }
        internal string GetAction()
        {
            switch (action)
            {
                case Action.Consume: return "Consumed Item" + Environment.NewLine;
                case Action.Escape: return "Ran Away" + Environment.NewLine;
                case Action.FailedEscape: return "Failed Escape" + Environment.NewLine;
                //
                case Action.Paralyzed: return (results[0] ?? action.ToString()) + Environment.NewLine;

                case Action.Exhausted:
                case Action.Missed: return action.ToString() + Environment.NewLine;
                default:
                    if (results[0] != null)
                        return $" `{results[0]}`" + Environment.NewLine;
                    else return null;
            }
        }
        internal void FilterNonCastAction(string actionName)
        {
            switch (actionName)
            {
                case "~Run": action = Action.Escape; break;
                case "~Consume": action = Action.Consume; break;
            }
        }

        internal CombatResult[] GetAllies()
        {
            switch (charTeam)
            {
                case Team.M: return combat.mobParty;
                case Team.P: return combat.playerParty;
            }
            return null;
        }

        internal CombatResult[] GetEnemies()
        {
            switch (charTeam)
            {
                case Team.M: return combat.playerParty;
                case Team.P: return combat.mobParty;
            }
            return null;
        }

        public void Paralyse(string result = "Paralyzed")
        {
            action = Action.Paralyzed;
            results[0] = result;
        }
    }
}
