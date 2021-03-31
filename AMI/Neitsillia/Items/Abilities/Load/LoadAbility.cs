using System;

namespace AMI.Neitsillia.Items.Abilities.Load
{
    static partial class LoadAbility
    {
        private static AMIData.ReflectionCache reflectionCache = new AMIData.ReflectionCache(typeof(LoadAbility));

        public static readonly string[,] Starters = new string[,]
        {
            { "Strike", "Shelter",      "Snake Oil" },
            { "Sunder", "Counter Prep", "White Bite" },
            { "Vivace", "Bold Stance",  "Sabotage" }
        };

        internal static Ability Load(string name, int level = -1)
            => reflectionCache.Run<Ability>(name.Replace(" ", ""), name, level);

        static Ability Set(Ability a, int l)
        {
            l -= a.level;
            //l++;
            while (l > a.level && a.level < a.maxLevel) a.LevelUp(true);
            return a;
        }

        /* Template
        public static Ability Template(string aname, int alevel = 0)
        {
            Ability a = new Ability(aname)
            {
                name = aname,
                type = Ability.AType,
                //
                critChance = 0,
                critMult = 0,
                agility = 0,
                healing = 0,
                //
                staminaDrain = 0,
                //
                level = 0,
                maxLevel = 0,
                tier = 0,
            };
            a.damage[0] = 1;
            a.resistance[0] = 0;
            Set(a, alevel);
            a.description
            return a;
        }
        //*/

        //Martial
        public static Ability Brawl(string aname, int alevel = -1)
        {
            Ability a = new Ability(aname)
            {
                type = Ability.AType.Martial,
                //
                critChance = 0,
                critMult = 0,
                agility = 0,
                //
                staminaDrain = 0,
                //
                level = 0,
                maxLevel = 0,
                tier = 0,
                description = "A simple attack dealing physical damage +1 Physical damage per Strength points.",
            };
            a.damage[0] = 1;
            return a;
        }

        #region Starters

        #region Offense Tree
        public static Ability Strike(string aname, int alevel = -1)
        {
            Ability a = new Ability(false)
            {
                name = aname,
                type = Ability.AType.Martial,
                //
                critChance = 1,
                critMult = 5,
                agility = 0,
                //
                staminaDrain = 30,
                //
                level = 0,
                maxLevel = 20,
                tier = 0,
                description = "A malleable strike dealing physical damage.",
                evolves = new[] 
                {
                    "Fast Strike",
                    "Toxic Strike",
                }
            };
            a.damage[0] = 3;
            Set(a, alevel);
            return a;
        }
        // >>
        public static Ability FastStrike(string aname, int alevel = -1)
        {
            Ability a = new Ability(false)
            {
                name = aname,
                type = Ability.AType.Martial,
                //
                critChance = 10,
                critMult = 1,
                agility = 1,
                //
                staminaDrain = 35,
                //
                level = 0,
                maxLevel = 20,
                tier = 1,
                description = "A fast strike dealing physical damage with an increase in critical chance.",

                evolves = new[]
                {
                    "Strike",
                    "Toxic Strike",
                }
            };
            a.damage[0] = 9;
            Set(a, alevel);
            return a;
        }
        public static Ability ToxicStrike(string aname, int alevel = -1)
        {
            Ability a = new Ability(false)
            {
                name = aname,
                type = Ability.AType.Martial,
                //
                critChance = 1,
                critMult = 1,
                agility = 2,
                //
                staminaDrain = 32,
                //
                level = 0,
                maxLevel = 20,
                tier = 1,

                statusEffect = "Poisoned",

                evolves = new[]
                {
                    "Fast Strike",
                    "Strike",
                }
            };
            a.damage[0] = 4;
            Set(a, alevel);
            a.description = $"A slighter stronger strike with a {10 + a.level}% to apply poison to target for 6 turns.";

            return a;
        }
        //3
        #endregion


        #region Sunder Tree
        public static Ability Sunder(string aname, int alevel = -1)
        {
            Ability a = new Ability(false)
            {
                name = aname,
                type = Ability.AType.Martial,
                //
                critChance = 0,
                critMult = 20,
                agility = -5,
                //
                staminaDrain = 10,
                //
                level = 1,
                maxLevel = 15,
                tier = 0,
                statusEffect = "Punctured",
                evolves = new string[]
                {
                    "Blunt",
                },
            };
            a.damage[0] = 2;
            //
            Set(a, alevel);
            a.description = $"A decisive and slower blow with a {35 + a.level}% chance to reduce" +
                $" enemy physical resistance by {10 * a.level} for 3 turns.";
            //
            return a;
        }
        // >>
        //More effective Sunder (+20% base damage to physical damage and +1 status duration per x level)
        public static Ability Blunt(string aname, int alevel = -1)
        {
            Ability a = new Ability(false)
            {
                name = aname,
                type = Ability.AType.Martial,
                //
                critChance = 5,
                critMult = 10,
                agility = -10,
                //
                staminaDrain = 40,
                //
                level = 0,
                maxLevel = 20,
                tier = 1,
                statusEffect = "Suppressed",

                evolves = new string[]
                {
                    "Sunder",
                }
            };
            a.damage[0] = 3;
            //
            Set(a, alevel);
            a.description = $"A blunt attack with a {35 + a.level}% chance to reduce" +
                $" enemy damage by {10 + a.level}% for {3 + (a.level/5)} turns.";
            //
            return a;
        }
        //3
        #endregion
        
