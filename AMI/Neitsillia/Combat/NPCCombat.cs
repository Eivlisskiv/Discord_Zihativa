using AMI.Methods.Graphs;
using AMI.Neitsillia.Items;
using AMI.Neitsillia.NPCSystems;
using AMYPrototype;
using System;
using System.Collections.Generic;

namespace AMI.Neitsillia.Combat
{
    class NPCCombat
    {
        //Mob VS Mob
        public static NPC[] SetCombat(NPC npcA, NPC npcB)
        {
            Random rng = new Random();
            if (npcA.PowerLevel() > npcB.PowerLevel())
                npcB.health = 0;
            else
                npcA.health = 0;
            return new NPC[] { npcA, npcB };
        }
        public static Ability MobAI(NPC mob)
        {
            return mob.abilities[MobAI(mob, null, null)[0]];
        }
        public static int[] MobAI(NPC self, CharacterMotherClass[] allies, CharacterMotherClass[] enemies)
        {
            if (self.abilities.Count == 1 || self.stamina < self.abilities[1].staminaDrain)
                return BrawlAI(enemies);

            //         ability index, team, target,
            int[] res = new int[] { 0, 0, 0 };
            switch(self.role)
            {
                case ReferenceData.CombatRole.Healer:
                    {
                        int t = -1;
                        if (self.abilities.Count > 1 &&
                            self.stamina >= self.abilities[1].staminaDrain &&
                            (t = HealAI(((-self.abilities[1].level) / 15) - 1, allies)) > -1)
                            return new int[] { 1, 1, t };
                        return BrawlAI(enemies);
                    }
                case ReferenceData.CombatRole.Fighter:
                    {
                        List<int> validTargets = new List<int>();
                        for (int i = 0; i < enemies.Length; i++)
                            if (enemies[i].health > 0)
                                validTargets.Add(i);
                        return new int[] { 1, 0, validTargets[Program.rng.Next(validTargets.Count)] };
                    }
                //case ReferenceData.CombatRole.Trickster:
                //    break;
                default:
                    return BrawlAI(enemies);
            }
        }

        static int HealAI(int minState, CharacterMotherClass[] allies)
        {
            int? lowestHP = null;

            for (int i = 0; i < allies.Length; i++)
            {
                var ally = allies[i];
                var state = ally.HealthStatus(out _);

                if (lowestHP != null && 
                    !ally.status.Exists(s => s.name == "Recovering") &&
                    ally.health < allies[lowestHP.Value].health &&
                    state > minState && state < 4)
                    lowestHP = i;
                else if (lowestHP == null && state > minState && state < 6)
                    lowestHP = i;
            }
            return lowestHP ?? -1;
        }
        private static int[] BrawlAI(CharacterMotherClass[] enemies)
        {
            List<int> validTargets = new List<int>();
            for (int i = 0; i < enemies.Length; i++)
                if (enemies[i].health > 0)
                    validTargets.Add(i);
            return new int[] { 0, 0, validTargets[Program.rng.Next(validTargets.Count)]};
        }

        public static long ElementalResistance(long resistance, long damage)
        {
            if (resistance > 0)
            {
                double rawReduction = Exponential.Armor(resistance) / 100;
                int dmgReduction = Convert.ToInt32(damage * rawReduction);
                damage -= dmgReduction;
                return damage;
            }
            else
            {
                double rawReduction = Exponential.Armor(-resistance) / 100;
                int dmgReduction = Convert.ToInt32(damage * rawReduction);
                damage += dmgReduction;
                return damage;
            }
        }
    }
}
