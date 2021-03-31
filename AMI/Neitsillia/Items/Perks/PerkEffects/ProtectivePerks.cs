using AMI.Methods;
using AMI.Neitsillia.Combat;
using AMYPrototype;

namespace AMI.Neitsillia.Items
{
    partial class Perk
    {
        public object[] ReflectiveWard(CombatResult owner, CombatResult target)
        {
            if (target.SentHit)
            {
                if (Program.Chance(tier))
                {
                    long totalDamage = 0;
                    for (int i = 0; i < target.baseDamage.Length; i++)
                    {
                         totalDamage += NumbersM.FloorParse<long>
                            ((target.baseDamage[i]
                            + target.bonusDamage[i]) * 
                            target.damageMultiplier[i]);

                        target.damageMultiplier[i] = 0;
                    }
                    long returned = NumbersM.FloorParse<long>(totalDamage *
                        (5.00 + tier) / 100);
                    target.character.TakeDamage(returned);
                    owner.perkProcs.Add($"Returned {returned} damage to {target.Name}");
                }
            }

            return R(owner, target);
        }
    }
}
