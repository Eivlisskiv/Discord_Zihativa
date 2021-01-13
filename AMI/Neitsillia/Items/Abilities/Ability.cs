using AMI.Methods;
using AMI.Methods.Graphs;
using AMI.Neitsillia.User.UserInterface;
using Discord;
using Newtonsoft.Json;
using System;

namespace AMI.Neitsillia.Items
{
    [MongoDB.Bson.Serialization.Attributes.BsonIgnoreExtraElements]
    public class Ability
    {
        private static AMIData.ReflectionCache<Ability> reflectionCache = new AMIData.ReflectionCache<Ability>();

        private static AMIData.ReflectionCache effectCache = new AMIData.ReflectionCache(typeof(AbilityEffects));
        /* Types:
         * Martial: Stats added to user's equipment stats;
         * Elemental: Offensive elemental spell, is buff'ed
         * by user INT;
        //*/
        public enum AType { Martial, Elemental, Enchantment, Defensive, Tactical }

        //
        public string name;
        public AType type;
        public string[] evolves;

        //Damage
        public long[] damage = new long[ReferenceData.DmgType.Length];
        //
        public double critChance;
        public double critMult;
        public int agility;
        //
        public double staminaDrain;
        //
        public long xp;
        public int level;
        public int maxLevel;
        public int tier;
        //
        public string description;
        public string statusEffect;

        internal static string StarterAbilityTree()
        {
            string trees = null;
            foreach (string s in LoadAbility.Starters)
                trees += Load(s).AbilityTree("", 1, 0, 1,1) + 
                    Environment.NewLine + "-------------------------------------------------------------------------------------------------------------------"  + Environment.NewLine;
            return trees;
        }

        internal string AbilityTree(string tree, int depth, int tabs, params int[] grid)
        {
            tree += "".PadLeft(tabs + ((depth - 1) * 6), '-');
            tree += name + $"[{depth}] ";
            if (evolves != null && evolves.Length > 0)
            {
                if (grid[0] <= depth)
                {
                    grid[0]++;
                }
                grid[1] += evolves.Length - 1;
                depth++;
                
                foreach (string s in evolves)
                {
                    tree = Load(s).AbilityTree(tree, depth, tabs, grid) + Environment.NewLine;
                    tabs += name.Length;
                }
                depth --;
                tabs -= name.Length;
            }
            return tree;
        }

        //Loading Abilities
        internal static Ability Load(string name, int level = -1)
            => LoadAbility.Load(name, level);

        [JsonConstructor]
        public Ability(bool JSON)
        { }
        public Ability(string aname, int atier = 0)
        { name = aname; tier = atier; }
        public override string ToString()
        {
            return $"{name} [{level}]";
        }
        //
        internal void InvokeEffect(params object[] param)
            => effectCache.Run(name.Replace(" ", ""), param);

        //XP and Level
        public bool GainXP(long argxp, double xpRate)
        {
            bool leveled = false;
            xp += Convert.ToInt64(argxp * xpRate);
            while (LevelUp())
                leveled = true;
            return leveled;
        }
        internal bool LevelUp(bool skip = false)
        {
            long requirment = XPRequired();
            if (level < maxLevel && (skip || xp >= requirment))
            {
                //
                for (int i = 0; i < damage.Length; i++)
                    damage[i] = UpgradeStat(damage[i], 1.20);
                critChance = UpgradeStat(critChance, 1.08);
                critMult = UpgradeStat(critMult, 1.08);
                agility = UpgradeStat(agility, 1.01);
                staminaDrain *= 1.02;
                //
                if (!skip)
                {
                    xp -= requirment;
                    description = Load(name, level).description;
                }
                level++;
                return true;
            }
            return false;
        }
        internal long XPRequired()
        { return Quadratic.AbilityXPRequirement((level + 1) * (tier + 1)); }

        internal string DetailedXP()
        {
            long req = XPRequired();
            return $"{Utils.Display(xp)}/{Utils.Display(req)} (**{Utils.Display(req - xp)}**)";
        }

