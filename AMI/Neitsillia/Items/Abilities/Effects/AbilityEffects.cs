using AMI.Methods;
using AMI.Neitsillia.Combat;
using AMI.Neitsillia.Items.Perks.PerkLoad;
using AMYPrototype;
using System;
using System.Collections.Generic;

namespace AMI.Neitsillia.Items.Abilities.Effects
{
    static partial class AbilityEffects
    {
        public static void Brawl(CombatResult caster, CombatResult target)
        {
            caster.bonusDamage[0] += caster.character.stats.GetSTR();
        }

        #region Starters

        #region Offense Tree
        public static void ToxicStrike(CombatResult caster, CombatResult target)
        {
            Ability toxicStrike = caster.abilityUsed;
            if (caster.SentHit && Program.Chance(10 + toxicStrike.level))
            {
                target.character.Status(toxicStrike.statusEffect ?? "Poisoned", 6, 
                    (int)Verify.Min(caster.character.TotalBaseDamage()/10, 2));
                caster.perkProcs.Add($"Applied Poisoned > {target.Name}");
            }
        }
        #endregion

        #region Vivace Tree
        public static void Vivace(CombatResult caster, CombatResult target)
        {
            if (caster.SentHit)
            {
                Ability vivace = caster.abilityUsed;
                int staminaDrained = Verify.Max(2 * vivace.level, 
                    target.character.stamina);
                target.character.StaminaE(-staminaDrained);
                caster.character.StaminaE(staminaDrained);
                caster.perkProcs.Add($"{caster.Name} drained {staminaDrained}SP from {target.Name}");
            }
        }
        public static void SpiritRip(CombatResult caster, CombatResult target)
        {
            Ability ability = caster.abilityUsed;
            if (caster.SentHit && Program.Chance(38 + ability.level))
            {
                target.character.Status(ability.statusEffect ?? "Energy Leak", ability.level,
                                    ability.level + 10);
                caster.perkProcs.Add($"{caster.Name} applied Energy Leak to {target.Name}");
            }
        }
        #endregion

        #region Sunder Tree
        public static void Sunder(CombatResult caster, CombatResult target)
        {
            if (caster.SentHit)
            {
                Ability sunder = caster.abilityUsed;
                if ((35 + sunder.level) <= Program.rng.Next(101))
                {
                    if (target.character.Status(sunder.statusEffect ?? "Punctured", 3, sunder.level) == 1)
                        caster.perkProcs.Add($"{caster.Name} Punctured {target.Name}");
                }
            }
        }
        public static void Blunt(CombatResult caster, CombatResult target)
        {
            Ability sunder = caster.abilityUsed;
            if (caster.SentHit && (35 + sunder.level) <= Program.rng.Next(101))
            {
                if (target.character.Status(sunder.statusEffect, 3 + (sunder.level / 5), 10 + sunder.level) == 1)
                    caster.perkProcs.Add($"{caster.Name} {sunder.statusEffect} {target.Name}");
            }
        }
        #endregion

        #region Shelter Tree
        public static void Shelter(CombatResult caster, CombatResult target)
        {
            if (caster.SentHit)
            {
                Ability shelter = caster.abilityUsed;
                int result = target.character.Status(shelter.statusEffect ?? "Hardened", 
                    (2 + (shelter.level/5)), shelter.level);
                if (result == 1)
                    caster.perkProcs.Add($"Hardened {target.Name}'s physical armor");
            }
        }
        public static void Heal(CombatResult caster, CombatResult target)
        {
            if (caster.SentHit)
            {
                Ability heal = caster.abilityUsed;
                long mhp = target.character.Health();
                if (target.character.HealthStatus(out string status) <= -4)
                    caster.perkProcs.Add($"{heal.name} is not powerful enough to have an effect on {status} targets");
                else if (target.character.health >= mhp)
                    caster.perkProcs.Add($"Healed {target.Name} for 0HP");
                else
                {
                    double percHealing = ((heal.level + 10 + caster.character.stats.GetINT())/100.00);
                    long maxOutput = NumbersM.NParse<long>(mhp * percHealing);
                    long healing = target.character.Healing(maxOutput, true);
                    caster.perkProcs.Add($"Healed {target.Name} for {healing}HP");
                }
            }
        }
        #endregion

