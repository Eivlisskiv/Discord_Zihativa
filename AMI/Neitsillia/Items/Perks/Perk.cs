using AMI.Methods;
using AMI.Neitsillia.Collections;
using AMI.Neitsillia.Combat;
using AMI.Neitsillia.Items.Abilities;
using AMI.Neitsillia.Items.Perks.PerkLoad;
using AMI.Neitsillia.NPCSystems;
using AMI.Neitsillia.User.PlayerPartials;
using AMYPrototype;
using Neitsillia.Items.Item;
using Newtonsoft.Json;
using System;
using System.Linq;

namespace AMI.Neitsillia.Items
{
    public partial class Perk
    {
        #region Instance
        public static int RNG(int max) { return Program.rng.Next(max); }
        public enum Trigger
        {
            Null,
            BeforeDefense, Defense, BeforeOffense, Offense,
            StartFight, EndFight, Kill, Death, //Combat
            Health, Healing, Stamina, StaminaAmount, MaxHealth,
            Turn, g, h, i, j,
            Crafting, Upgrading, k, l, m, //customization
            GainXP, n, o, p, q, //Progression
            r, s, t, u, v,
            w, x, y, z, aa,
            ab, ac, ad, ae, af,
        };
        public enum StatusType
        {
            None, Positive, Negative, Blessing, Curse
        }
        public string name;
        public string desc;
        public int tier;
        public Trigger trigger;
        public Trigger end;
        public StatusType type;
        //
        public string data;
        public int rank = 0;
        public int maxRank = 0;
        //
        internal CharacterMotherClass user;
        public override string ToString()
        {
            string s = name;
            if (maxRank > 0)
                s += $" {rank}/{maxRank}";
            return s;
        }
        string GetFunc()
        { return name.Replace(" ", ""); }
        public Perk(string aname)
        { name = aname; }
        public Perk(string aName, int aTier = 0, int aRank = 0, int? aMaxRank = null)
        { name = aName; tier = aTier; rank = aRank; maxRank = aMaxRank ?? aRank; }
        [JsonConstructor]
        public Perk() { }

        internal object[] Run(CharacterMotherClass player, bool end,
            object[] parameters)
        {
            user = player;
            //return Utils.RunMethod<T>(GetFunc(), this, parameter);
            string funcName = GetFunc();
            if (end)
                funcName += "_End";
            var function = Utils.GetFunction(typeof(Perk), funcName);
            try
            {
                return (object[])function.Invoke(this, parameters);
            } catch (Exception e)
            {
                Log.LogS(e);
                _ = Handlers.UniqueChannels.Instance.SendToLog($"Perk \"{name}\" of {user} Method \"{funcName}\" Error: {e.Message}");
            }
            return parameters;
        }

        #endregion

        public static bool SigilCheck(string diety, Item[] jewelries)
        {
            foreach(Item jewel in jewelries)
            {
                if (jewel != null && jewel.isUnique && Religion.Faith.Sigils.ContainsKey(jewel.originalName) 
                    && Religion.Faith.Sigils[jewel.originalName].diety == diety)
                    return true;
            }
            return false;
        }

        #region Shared Effects
        public static object[] ElementalStatus(Perk status, CombatResult owner, int elementIndex)
        {
            long damage = NPCCombat.ElementalResistance(
            owner.character.Resistance(elementIndex), 2 * status.tier);
            owner.character.TakeDamage(damage);
            status.rank--;
            owner.perkProcs.Add($"Suffered {damage} {ReferenceData.DmgType[elementIndex]} Damage");
            return new object[] { owner };
        }

        public static object[] ElementalWeaponPerk(CombatResult owner, CombatResult target, int elementIndex)
        {
            long otherElementalDamage =
                Enumerable.Sum(owner.baseDamage) - owner.baseDamage[elementIndex];
            long bonus = NumbersM.FloorParse<long>(otherElementalDamage * 0.05);
            if (bonus > 0)
            {
                owner.bonusDamage[elementIndex] += bonus;
                owner.perkProcs.Add($"+{bonus} {ReferenceData.DmgType[elementIndex]} Damage");
            }
            return new object[] { owner, target };
        }