        //Defensives
        #region Shelter Tree
        public static Ability Shelter(string aname, int alevel = 0)
        {
            Ability a = new Ability(aname)
            {
                type = Ability.AType.Defensive,
                //
                agility = 50,
                //
                staminaDrain = 45,
                //
                level = 1,
                maxLevel = 10,

                statusEffect = "Hardened",

                evolves = new[]
                {
                    "Heal",
                }
            };
            //
            Set(a, alevel);
            a.description = $"Grants {20 * a.level} physical resistance to the target for {2 + (a.level / 5)} turns.";
            //
            return a;
        }
        // >>
        //Stronger shelter
        public static Ability Heal(string aname, int alevel = 0)
        {
            Ability a = new Ability(aname)
            {
                type = Ability.AType.Defensive,
                //
                agility = 25,
                //
                staminaDrain = 25,
                //healing = 50,
                //
                tier = 1,
                level = 0,
                maxLevel = 35,

                evolves = new[]
                {
                    "Shelter",
                }
            };
            //
            Set(a, alevel);
            a.description = $"Heals the target for {a.level + 8}% of target's max HP." +
                $" Caster's intelligence increases the effect by 1% per point.";
            //
            return a;
        }
        //3
        #endregion


        #region BoldStance
        public static Ability BoldStance(string aname, int alevel = 0)
        {
            Ability a = new Ability(aname)
            {
                type = Ability.AType.Defensive,
                //
                agility = 70,
                //
                staminaDrain = 25,
                //
                level = 1,
                maxLevel = 40,
                statusEffect = "Discourage",
                evolves = new string[] { "Cherish" }
            };
            //
            Set(a, alevel);
            a.description = $"Grants {35 + a.level}% chance to reduces attacker's damage by {a.level * 2}% for {4 + (a.level/10)} turns. {1 + (a.level/20)} Charges";
            return a;
        }
        public static Ability Cherish(string aname, int alevel = 0)
        {
            Ability a = new Ability(aname)
            {
                type = Ability.AType.Defensive,
                //
                agility = 70,
                //
                staminaDrain = 125,
                //
                level = 0,
                tier = 1,
                maxLevel = 40,
                statusEffect = "Hospitable Aura",
                evolves = new string[] { "Bold Stance" }
            };
            //
            Set(a, alevel);
            a.description = $"Grants [{60 + a.level}% chance for +{10 + a.level} elemental RES to self and caster " +
                $"for {2 + (a.level / 8)} turns] when receiving defensive casts. Effect lasts {4 + (a.level / 10)} turns and has {1 + (a.level / 20)} Charges";
            return a;
        }
        #endregion

        //Tactical
        #region Sabotage Tree
        public static Ability Sabotage(string name, int alevel = 0)
        {
            Ability a = new Ability(name)
            {
                type = Ability.AType.Tactical,
                //
                staminaDrain = 30,
                agility = 50,
                level = 1,
                maxLevel = 15,
                statusEffect = "Rigged",
                evolves = new string[] { "Termite" }
            }; //
            Set(a, alevel);
            a.description = $"For each turn passed, damage received is increased by {a.level}%. Effect wears off after {2 + (a.level / 5)} turns or after receiving a hit.";
            return a;
        }
        public static Ability Termite(string name, int alevel = 0)
        {
            Ability a = new Ability(name)
            {
                type = Ability.AType.Tactical,
                //
                staminaDrain = 60,
                agility = 20,
                level = 1,
                maxLevel = 50,
                statusEffect = "Decay",
                evolves = new string[] { "Sabotage",  }
            }; //
            Set(a, alevel);
            a.description = $"Target loses {5 + (a.level) / 5} physical resistance per turn for {5 + (2* (a.level) / 10)} turns.";
            return a;
        }
        #endregion