        #region Counter Prep Tree
        public static void CounterPrep(CombatResult caster, CombatResult target)
        {
            if (caster.SentHit)
            {
                Ability counterPrep = caster.abilityUsed;
                target.character.Status(counterPrep.statusEffect ?? "Patient Requite", counterPrep.level,
                    counterPrep.level);
                caster.perkProcs.Add($"Applied Patient Requite to {target.Name}");
            }
        }
        public static void KeenEye(CombatResult caster, CombatResult target)
        {
            if (caster.SentHit)
            {
                Ability counterPrep = caster.abilityUsed;
                target.character.Status(counterPrep.statusEffect, 5 + (counterPrep.level / 10),
                    5 + (counterPrep.level / 10));
                caster.perkProcs.Add($"Applied Keen Evaluation to {target.Name}");
            }
        }
        #endregion

        #region Bold Stance Tree
        public static void BoldStance(CombatResult caster, CombatResult target)
        {
            if (caster.SentHit)
            {
                Ability boldStance = caster.abilityUsed;
                target.character.Status(boldStance.statusEffect ?? "Discourage",
                    1 + (boldStance.level / 20), boldStance.level);
                caster.perkProcs.Add($"Taught {target.Name} how to discourage their enemies.");
            }
        }

        public static void Cherish(CombatResult caster, CombatResult target)
        {
            if (caster.SentHit)
            {
                Ability boldStance = caster.abilityUsed;
                Perk status = PerkLoad.Effect(boldStance.statusEffect,
                    4 + (boldStance.level / 10), boldStance.level);
                status.data = $"{1 + (boldStance.level / 20)}";
                if(target.character.Status(status) == 1)
                    caster.perkProcs.Add($"Cherished {target.Name}");
            }
        }
        #endregion

        #region Sabotage Tree
        public static void Sabotage(CombatResult caster, CombatResult target)
        {
            if (caster.SentHit)
            {
                Ability sabotage = caster.abilityUsed;
                if (target.character.Status(sabotage.statusEffect ?? "Rigged",
                    2 + (sabotage.level / 5), sabotage.level) == 1)
                    caster.perkProcs.Add($"Rigged {target.Name}");
            }
        }
        public static void Termite(CombatResult caster, CombatResult target)
        {
            if (caster.SentHit)
            {
                Ability sabotage = caster.abilityUsed;
                if (target.character.Status(sabotage.statusEffect,
                    5 + (2 * (sabotage.level) / 10),  5 + (sabotage.level / 5)) == 1)
                    caster.perkProcs.Add($"Decaying {target.Name}'s physical armor");
            }
        }
        #endregion