        public static object[] InfectionEffect(CombatResult owner, CombatResult target,
            int chances, string infection, int durationMultiplier, int intensityDevider)
        {
            int lvl = owner.character.level;
            if (Program.Chance(chances - (target.character.Resistance(3) / 100)))
                target.character.Status(infection, lvl * durationMultiplier,
                    Verify.Min((lvl * lvl) / intensityDevider, 5));
            return new object[] { owner, target };
        }

        public static object[] Shared_Dodge(CombatResult owner, CombatResult target,
            int chance, params Ability.AType[] fromtypes)
        {
            if (target.SentHit &&
            (fromtypes.Length == 0 || target.abilityUsed.IsType(fromtypes))
            && Program.Chance(chance))
            {
                target.sentHit = false;
                owner.perkProcs.Add($"Dodged {target.Name}'s attack");
            }
            return R(owner, target);
        }

        public object[] TurnPassed(params object[] args)
        {
            rank--;
            return args;
        }
        #endregion

        #region Race Perks
        public object[] IreskianTalent(Item item)
        {
            Random rng = Program.rng;
            int x = rng.Next(8);
            Player player = (Player)user;
            switch (x)
            {
                case 0:
                case 1:
                    {
                        if (item.type == Item.IType.Weapon)
                        {
                            int i = rng.Next(item.damage.Length);
                            if (item.damage[i] > 0)
                                item.damage[i] = NumbersM.CeilParse<long>(item.damage[i] * 1.5);
                            else if (item.damage[i] < 0)
                                item.damage[i] /= 2;
                            else
                                item.damage[i] += Verify.MinMax(item.tier / 2, player.level, 5);
                        }
                        else
                        {
                            int i = rng.Next(item.resistance.Length);
                            if (item.resistance[i] > 0)
                                item.resistance[i] = NumbersM.CeilParse<int>(item.resistance[i] * 1.5);
                            else if (item.resistance[i] < 0)
                                item.resistance[i] /= 2;
                            else
                                item.resistance[i] += Verify.MinMax(item.tier / 2, player.level, 5);

                        }
                    }
                    break;
                case 2:
                    {
                        if (item.type != Item.IType.Weapon)
                        {
                            if (item.healthBuff > 0)
                                item.healthBuff += NumbersM.CeilParse<long>(item.healthBuff * 1.5);
                            else
                                item.healthBuff += Verify.MinMax(item.tier / 2, player.level / 2, 2);
                        }
                        else
                            item.durability += Verify.MinMax(item.tier / 2, player.level, 5);
                    }
                    break;
                case 3:
                    {
                        if (item.agility > 0)
                            item.agility *= 2;
                        else
                            item.agility += Verify.MinMax(item.tier / 2, player.level, 2);
                    }
                    break;
                case 4:
                    {
                        if (item.critChance > 0)
                            item.critChance *= 2;
                        else
                            item.critChance += Verify.MinMax(item.tier / 2, player.level, 5);
                    }
                    break;
                case 5:
                    {
                        if (item.critMult > 0)
                            item.critMult *= 2;
                        else
                            item.critMult += Verify.MinMax(item.tier / 2, player.level, 5);
                    }
                    break;
                case 6:
                    {
                        if (item.durability > 0)
                            item.durability *= 2;
                        else
                            item.durability += Verify.MinMax(item.tier / 2, player.level * 2, 5);
                    }
                    break;
                case 7:
                    {
                        if (item.staminaBuff > 0)
                            item.staminaBuff *= 2;
                        else
                            item.staminaBuff += Verify.MinMax(item.tier / 2, player.level * 2, 5);
                    }
                    break;
            }
            int or = item.CalculateStats();
            if (item.tier > or)
                item.baseTier += (item.tier - or);
            return new object[] { item };
        }
        public object[] IreskianTalent_End(Item item, int matamount, int matTier)
        {
            if (RNG(100) + matamount > 80)
            {
                int i = RNG(5 + ReferenceData.DmgType.Length);
                if (item.type == Item.IType.Weapon && i == 0)
                    i++;
                int or = item.Upgrade(1, i);
                if (item.tier > or)
                    item.baseTier += (item.tier - or);
            }
            return new object[] { item };
        }
        public object[] UskavianLearning(CombatResult owner, CombatResult enemy)
        {
            if (owner.abilityUsed != null)
            {
                if (owner.abilityUsed.name == data && rank < 10)
                {
                    rank++;
                    owner.perkProcs.Add($"{name} > +{rank * 10.00}% Damage");
                }
                else
                {
                    rank = 0;
                    data = owner.abilityUsed.name;
                }


                Array.ForEach(owner.damageMultiplier, (double d) => {
                    d += (rank / 10.00);
                });
            }
            return new object[] { owner, enemy };
        }
        public object[] UskavianLearning_End(CharacterMotherClass player)
        {
            data = "";
            rank = 0;
            return new object[] { player };
        }
        public object[] MiganaSkin(CombatResult owner, CombatResult enemy)
        {
            const int maxCharge = 5;
            const int resBonus = 10;
            if (enemy.SentHit)
            {
                if (maxRank == 0)//charges
                {
                    if (rank > 0 || Program.Chance(45))
                    {
                        rank++;
                        if (rank >= maxCharge)
                            maxRank = maxCharge;
                        owner.perkProcs.Add($"{owner.Name}'s skin is strengthening");
                    }
                }
                else //decharges
                {
                    owner.perkProcs.Add($"{owner.Name}'s armored skin is degrading [+{resBonus * rank} PHY RES]");
                    owner.bonusResistance[0] += resBonus * rank;
                    rank--;
                    if (rank == 0) maxRank = 0;
                }
            }
            return new object[] { owner, enemy };
        }
        public object[] HumanAdaptation(CombatResult owner, CombatResult enemy)
        {
            if (enemy.SentHit &&
                (enemy.abilityUsed.type == Ability.AType.Martial || enemy.abilityUsed.type == Ability.AType.Elemental))
            {
                if (rank == 5)
                {
                    user.stats.baseBuffs[int.Parse(data)] -= 1;
                    rank = 0;
                }
                if (rank == 0)
                {
                    int i = RNG(user.stats.baseBuffs.Length);
                    data = i.ToString();
                    user.stats.baseBuffs[i] += 1;
                    if (i == 0)
                        owner.character.health += Stats.hpPerEnd * 2;
                    owner.perkProcs.Add($"New {name} proc");
                }
                rank++;
            }
            return new object[] { owner, enemy };
        }

