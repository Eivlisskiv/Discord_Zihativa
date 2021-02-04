using AMI.Neitsillia.Collections;
using Discord;
using System;

namespace AMI.Neitsillia
{
    public static class ReferenceData
    {
        public const string olduserPath = @".\Users\";
        public const string oldareaPath = @".\Data\Areas\";
        public const string olditemsPath = @".\Data\Items\";
        public const string oldskaviPath = @".\Data\Items\Skavi\";
        public const string oldmobPath = @".\Data\Creatures\";
        public const int maxSkillRoll = 8;
        //
        public const int maxCharacterCount = 3;
        //
        public const int dailyCoinsRates = 8;
        public const double economy = 1;
        public const int xpToLvlMult = 73;
        public const int xpToLvlAbility = 500;

        public const int GearLevelRequirement = 10;

        internal static string StatsAcronym(int i)
        {
            switch (i)
            {
                case 0: return "END";
                case 1: return "INT";
                case 2: return "STR";
                case 3: return "CHA";
                case 4: return "DEX";
                case 5: return "PER";
            }
            return null;
        }

        public const double xprate = 1;
        public const long strongholdCostperSize = 1000000;

        public const int CrateRate = 1;
        //Combat
        public const int maximumAgilityDifference = 25;
        public const int hitChance = 70;
        public static string[] DmgType => Enum.GetNames(typeof(DamageType));
        public enum DamageType { Physical, Blaze, Cold, Toxic, Electric }

        public const double levelscaling = 0.10;
        //Races
        public enum HumanoidRaces { Human, Tsiun, Uskavian, Miganan, Ireskian };
        public enum Profession { Creature,
            Child, Peasant, Merchant, Gladiator, Adventurer, //Fighter
            Blacksmith, //Blacksmith
            Alchemist, Tapster //Healer
        };
        public enum CombatRole { None, Healer, Fighter}
        //items
        public enum EquipmentsSlot {Boots, ChestP, Helmet, Trousers, Weapon, Mask};

        public enum ResourceCrate { Wooden, Bronze, Silver, Golden, Platinum }

        internal static string Version(int i = 0)
        {
            string[] nums = currentVersion.Split('.');
                
            if (i == 1) return $"{versionState} {nums[1]}.{nums[2]} ";
            else if (i==2) return $"{versionTitle} {nums[3]}.{nums[4]}";

            return $"{versionState} {nums[1]}.{nums[2]} " +
                $"{versionTitle} {nums[3]}.{nums[4]}";
        }
        public const string currentVersion = "0.7.0.3.5";
        internal const string versionState = "Beta";
        internal const string versionTitle = "Walls of Stone";

        public static EmbedBuilder HelpSkills(EmbedBuilder h)
        {
            h.AddField("Skills",
                EnduranceInfo() + Environment.NewLine +
                 IntelligenceInfo() + Environment.NewLine +
                 StrengthInfo() + Environment.NewLine +
                 CharismaInfo() + Environment.NewLine +
                 DexterityInfo() + Environment.NewLine +
                 PerceptionInfo() + Environment.NewLine +
                "");
            return h;
        }
        //
        internal static string StatInfo(int i, bool stat)
        {
            if(stat)
            switch(i)
            {
                    case 0: return EnduranceStat();
                    case 1: return IntelligenceStat();
                    case 2: return StrengthStat();
                    case 3: return CharismaStat();
                    case 4: return DexterityStat;
                    case 5: return PerceptionStat;

            }
            switch(i)
            {
                case 0: return EnduranceInfo();
                case 1: return IntelligenceInfo();
                case 2: return StrengthInfo();
                case 3: return CharismaInfo();
                case 4: return DexterityInfo();
                case 5: return PerceptionInfo();
            }
            return null;
        }
        public static string EnduranceStat()
        { return $"Endurance |>" +
                $"{Environment.NewLine} +{Stats.hpPerEnd} Health Points." +
                $"{Environment.NewLine}-{Stats.DurabilityPerEnd}x item durability loss"; }
        public static string IntelligenceStat()
        { return $"Intelligence |>" +
                $"{Environment.NewLine} +{Stats.aEfficiencyPerInt}x Ability Efficiency" +
                $"{Environment.NewLine} +{Stats.SchemDropRatePerInt}x Schematic gain upon dismantling"; }
        public static string StrengthStat()
        { return $"Strength |>" +
                $"{Environment.NewLine}+{Stats.dmgPerStr}x Physical Damage" +
                $"{Environment.NewLine}+{Stats.invSizePerStr} Inventory size"; }
        public static string CharismaStat()
        { return $"Charisma |>" +
                $"{Environment.NewLine}+{Stats.pricePerCha}x Better Trading Prices" +
                $""; }
        public static string DexterityStat => $"Dexterity |>" +
                $"{Environment.NewLine}+{Stats.passiveSp}% Passive stamina regenaration during combat{Environment.NewLine}" +
                $"+{Stats.restSpeed}(Seconds) Resting speed";
        public static string PerceptionStat => $"Perception |>" +
                $"{Environment.NewLine}+{Stats.critcPerPerc} Critical Chance" +
                $"{Environment.NewLine}+{Stats.LootPerPerc} Maximum loot chances";
        //
        public static string EnduranceInfo()
        {
            return $"Endurance increases base health and decreases the chance of losing gear durability";
        }
        public static string IntelligenceInfo()
        {
            return $"Intelligence increases the strength of some abilities and grants higher chances to learn schematics.";
        }
        public static string StrengthInfo()
        {
            return $"Strength increases physical damage and carrying capacity.";
        }
        public static string CharismaInfo()
        {
            return $"Charisma gets you better prices while trading and recruiting with NPCs.";
        }
        public static string DexterityInfo()
        {
            return $"Dexterity increases hit chances, total stamina and resting speed.";
        }
        public static string PerceptionInfo()
        {
            return $"Perception increases base critical hit chance and increases chances to find more items while looting.";
        }
    }
}