        #region Snake oil Tree
        public static void SnakeOil(CombatResult caster, CombatResult target)
        {
            if (caster.SentHit)
            {
                Ability snakeOil = caster.abilityUsed;
                int x = Program.rng.Next(3);
                string effect = "Bleeding";
                switch (x)
                {
                    case 0: effect = "Bleeding"; break;
                    case 1: effect = "Burning"; break;
                    case 2: effect = "Poisoned"; break;
                }
                target.character.Status(effect, 3 + (int)Math.Floor(snakeOil.level / 5.00M),
                    (int)Math.Ceiling(snakeOil.level / 2.00M));
                caster.perkProcs.Add($"Applied {effect} > {target.Name}");
            }
        }
        public static void Ignite(CombatResult caster, CombatResult target)
        {
            if (caster.SentHit)
            {
                Ability a = caster.abilityUsed;

                long baseDmg = caster.character.TotalBaseDamage();
                int damage = NumbersM.FloorParse<int>(baseDmg * ((30 + a.level + caster.character.stats.GetINT())/100.00));
                if (damage < 20)
                    damage = 20;
                target.character.Status(a.statusEffect ?? "Burning", 5 + (int)Math.Floor(a.level / 6.00M),
                    damage / 2);
                caster.perkProcs.Add($"Applied Burning > {target.Name}");
            }
        }
        public static void Envenom(CombatResult caster, CombatResult target)
        {
            if (caster.SentHit)
            {
                Ability a = caster.abilityUsed;

                int damage = NumbersM.FloorParse<int>(caster.character.TotalBaseDamage() * ((30 + a.level + caster.character.stats.GetINT()) / 100.00));
                if (damage < 20)
                    damage = 20;

                target.character.Status(a.statusEffect ?? "Poisoned", 5 + (int)Math.Floor(a.level / 6.00M),
                    damage / 2);
                caster.perkProcs.Add($"Applied Poisoned > {target.Name}");
            }
        }
        public static void Undercut(CombatResult caster, CombatResult target)
        {
            if (caster.SentHit)
            {
                Ability a = caster.abilityUsed;

                int damage = NumbersM.FloorParse<int>(caster.character.TotalBaseDamage() * ((30 + a.level + caster.character.stats.GetINT()) / 100.00));
                if (damage < 20)
                    damage = 20;

                target.character.Status(a.statusEffect ?? "Bleeding", 5 + (int)Math.Floor(a.level / 6.00M),
                    damage / 2);
                caster.perkProcs.Add($"Applied Bleeding > {target.Name}");
            }
        }
        #endregion


        #endregion

        #region Elemental
        public static void Heat(CombatResult caster, CombatResult target)
        {
            if (caster.SentHit && Program.rng.Next(101) <= 10)
            {
                Ability heat = caster.abilityUsed;
                Perk status = target.character.status.Find(s => s.name == heat.statusEffect);

                if (status != null)
                {
                    status.tier += heat.tier;
                    caster.perkProcs.Add($"Worsened {target.Name}'s Burn");
                }
                else
                {
                    target.character.Status(heat.statusEffect, 1 + (heat.level / 10), 1 + (heat.level / 5));
                    caster.perkProcs.Add($"Burned {target.Name}");
                }
            }
        }

        public static void Blaze(CombatResult caster, CombatResult target)
        {
            if (caster.SentHit && Program.rng.Next(101) <= 20)
            {
                Ability heat = caster.abilityUsed;
                Perk status = target.character.status.Find(s => s.name == heat.statusEffect);

                if (status != null)
                {
                    status.tier += heat.tier;
                    caster.perkProcs.Add($"Worsened {target.Name}'s Burn");
                }
                else
                {
                    target.character.Status(heat.statusEffect, 1 + (heat.level / 10), 1 + (heat.level / 5));
                    caster.perkProcs.Add($"Burned {target.Name}");
                }
            }
        }

        public static void Toxin(CombatResult caster, CombatResult target)
        {
            if (caster.SentHit && Program.rng.Next(101) <= 10)
            {
                Ability toxin = caster.abilityUsed;
                Perk status = target.character.status.Find(s => s.name == toxin.statusEffect);

                if (status != null)
                {
                    status.tier += toxin.tier;
                    caster.perkProcs.Add($"Worsened {target.Name}'s poisoning");
                }
                else
                {
                    target.character.Status(toxin.statusEffect, 1 + (toxin.level / 10), 1 + (toxin.level / 5));
                    caster.perkProcs.Add($"Poisoned {target.Name}");
                }
            }
        }

        public static void Static(CombatResult caster, CombatResult target)
        {
            if (caster.SentHit && Program.rng.Next(101) <= 10)
            {
                Ability _static = caster.abilityUsed;
                Perk status = target.character.status.Find(s => s.name == _static.statusEffect);

                if (status != null)
                {
                    status.tier += _static.tier;
                    caster.perkProcs.Add($"Worsened {target.Name}'s paralysis");
                }
                else
                {
                    target.character.Status(_static.statusEffect, 1 + (_static.level / 10), 1 + (_static.level / 5));
                    caster.perkProcs.Add($"Induced paralysis to {target.Name}");
                }
            }
        }
        #endregion