        public object[] TsiunTrickery(CharacterMotherClass user, CharacterMotherClass[] mobs)
        {
            Stats s = Utils.RandomElement(mobs).stats;
            int[] stats = new int[] { s.endurance, s.intelligence, s.strength, s.charisma,
            s.dexterity, s.perception};
            int i = NumbersM.HighestIndex(stats);
            data = i.ToString();
            user.stats.baseBuffs[i] += 2;
            if (i == 0)
                user.health += Stats.hpPerEnd * 2;
            return new object[] { user, mobs };
        }
        public object[] TsiunTrickery_End(CharacterMotherClass player)
        {
            if (data != null)
                player.stats.baseBuffs[int.Parse(data)] -= 2;
            data = null;
            return new object[] { player };
        }
        #endregion

        #region Perks

        #region Creatures

        public object[] PrickledSkin(CombatResult owner, CombatResult enemy)
        {
            if (enemy.abilityUsed != null && enemy.SentHit &&
                enemy.abilityUsed.type == Ability.AType.Martial)
            {
                long reflectedDamage = NumbersM.NParse<long>(enemy.baseDamage[0] *
                Verify.Max(owner.character.stats.GetEND() * 0.05, 0.5));
                if (reflectedDamage > 0)
                {
                    enemy.selfDamageTaken[0] += reflectedDamage;
                    owner.perkProcs.Add($"{owner.Name} returned {reflectedDamage} to {enemy.Name}");
                }
            }
            return new object[] { owner, enemy };
        }

