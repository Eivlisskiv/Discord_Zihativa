using AMI.Methods;
using AMI.Neitsillia.Combat;
using AMI.Neitsillia.Items.Abilities;

namespace AMI.Neitsillia.Items
{
    public partial class Perk
    {
        public object[] TakeOnMe(CombatResult owner, CombatResult enemy)
        {
            if (enemy.abilityUsed.type == Ability.AType.Elemental
                || enemy.abilityUsed.type == Ability.AType.Enchantment
                || enemy.abilityUsed.type == Ability.AType.Martial)
            {
                if (!enemy.SentHit && RNG(101) <= 15)
                    enemy.sentHit = true;
                if (enemy.SentHit && RNG(101) <= 10)
                {
                    long healed = owner.character.Healing(NumbersM.CeilParseLong(owner.character.Health() * 0.10), true);
                    owner.perkProcs.Add($"{owner.Name} proc'ed {name}'s healing [+{healed} health]");
                }
            }
            return new object[] { owner, enemy };
        }
        public object[] SpikedArmor(CombatResult owner, CombatResult enemy)
        {
            if (enemy.abilityUsed != null && enemy.SentHit &&
                enemy.abilityUsed.type == Ability.AType.Martial)
            {
                long reflectedDamage = (long)(enemy.baseDamage[0] *
                Verify.Max(owner.character.stats.GetEND() * 0.01, 0.25));
                if (reflectedDamage > 0)
                {
                    enemy.selfDamageTaken[0] += reflectedDamage;
                    owner.perkProcs.Add($"{owner.Name} returned {reflectedDamage} to {enemy.Name}");
                }
            }
            return new object[] { owner, enemy };
        }
        public object[] ToxinFilter(CombatResult owner, CombatResult enemy)
        {
            long toxicdmg = 0;
            if (enemy.SentHit && (toxicdmg = enemy.GetTotalDamage(3)) > 0)
            {
                if (RNG(101) <= 10 + (5 * rank))
                {
                    owner.perkProcs.Add($"Gained " + owner.character.StaminaE(
                        (int)(toxicdmg * (0.25 + (0.5 * rank)))) +
                          $" Stamina from Toxin Filter");
                    rank = 0;
                }
                else if (rank < maxRank) rank++;
            }
            return new object[] { owner, enemy };
        }
        public object[] LoneWolf(CombatResult owner, CombatResult enemy)
        {
            if (enemy.SentHit && enemy.abilityUsed.type == Ability.AType.Martial)
            {
                var enemies = enemy.GetAllies();
                var allies = owner.GetAllies();
                int d = enemies.Length - allies.Length;
                if (d > 0)
                {
                    owner.bonusResistance = ArrayM.AddEach(owner.bonusResistance, 20 * d);
                }
            }
            return new object[] { owner, enemy };
        }
        public object[] Opportunist(CombatResult owner, CombatResult enemy)
        {
            if (enemy.character.status.Count > 0)
            {
                owner.damageMultiplier = ArrayM.AddEach(owner.damageMultiplier,
                    0.01 * enemy.character.status.Count);
            }
            return new object[] { owner, enemy };
        }
        public object[] CleanSlate(CombatResult owner)
        {
            if (owner.character.status.Count == 0)
            {
                owner.character.StaminaE(0.05);
            }
            return new object[] { owner };
        }
        public object[] Underestimated(CombatResult owner, CombatResult enemy)
        {
            if (owner.character.level < enemy.character.level ||
                owner.character.Rank() < enemy.character.Rank())
            {
                owner.damageMultiplier = ArrayM.AddEach(owner.damageMultiplier, 0.1);
            }
            return new object[] { owner, enemy };
        }
        public object[] AwaitedStorm(CombatResult owner, CombatResult enemy)
        {
            if (owner.abilityUsed.IsType(Ability.AType.Elemental, Ability.AType.Enchantment, Ability.AType.Martial))
            {
                if (rank > 0)
                {
                    int bonus = owner.character.stats.GetSTR() * rank;
                    owner.damageMultiplier = ArrayM.AddEach(owner.damageMultiplier, bonus / 100.00);
                    owner.perkProcs.Add($"+{bonus}% DMG on {enemy.Name}");
                    rank = 0;
                }
            }
            else
                rank++;
            return R(owner, enemy);
        }
        public object[] FastLearner(double mult) => R(mult + 0.15);
        //Tier 1
        public object[] Lightweight(CombatResult owner, CombatResult target)
        {
            return Shared_Dodge(owner, target, 15,
                Ability.AType.Enchantment, Ability.AType.Martial);
        }

        public object[] MagicDescry(CombatResult owner, CombatResult target)
        {
            return Shared_Dodge(owner, target, 10,
                Ability.AType.Elemental, Ability.AType.Tactical);
        }

        public object[] Vengeful(CombatResult owner)
        {
            double per = (owner.character.health / owner.character.Health()) - 0.35;
            if (per > 0) owner.damageMultiplier = ArrayM.AddEach(owner.damageMultiplier, per);
            return R(owner);
        }
    }
}