        #region Specialties
        //Fighter
        public static void Execute(CombatResult caster, CombatResult target)
        {
            Ability a = caster.abilityUsed;
            if (caster.SentHit)
            {
                long xtraDamage = NumbersM.FloorParse<long>(caster.character.TotalBaseDamage() *
                    ((5 + a.level) / 100f));
                if (xtraDamage > 0)
                {
                    target.character.TakeDamage(xtraDamage);
                    caster.perkProcs.Add($"+{xtraDamage} Damage dealt");
                }
            }
        }
        //Healer
        public static void HealingSpore(CombatResult caster, CombatResult target)
        {
            Ability a = caster.abilityUsed;
            if (target.character.HealthStatus(out string status) <= ((-a.level) / 15) - 1)
                caster.perkProcs.Add($"{a.name} is not powerful enough to have an effect on {status} targets");
            else if (caster.SentHit && target.character.Status(a.statusEffect, 5 + (a.level / 5), a.level) > -1)
                caster.perkProcs.Add("Applied recovering to " + target.Name);
        }
        public static void Overheal(CombatResult caster, CombatResult target)
        {
            Ability a = caster.abilityUsed;
            long maxhp = target.character.Health();
            if (caster.SentHit)
            {
                if (target.character.health >= maxhp)
                {
                    double maxOverHeal = 0.20 + (a.level / 100.00) +
                        (caster.character.stats.GetINT() / 100.00);
                    long healing = NumbersM.CeilParse<long>(maxhp * maxOverHeal);
                    if (target.character.health > maxhp)
                        healing = Verify.Min(healing - target.character.health, 0);
                    caster.perkProcs.Add($"Overhealed {target} for {target.character.Healing(healing, true)} hp");
                }
                else caster.perkProcs.Add("Overheal requires target to have full health.");
            }
        }
        //Blacksmith
        public static void FullImpact(CombatResult caster, CombatResult target)
        {
            Ability fullImpact = caster.abilityUsed;
            Collections.Equipment eq = caster.character.equipment;
            int tiers = 0;
            for(int i =0; i < 10; i++)
            {
                if (i < 5 || i > 7)
                {
                    var gear = eq.GetGear(i);
                    if (gear != null && gear.tier > gear.baseTier)
                        tiers += Math.Max(gear.tier - gear.baseTier, 0);
                }
            }
            double bonus = (0.004 * fullImpact.level) * tiers;
            if(bonus > 0)
                caster.damageMultiplier = ArrayM.AddEach(caster.damageMultiplier, bonus);
            caster.perkProcs.Add($"{bonus * 100}% Impact");
        }
        public static void Deconstruct(CombatResult caster, CombatResult target)
        {
            Ability deconstruct = caster.abilityUsed;
            if (Program.rng.Next(101) <= 30 + deconstruct.level)
            {
                List<int> ints = new List<int>();
                for (int i = 0; i < 10; i++)
                    if (target.character.equipment.GetGear(i) != null)
                        ints.Add(i);
                int j = ints[Program.rng.Next(ints.Count)];
                var item = target.character.equipment.GetGear(j);
                    item.condition -= deconstruct.level * 3;
                caster.perkProcs.Add($"Damaged {target.Name}'s {item} by {deconstruct.level * 3}" +
                    $" CND");
            }
        }
        //Rogue
        public static void RogueLifeSteal(CombatResult caster, CombatResult target)
        {
            Ability ability = caster.abilityUsed;
            if(caster.isCritical || Program.rng.Next(101) <= 26 + (ability.level * 2))
            {
                //Get % of damage restored to health
                double leech = caster.GetTotalCriticalMultiplier() / 10.00;
                //Get total damages
                long damage = 0;
                for (int i = 0; i < ReferenceData.DmgType.Length; i++)
                    damage += caster.GetTotalDamage(i);
                //Multiply
                long restores = NumbersM.FloorParse<long>(damage * (leech/100.00));
                //restore
                caster.character.Healing(restores);
            }
        }
        #endregion
    }
}