        public object[] CarelessWhisper(CombatResult owner, CombatResult enemy)
        {
            if (enemy.abilityUsed != null &&
                (enemy.abilityUsed.type == Ability.AType.Martial || enemy.abilityUsed.type == Ability.AType.Elemental))
            {
                double mod = Verify.MinMax(((owner.character.stats.GetCHA() -
                    enemy.character.stats.GetCHA()) * 0.5), 20);
                if (mod > 0)
                {
                    enemy.multiplicativeBonusHitChance -= NumbersM.FloorParse<int>(mod);
                    owner.perkProcs.Add($"{owner.Name} reduced {enemy.Name}'s hit chance by {mod}%");
                }
            }
            return new object[] { owner, enemy };
        }

        public object[] InfectionCarrier(CombatResult owner, CombatResult target)
        {
            if (owner.abilityUsed.IsType(Ability.AType.Martial, Ability.AType.Enchantment))
                return InfectionEffect(owner, target, 10, "Flesh Curse", 5, 1);
            return R(owner, target);
        }
        public object[] InfectionCarrier_End(CombatResult owner, CombatResult target)
        {
            if (target.abilityUsed.IsType(Ability.AType.Martial, Ability.AType.Enchantment))
                return InfectionEffect(owner, target, 5, "Flesh Curse", 5, 1);
            return R(owner, target);
        }

        public object[] SharpClaws(CombatResult owner, CombatResult target)
        {
            if(owner.SentHit && Program.Chance(15) &&
                target.character.Status("MalformedArmor", 8, owner.character.stats.GetSTR()) == 1)
                    owner.perkProcs.Add($"Dented {target.Name}'s armor");

            return R(owner, target);
        }
        public object[] RascallyAttacks(CombatResult owner, CombatResult target)
        {
            if (owner.SentHit && Program.Chance(15))
                owner.perkProcs.Add($"Exhausted {target.character.StaminaE(50)} out of {target.Name}");
            return R(owner, target);
        }

        public object[] Evassive(CombatResult owner, CombatResult target)
        {
            return Shared_Dodge(owner, target, 25,
                Ability.AType.Enchantment, Ability.AType.Martial);
        }
        public object[] SixthSense(CombatResult owner, CombatResult target)
        {
            return Shared_Dodge(owner, target, 20,
                Ability.AType.Elemental, Ability.AType.Tactical);
        }

        public object[] HeadSick(object obj)
        {
            if (obj is CombatResult owner)
            {
                long mhp = owner.character.Health();
                if (owner.character.health > 0 && owner.character.health < mhp)
                    owner.damageMultiplier = ArrayM.AddEach(owner.damageMultiplier,
                        owner.character.health / mhp);
                return R(owner);
            }
            else
            {
                trigger = Trigger.Turn;
                return new object[] { obj };
            }
            
        }

        #endregion