        #region Snake Oil Tree
        public static Ability SnakeOil(string name, int alevel = 0)
        {
            Ability a = new Ability(name)
            {
                type = Ability.AType.Tactical,
                //
                staminaDrain = 40,
                agility = 50,
                level = 1,
                maxLevel = 20,

                evolves = new string[] { "Ignite", "Envenom", "Undercut" }
            }; //
            Set(a, alevel);
            a.description = "Applies a random status effect between Bleeding, Burning and Poisoned " +
               $"for {3 + (a.level/5)} Turns with an intensity of {(int)Math.Ceiling(a.level / 2.00M)}";
            return a;
        }
        // >>
        public static Ability Undercut(string name, int alevel = 0)
        {
            Ability a = new Ability(name)
            {
                type = Ability.AType.Tactical,
                staminaDrain = 80,
                level = 0,
                maxLevel = 30,
                tier = 1,

                statusEffect = "Bleeding",

                evolves = new string[]
                {
                    "Ignite", "Envenom",
                }
            };
            Set(a, alevel);

            a.description = "Bleeds the target for " +
                $"{5 + (a.level / 6)} turns dealing {30 + a.level}% of your base damage.";
            return a;
        }
        public static Ability Ignite(string name, int alevel = 0)
        {
            Ability a = new Ability(name)
            {
                type = Ability.AType.Tactical,
                staminaDrain = 80,
                level = 0,
                maxLevel = 30,
                tier = 1,

                statusEffect = "Burning",

                evolves = new string[]
                {
                    "Envenom", "Undercut"
                }
            };
            Set(a, alevel);

            a.description = "Burns the target for " +
                $"{5 + (a.level / 6)} turns dealing {30 + a.level}% of your base damage.";
            return a;
        }
        public static Ability Envenom(string name, int alevel = 0)
        {
            //Envenom 
            Ability a = new Ability(name)
            {
                type = Ability.AType.Tactical,
                staminaDrain = 80,
                level = 0,
                maxLevel = 30,
                tier = 1,

                statusEffect = "Poisoned",

                evolves = new string[]
                {
                    "Ignite", "Undercut"
                }
            };
            Set(a, alevel);

            a.description = "Poisons the target for " +
                $"{5 + (a.level / 6)} turns dealing {30 + a.level}% of your base damage.";
            return a;
        }
        #endregion

        #region White Bite Tree
        public static Ability WhiteBite(string name, int alevel = 0)
        {
            Ability a = new Ability(name)
            {
                type = Ability.AType.Tactical,
                //
                staminaDrain = 30,
                agility = 50,
                level = 1,
                maxLevel = 15,
                statusEffect = "Energy Leak",
            }; //
            Set(a, alevel);
            a.description = $"Applies Energy Leak status on target for {3 + (a.level / 3)} Turns. " +
                $"Energy Leak: When hit, attacker has {35 + a.level}% chance to drain {a.level * 8} stamina";
            return a;
        }

        public static Ability Taunt(string aname, int alevel = -1)
        {
            Ability a = new Ability(aname)
            {
                type = Ability.AType.Tactical,
                description = "Taunts a target to get aggro and restore stamina.",
            };
            return a;
        }
        #endregion

        #endregion

        #region Elemental 
        //Elementals ignore the caster's stats
        //-//Blaze
        public static Ability Heat(string aname, int alevel = 0)
        {
            Ability a = new Ability(false)
            {
                name = aname,
                type = Ability.AType.Elemental,
                //
                critChance = 10,
                critMult = 20,
                agility = 60,
                //
                staminaDrain = 130,
                //
                maxLevel = 22,
                statusEffect = "Burning",
                description = "Deals low blaze damage and has 10% chance to apply Burning on target.",
            };
            a.damage[1] = 1;
            Set(a, alevel);
            return a;
        }
        public static Ability Blaze(string aname, int alevel = 0)
        {
            Ability a = new Ability(false)
            {
                name = aname,
                type = Ability.AType.Elemental,
                //
                critChance = 35,
                critMult = 60,
                agility = 60,
                //
                staminaDrain = 180,
                //
                level = 0,
                maxLevel = 15,
                tier = 1,
                statusEffect = "Burning",
                description = "Deals medium blaze damage and has 20% chance to apply Burning on target.",
            };
            a.damage[1] = 20;
            Set(a, alevel);
            return a;
        }