        static long UpgradeStat(long stat, double increase)
        {
            if(stat > 0)
                stat = Verify.Min(NumbersM.FloorParse<long>(stat * increase), stat + 1);
            else if (stat < 0)
                stat = Verify.Min(NumbersM.FloorParse<long>(stat * increase), stat - 1);
            return stat;
        }
        static int UpgradeStat(int stat, double increase)
        {
            if (stat > 0)
                stat = Verify.Min(NumbersM.FloorParse<int>(stat * increase), stat + 1);
            else if (stat < 0)
                stat = Verify.Min(NumbersM.FloorParse<int>(stat * increase), stat - 1);
            return stat;
        }
        static double UpgradeStat(double stat, double increase)
        {
            if (stat > 0)
                stat = Math.Round(Verify.Min(stat * increase, stat + 1), 2);
            else if (stat < 0)
                stat = Math.Round(Verify.Min(stat * increase, stat - 1), 2);
            return stat;
        }
        internal EmbedBuilder EvolveOptions(EmbedBuilder evolveField)
        {
            if (evolveField == null)
                evolveField = new EmbedBuilder();
            string value = "";
            if (evolves != null)
            {
                for (int i = 0; i < evolves.Length; i++)
                {
                    Ability a = Load(evolves[i]);
                    value +=
                        $"{EUI.GetNum(i)} | **{a.name}**{Environment.NewLine}" +
                        $"*{a.description}*{Environment.NewLine}" +
                        $"``{a.GetStats()}``{Environment.NewLine}" +
                        "";
                }
                if (level < maxLevel)
                    value += $"{Environment.NewLine}{Environment.NewLine}{name} is not ready to evolve: {level}/{maxLevel}";
            }
            else
                value = "No Evolves Available";
            evolveField.AddField($"{name} Evolve Options", value);
            return evolveField;
        }
        //Gets
        public int StaminaDrain()
        { return Convert.ToInt32(Math.Ceiling(staminaDrain)); }
        /// <summary>
        /// Calculates the total Damage
        /// </summary>
        /// <param name="i">index of damage type</param>
        /// <param name="b">User Base stat</param>
        /// <param name="e">User Efficiency</param>
        /// <returns></returns>
        public long Damage(int i, double b, double e)
        {
            while(damage.Length < ReferenceData.DmgType.Length)
                damage = ArrayM.AddItem(damage, 0);
            switch(type)
            {
                case AType.Martial:
                    return Convert.ToInt32(damage[i] + b);
                case AType.Enchantment:
                    return Convert.ToInt32(((1 + (damage[i] / 100.00)) * b) * e);
                case AType.Elemental:
                    return Convert.ToInt32(damage[i] * e);
                case AType.Tactical:
                case AType.Defensive:
                default:
                    return 0;
            }
        }
        /// <summary>
        /// Calculates the total Critical Hit chance
        /// </summary>
        /// <param name="b">User Base stat</param>
        /// <param name="e">User Efficiency</param>
        /// <returns></returns>
        public double CritChance(double b, double e)
        {
            switch (type)
            {
                case AType.Martial:
                    return Convert.ToInt32(critChance + b);
                case AType.Enchantment:
                    return ((1 + (critChance / 100.00) + e) * b);
                case AType.Elemental:
                    return critChance * e;
                case AType.Tactical:
                case AType.Defensive:
                default:
                    return 0;
            }
        }
        /// <summary>
        /// Calculates the total Critical Hit Damage
        /// </summary>
        /// <param name="b">User Base stat</param>
        /// <param name="e">User Efficiency</param>
        /// <returns></returns>
        public double CritMult(double b, double e)
        {
            switch (type)
            {
                case AType.Martial:
                    return Convert.ToInt32(critMult + b);
                case AType.Enchantment:
                    return ((1 + (critMult / 100.00) + e) * b);
                case AType.Elemental:
                    return critMult * e;
                case AType.Tactical:
                case AType.Defensive:
                default:
                    return 0;
            }
        }
        /// <summary>
        /// Calculates the total Agility
        /// </summary>
        /// <param name="b">User Base stat</param>
        /// <param name="e">User Efficiency</param>
        /// <returns></returns>
        public int Agility(double b, double e)
        {
            switch (type)
            {
                case AType.Martial:
                    return Convert.ToInt32(agility + b);
                case AType.Enchantment:
                    return Convert.ToInt32((1 + (agility / 100.00) + e) * b);
                case AType.Defensive:
                case AType.Elemental:
                case AType.Tactical:
                default:
                    return Convert.ToInt32(agility * e);
            }
        }
        //
        public EmbedBuilder InfoPage(EmbedBuilder a, bool getEvolveOptions, bool newField = false)
        {
            if (!newField)
            {
                a.Title += name;
                a.WithDescription(GetDescription());
                a.AddField("Stats", GetStats());
            }
            else
            {
                a.AddField(name, GetDescription() + Environment.NewLine + GetStats());
            }
            if(getEvolveOptions)
            EvolveOptions(a);
            return a;
        }
        internal string GetStats()
        {
            string stats = $"Stamina Requirement: {Math.Floor(staminaDrain)}{Environment.NewLine}";
            if(critChance != 0)
                switch (type)
                {
                    case AType.Enchantment:
                        stats += $"CC: x{100+critChance}%{Environment.NewLine}";
                        break;
                    case AType.Martial:
                        stats += $"CC: +{critChance}%{Environment.NewLine}";
                        break;
                    default:
                        stats += $"CC: {critChance}%{Environment.NewLine}";
                        break;
                }
            if (critMult != 0)
                switch (type)
                {
                    case AType.Enchantment:
                        stats += $"CD: x{100+critMult}%{Environment.NewLine}";
                        break;
                    case AType.Martial:
                        stats += $"CD: +{critMult}%{Environment.NewLine}";
                        break;
                    default:
                        stats += $"CD: {critMult}%{Environment.NewLine}";
                        break;
                }
            if (agility != 0)
                switch (type)
                {
                    case AType.Enchantment:
                        stats += $"AGI: +{agility}%{Environment.NewLine}";
                        break;
                    case AType.Martial:
                        stats += $"AGI: {agility}{Environment.NewLine}";
                        break;
                    default:
                        stats += $"AGI: {agility}{Environment.NewLine}";
                        break;
                }
            //
            for (int i = 0; i < damage.Length; i++)
                if (damage[i] > 0)
                {
                    switch(type)
                    {
                        case AType.Enchantment:
                            stats += $"{ReferenceData.DmgType[i]} DMG: +{damage[i]}% {Environment.NewLine}";
                            break;
                        case AType.Martial:
                            stats += $"{ReferenceData.DmgType[i]} DMG: +{damage[i]}{Environment.NewLine}";
                            break;
                        default:
                            stats += $"{ReferenceData.DmgType[i]} DMG: {damage[i]}{Environment.NewLine}";
                            break;
                    }
                    
                }
            return stats;
        }
        string GetDescription()
        {
            string desc = "";
            if (maxLevel > 0)
                desc += $"Level: {level}";
            if(level < maxLevel)
                desc += $"| {DetailedXP()} XP";
            desc += $" |__{type.ToString()}__|{Environment.NewLine}{description}";
            return desc;
        }

        internal bool IsType(params AType[] types)
        {
            foreach (var t in types)
                if (type == t)
                    return true;
            return false;
        }

        internal bool IsOffense => 
            type == AType.Martial || type == AType.Enchantment || type == AType.Elemental;
    }
}