        #region Weapon
        public object[] TrialAndError(CombatResult owner, CombatResult enemy)
        {
            if (owner.SentHit && data == "miss")
            {
                owner.damageMultiplier = ArrayM.AddEach(owner.damageMultiplier, 0.08);

                data = null;
                owner.perkProcs.Add($"{owner.Name} proc'ed {name} [+5% Damage]");
            }
            else if (!owner.SentHit && owner.action != CombatResult.Action.Exhausted)
            {
                if (data == "miss")
                    data = null;
                else
                    data = "miss";
            }
            else if (owner.SentHit && data == "crit")
            {
                owner.bonusAdditiveCritChance += 0.03;
                data = null;
                owner.perkProcs.Add($"{owner.Name} proc'ed {name} [+5% Critical Damage]");
            }
            else if (owner.SentHit && !owner.isCritical)
            {
                if (data == "crit")
                    data = null;
                else
                    data = "crit";
            }
            return new object[] { owner, enemy };
        }
        public object[] TrialAndError_End(CharacterMotherClass owner)
        {
            data = null;
            return new object[] { owner };
        }
        //Elemental
        public object[] Blazed(CombatResult owner, CombatResult enemy)
        {
            return ElementalWeaponPerk(owner, enemy, 1);
        }
        public object[] Icy(CombatResult owner, CombatResult enemy)
        {
            return ElementalWeaponPerk(owner, enemy, 2);
        }
        public object[] Toxic(CombatResult owner, CombatResult enemy)
        {
            return ElementalWeaponPerk(owner, enemy, 3);
        }
        public object[] Wired(CombatResult owner, CombatResult enemy)
        {
            return ElementalWeaponPerk(owner, enemy, 4);
        }
        public object[] Curare(CombatResult owner, CombatResult enemy)
        {
            if(owner.abilityUsed?.type == Ability.AType.Martial && Program.Chance(25))
            {
                enemy.character.Status("Paralysis", Program.rng.Next(5, 10), 45);
                owner.perkProcs.Add($"Induced paralysis to {enemy.Name}");
            }
            return new object[]{ owner, enemy};
        }
        #endregion

        #endregion

        #region Status
        //Elements
        public object[] Bleeding(CombatResult owner)
        {
            return ElementalStatus(this, owner, 0);
        }
        public object[] Burning(CombatResult owner)
        {
            return ElementalStatus(this, owner, 1);
        }
        public object[] Poisoned(CombatResult owner)
        {
            return ElementalStatus(this, owner, 3);
        }
        

        #region Resistance
        //Negative
        public object[] Punctured(CombatResult owner)
        {
            owner.bonusResistance[0] -= 10 * tier;
            return TurnPassed(owner);
        }
        public object[] MalformedArmor(CombatResult owner)
        {
            owner.resistanceMultiplier[0] -= tier/100.00;
            return TurnPassed(owner);
        }
        public object[] Decay(CombatResult owner)
        {
            owner.bonusResistance[0] -= tier * (maxRank - (rank + 1));
            return TurnPassed(owner);
        }
        //Positive
        public object[] Hardened(CombatResult owner)
        {
            owner.bonusResistance[0] += 20 * tier;
            return TurnPassed(owner);
        }

        public object[] ElementalResillience(CombatResult owner)
        {
            for(int i = 1; i < owner.bonusResistance.Length; i++)
                owner.bonusResistance[i] += tier;
            return TurnPassed(owner);
        }

        #endregion

        public object[] Vigorous(long bonus)
        {
            bonus += tier * 10;
            return TurnPassed(bonus);
        }

        public object[] Warded(CombatResult owner, CombatResult attacker)
        {
            if(attacker.sentHit && attacker.abilityUsed.IsType(Ability.AType.Martial, 
                Ability.AType.Enchantment))
            {
                attacker.sentHit = false;
                attacker.action = CombatResult.Action.Paralyzed;
                owner.perkProcs.Add($"Warded off {attacker.Name}'s attack");
                return TurnPassed(owner, attacker);
            }
            return R(owner, attacker);
        }

        public object[] Rigged(CombatResult owner) => TurnPassed(owner);
        public object[] Rigged_End(CombatResult owner, CombatResult target)
        {
            if (target.SentHit && target.abilityUsed.type != Ability.AType.Defensive &&
            target.abilityUsed.type != Ability.AType.Tactical)
            {
                double bonus = (tier * (maxRank - rank)) / 100.00;
                for (int i = 0; i < target.damageMultiplier.Length; i++)
                    target.damageMultiplier[i] += bonus;
                target.perkProcs.Add($"{target.Name} gained {bonus * 100}% DMG on {owner.Name}");
                rank -= rank;
            }
            return new object[] { owner, target };
        }