        public static Ability Toxin(string aname, int alevel = 0)
        {
            Ability a = new Ability(false)
            {
                name = aname,
                type = Ability.AType.Elemental,
                //
                critChance = 10,
                critMult = 20,
                agility = 60,
                //
                staminaDrain = 130,
                //
                level = 0,
                maxLevel = 22,
                tier = 0,
                statusEffect = "Poisoned",
                description = "Deals low toxic damage and has 10% chance to poison the target.",
            };
            a.damage[(int)ReferenceData.DamageType.Toxic] = 1;
            Set(a, alevel);
            return a;
        }
        public static Ability Static(string aname, int alevel = 0)
        {
            Ability a = new Ability(false)
            {
                name = aname,
                type = Ability.AType.Elemental,
                //
                critChance = 10,
                critMult = 20,
                agility = 60,
                //
                staminaDrain = 150,
                //
                level = 0,
                maxLevel = 22,
                tier = 0,
                statusEffect = "Paralyzed",
                description = "Deals low electric damage and has 10% chance to paralyze the target.",
            };
            a.damage[(int)ReferenceData.DamageType.Electric] = 1;
            Set(a, alevel);
            return a;
        }
        #endregion

        //Specialization Abilities
        #region Fighter
        //--// Enchantment Scaling damage attack
        public static Ability Execute(string aname, int alevel = 0)
        {
            Ability a = new Ability(false)
            {
                name = aname,
                type = Ability.AType.Enchantment,
                //
                critChance = 0,
                critMult = 0,
                agility = 0,
                //
                staminaDrain = 150,
                //
                level = 0,
                maxLevel = 30,
                tier = 3,
            };
            a.damage[0] = 1;
            Set(a, alevel);
            a.description = $"Additionally deals {2 + a.level}% of base damage ignoring target resistance.";
            return a;
        }
        //--//-Enemy armor resistance <meh>
        //--//-Enemy damage <meh>
        #endregion
        #region Healer
        //--//per turn healing Recovering
        public static Ability HealingSpore(string aname, int alevel = 0)
        {
            Ability a = new Ability(false)
            {
                name = aname,
                type = Ability.AType.Defensive,
                //
                critChance = 0,
                critMult = 0,
                agility = 0,
                //
                staminaDrain = 50,
                //
                level = 1,
                maxLevel = 40,
                tier = 3,
                statusEffect = "Recovering",
            };
            Set(a, alevel);
            a.description = $"Heals {a.level * 5}HP every turn for {5 + (a.level / 5)} Turns";
            return a;
        }
        //--// Heals when max health
        public static Ability Overheal(string aname, int alevel = 0)
        {
            Ability a = new Ability(false)
            {
                name = aname,
                type = Ability.AType.Defensive,
                //
                critChance = 0,
                critMult = 0,
                agility = 0,
                //
                staminaDrain = 20,
                //
                level = 1,
                maxLevel = 20,
                tier = 3,

            };
            Set(a, alevel);
            a.description = $"Over heals target for {20 + a.level}HP. Target must be full health or higher";
            return a;
        }
        //--// ?
        #endregion
        #region Blacksmith
        //--//Chance to repair armor (Need to be changed)
        public static Ability Deconstruct(string name, int alevel = 0)
        {
            Ability a = new Ability(name)
            {
                type = Ability.AType.Martial,
                //
                staminaDrain = 55,
                level = 1,
                tier = 3,
                maxLevel = 20,
            }; //
            Set(a, alevel);
            a.description = $"{30 + (a.level*2)}% chance to damage enemy gear condition" +
                $" by {a.level * 3}.";
            return a;
        }
        //--// Bonus damage from gear upgrade tiers
        public static Ability FullImpact(string name, int alevel = 0)
        {
            Ability a = new Ability(name)
            {
                type = Ability.AType.Martial,
                //
                staminaDrain = 40,
                level = 1,
                tier = 3,
                maxLevel = 10,
            }; //
            Set(a, alevel);
            a.description = $"+ {0.4 * a.level}% damage for each tier upgraded on equipped gear.";
            return a;
        }
        #endregion
        //Rogue
        //--//Life Steal
        public static Ability RogueLifeSteal(string name, int alevel = 0)
        {
            Ability a = new Ability(name)
            {
                type = Ability.AType.Martial,
                //
                staminaDrain = 30,
                agility = 50,
                tier = 3,
                level = 0,
                maxLevel = 12,
            }; //
            Set(a, alevel);
            a.description = $"Restores 10% of Critical Damage% of damage dealt to health on critical hit. " +
                $"Non critical hits have a {26 + (a.level*2)}% chance to proc effect.";
            return a;
        }
        //--//Stamina Steal
        public static Ability RogueStaminaSteal(string name, int alevel = 0)
        {
            Ability a = new Ability(name)
            {
                type = Ability.AType.Martial,
                //
                staminaDrain = 15,
                agility = 30,
                tier = 3,
                maxLevel = 12,
            }; //
            Set(a, alevel);
            a.description = $"Restores 5% of Critical Damage of damage dealt to stamina on critical hit. " +
                $"Non critical hits have a {26 + (a.level * 2)}% chance to proc effect.";
            return a;
        }
    }
}