        #region Charging
        //Positives
        public object[] PatientRequite(CombatResult owner) => TurnPassed(owner);
        public object[] PatientRequite_End(CombatResult owner, CombatResult target)
        {
            if (rank > 0 && target.SentHit && target.abilityUsed.type != Ability.AType.Defensive &&
            target.abilityUsed.type != Ability.AType.Tactical)
            {
                long bonus = (tier * 2) * (maxRank - rank);
                bonus = NPCCombat.ElementalResistance(
                    target.GetTotalResistance(0), bonus);
                target.character.TakeDamage(bonus);
                owner.perkProcs.Add($"{owner.Name} returned {bonus} DMG > {target.Name}");
                rank = 0;
            }
            return new object[] { owner, target };
        }

        public object[] KeenEvaluation(CombatResult owner)
        {
            if (owner.abilityUsed.IsOffense)
            {
                int buff = (maxRank - rank);
                owner.bonusAdditiveCritChance += buff;
                owner.bonusAdditiveCritMultiplier += (0.02) * buff;
                rank = 0;
                owner.perkProcs.Add($"+{buff} CC & +{buff * 0.02}x CD");
            }
            return TurnPassed(owner);
        }

        #endregion
        public object[] Suppressed(CombatResult owner, CombatResult target)
        {
            if (target.SentHit && target.abilityUsed.type != Ability.AType.Defensive &&
            target.abilityUsed.type != Ability.AType.Tactical)
            {
                double bonus = tier / 100.00;
                owner.damageMultiplier = ArrayM.AddEach(owner.damageMultiplier, -bonus);
                owner.perkProcs.Add($"-{tier}% DMG");
            }
            return new object[] { owner, target };
        }
        public object[] Suppressed_End(CombatResult owner) => TurnPassed(owner);

        public object[] EnergyLeak(CombatResult owner, CombatResult target)
        {
            if (target.SentHit && target.abilityUsed.type != Ability.AType.Defensive &&
            target.abilityUsed.type != Ability.AType.Tactical)
            {
                //$"When hit, attacker has {}% chance to drain {tier*5} stamina"
                if (Program.Chance(35 + tier) && owner.character.stamina > 0)
                {
                    int stolenSP = Verify.Max(tier * 8, owner.character.stamina);
                    target.character.StaminaE(stolenSP);
                    owner.character.StaminaE(-stolenSP);
                    owner.perkProcs.Add($"Leaked {stolenSP} SP to {target.Name}");
                }
            }
            return new object[] { owner, target };
        }
        public object[] EnergyLeak_End(CombatResult owner) => TurnPassed(owner);

        public object[] Discourage(CombatResult owner, CombatResult target)
        {
            if (target.SentHit && target.abilityUsed.IsOffense
                && Program.Chance(35 + tier))
            {
                target.character.Status("Suppressed", 4 + (tier / 10), tier * 2);

                return TurnPassed(owner, target);
            }
            return new object[] { owner, target };
        }

        public object[] HospitableAura(CombatResult owner, CombatResult target)
        {
            if (target.SentHit && target.abilityUsed.type == Ability.AType.Defensive
                && Program.Chance(tier * 5))
            {
                target.character.Status("ElementalResillience", 2 + (tier / 8), tier + 10);

                if (int.TryParse(data, out int charge) && charge > 1) data = $"{charge - 1}";
                else rank = 0;
            }
            return new object[] { owner, target };
        }
        public object[] HospitableAura_End(CombatResult owner) => TurnPassed(owner);

        public object[] Merciless(CombatResult owner, CombatResult target)
        {
            owner.damageMultiplier[0] += (tier * 0.01);
            return new object[] { owner, target };
        }
        public object[] Merciless_End(CombatResult owner) => TurnPassed(owner);

        public object[] Focused(CombatResult owner, CombatResult target)
        {
            owner.bonusMutiplicativeCritChance += (tier * 0.015);
            return new object[] { owner, target };
        }
        public object[] Focused_End(CombatResult owner) => TurnPassed(owner);

        public object[] CriticalWindow(CombatResult owner, CombatResult target)
        {
            owner.bonusAdditiveCritMultiplier += (tier * 0.05);
            return new object[] { owner, target };
        }
        public object[] CriticalWindow_End(CombatResult owner) => TurnPassed(owner);
        //
        public object[] Recovering(CombatResult owner)
        {
            owner.perkProcs.Add($"Recovered {owner.character.Healing(tier * 5)}HP");
            rank--;
            return new object[] { owner };
        }
        public object[] FullBreaths(CombatResult owner)
        {
            int msp = user.Stamina();
            user.stamina += Math.Min(NumbersM.CeilParse<int>(Math.Max(msp * .05, 1)), msp - user.stamina);
            rank--;
            return new object[] { owner };
        }
        //Diseases
        public object[] FleshCurse(CombatResult owner)
        {
            owner.perkProcs.Add("Suffered "
                + owner.character.TakeDamage(rank, ReferenceData.DamageType.Toxic) +
                " Damage from Flesh Curse");
            return R(owner);
        }
        #endregion

        #region Blessings
        public object[] BlessingOfAvlimia(CombatResult owner, CombatResult target)
        {
            if (owner.abilityUsed.IsType(Ability.AType.Martial, Ability.AType.Enchantment, Ability.AType.Elemental) &&
                owner.sentHit && Program.Chance(30 + (tier / 5)) && 
                SigilCheck("Avlimia", owner.character.equipment.jewelry))
            {
                Perk punctured = target.character.status.Find(x => x.name == "Punctured");
                if (punctured == null)
                {
                    punctured = PerkLoad.Effect("Punctured", 5, 1);
                    target.character.status.Add(punctured);
                }
                else
                {
                    punctured.tier++;
                    punctured.rank = 5;
                }

                target.perkProcs.Add("-10 PHY RES");

                return TurnPassed(owner, target);
            }

            return R(owner, target);
        }

        public object[] BlessingOfBakora(CombatResult owner, CombatResult target)
        {
            if (owner.abilityUsed.type == Ability.AType.Defensive &&
                owner.sentHit && Program.Chance(30 + (tier/5)) &&
                SigilCheck("Bakora", owner.character.equipment.jewelry))
            {
                target.character.Status("Warded", 1, 100);
                owner.perkProcs.Add("Warded " + target.Name);

                return TurnPassed(owner, target);
            }

            return R(owner, target);
        }
        #endregion

        #region SpecsPerks
        //*// Blacksmith
        public object[] BuiltToLast(Item item)
        {
            if (item.CanBeEquip())
            {
                int buff = user.stats.GetEND() + user.stats.GetINT() + 24;
                int bonus = NumbersM.CeilParse<int>(item.durability * (buff / 100.00));
                item.durability += bonus;
                item.condition += bonus;
                item.CalculateStats(true);
            }
            return new object[] { item };
        }
        public object[] ReinforcedMaterials(Item item)
        {
            if (item.CanBeEquip())
            {
                int buff = (user.stats.GetEND() + user.stats.GetINT()) * (user.level / 5);
                item.condition += buff;
            }
            return new object[] { item };
        }
        public object[] FinishingTouch(Item item, int matamount, int matTier)
        {
            if (user.level * 5 < item.tier + 1)
                return new object[] { item };
            Random rng = Program.rng;
            int d = ((user.level * 5) - (item.baseTier + 15));
            if (Program.Chance(d))
                item.baseTier += Verify.MinMax(matamount/5, d/5, 1);
            return new object[] { item };
        }
        //*// Healer
        public object[] Adrenaline(long healing)
        {
            if (healing > 0)
            {
                int rng = RNG(101);
                if (rng >= 85 - user.stats.GetINT())
                {
                    double staminaReturn = 0.20 + (user.stats.GetINT() / 100.00);
                    int amount = NumbersM.CeilParse<int>((int)healing * staminaReturn);
                    user.stamina += Verify.Max(amount, user.Stamina() - user.stamina);
                }
            }
            return new object[] { healing };
        }
        public object[] EnergizingTouch(CombatResult owner, CombatResult target)
        {
            //Energizing Touch
            if(owner.SentHit && RNG(101) <= 
                20 + owner.character.stats.GetINT() &&
                owner.abilityUsed.type == Ability.AType.Defensive)
            {
                target.character.Status("Full Breaths",
                    8 + (target.character.stats.GetDEX() / 5),
                    1);
            }
            return new object[] { owner, target };
        }
        public object[] Adaptation(CombatResult owner, CombatResult target)
        {
            //Energizing Touch
            if (target.abilityUsed.IsType(Ability.AType.Martial, Ability.AType.Enchantment,
                Ability.AType.Elemental))
            {
                if (rank >= maxRank)
                {
                    rank = 0;
                    data = null;
                    if (Program.Chance(30))
                        owner.perkProcs.Add($"Adaptation perk reset, healing " +
                            $"{owner.character.PercentHealing(10 + owner.character.stats.GetINT())} HP");
                }
                else if (int.TryParse(data, out int i))
                {
                    if (Program.Chance(40))
                        rank++;
                }
                else if (Program.Chance(40))
                {
                    i = 0;
                    for (int k = 0; k < target.baseDamage.Length; k++)
                        if (target.baseDamage[k] > target.baseDamage[i])
                            i = k;
                    data = i.ToString();
                    rank = 1;
                }
            }
            return new object[] { owner, target };
        }
        public object[] Adaptation_End(CombatResult owner, CombatResult target)
        {
            if (int.TryParse(data, out int i))
                owner.bonusResistance[i] += rank * 50;

            return new object[] { owner, target };
        }
        //*// Fighter
        public object[] UnstoppableForce(CombatResult owner, CombatResult target)
        {
            if (owner.SentHit && ((int)owner.abilityUsed.type <= 2 )
                && Program.Chance(15 + owner.character.stats.GetDEX()))
            {
                float minHealthToProc = owner.character.Health() * 0.30f;
                if (owner.character.health <= minHealthToProc)
                {
                    int regenerated = owner.character.StaminaE(
                        (int)(Enumerable.Sum(owner.baseDamage) / 4));
                    owner.perkProcs.Add($"Recovered {regenerated} stamina.");
                }
            }
            return new object[] { owner, target };
        }
        public object[] PrecisionEnhancement(CombatResult owner, CombatResult target)
        {
            if (owner.isCritical)
            {
                double cc = owner.character.CritChance();
                double chances = cc <= 30 ? 90 : cc >= 90 ? 20 : 100 - cc;
                if (RNG(101) <= chances)
                {
                    int intensity = 1 + (owner.character.stats.GetSTR() / 5);
                    if (owner.character.Status("Critical Window", 3, intensity) > 0)
                        owner.perkProcs.Add($"{owner.Name} gained +{intensity * 5}% Critical Damage for 3 turns.");
                }
            }
            return new object[] { owner, target };
        }
        public object[] FightingSpirit(CombatResult owner, CombatResult target)
        {
            if (!owner.sentHit)
            {
                owner.perkProcs.Add(owner.Name + " Hyped up");
                rank = 1;
            }
            else if (rank > 0) owner.bonusMutiplicativeCritChance += 1;

            return new object[] { owner, target };
        }
        public object[] FightingSpirit_End(CombatResult owner) => TurnPassed(owner);
        #endregion

        #region Unique

        public object[] BambooSnack(CombatResult owner, CombatResult target)
        {
            if(owner.abilityUsed.type == Ability.AType.Defensive)
            {
                if(Program.Chance(owner == target ? 10 : 20))
                {
                    target.character.Status("Vigorous", 8, NumbersM.CeilParse<int>(owner.character.Health(false) / 100.00));
                    target.perkProcs.Add("Received a bamboo snack from " + owner.Name);
                }
            }
            return R(owner, target);
        }


        #endregion

        static object[] R(params object[] a) => a;
    }
}
