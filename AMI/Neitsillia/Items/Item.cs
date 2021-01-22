using AMI.AMIData;
using AMI.Methods;
using AMI.Methods.Graphs;
using AMI.Neitsillia;
using AMI.Neitsillia.Crafting;
using AMI.Neitsillia.Items;
using AMI.Neitsillia.Items.Perks.PerkLoad;
using AMI.Neitsillia.User.UserInterface;
using AMYPrototype;
using Discord;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Neitsillia.Items.Item
{
    [BsonIgnoreExtraElements]
    public class Item
    {
        internal static MongoDatabase Database => Program.data.database;
        internal static StatsWeights sw = new StatsWeights();
        //static readonly double economy = ReferenceData.economy;

        #region vars

        public string originalName;
        public string name;
        //internal string path;
        public string description;
        public IType type;
        public long baseValue;
        //Item Stats
        public long[] damage;
        public int[] resistance;
        public long healthBuff;
        public int staminaBuff;
        public int agility;
        public double critChance;
        public double critMult;
        //
        public int durability;
        public int condition;
        public bool isUnique = false;
        //
        public int baseTier;
        public int tier;
        public Schematic schematic;
        public Perk perk;

        #endregion

        public enum IType {
            Material,//0
            Healing, Consumable, //1,2
            Usable, Mysterybox, //3,4
            Jewelry, Helmet, Trousers, Mask, Chestp, Boots, //5-10
            Weapon, //11
            notfound, //12
            Schematic, BuildingBlueprint, //13, 14
            RepairKit, Rune, EssenseVial
        };
        public static string[] mysterybox = new string[]
            {"None", "Small Mystery Bag"};
        public static string[] resourcerations = new string[]
            {"None", "Small Resource Ration"};
        //

        public override string ToString()
        {
            if (CanBeEquip())
                return name + " |" + (condition > 0 ? ((condition * 100) / durability).ToString() + '%' : "Broken");
            else
                return name;
        }
        public string TypeToString()
        {
            switch (type)
            {
                case IType.Chestp: return "Cheat Piece";
                default: return type.ToString();
            }
        }

        public static Predicate<Item> FindWithName(Item argItem)
        {
            return delegate (Item item) { return item.name == argItem.name; };
        }
        public static Predicate<Item> FindWithName(string argItem)
        {
            return delegate (Item item) { return item.name == argItem; };
        }

        #region Loading
        [JsonConstructor]
        public Item(bool jsonOnly) { }
        private Item(int level, string name, IType type)
        {
            originalName = name;
            this.name = name;
            this.type = type;

            baseTier = level * 5;
            tier = baseTier;
            durability = tier;
            condition = durability;
        }

        internal int Scale(int level)
        {
            if (level < 1 || tier > level || level <= Math.Ceiling(tier / 5.00) * 5) return 1;
            int t = tier;
            switch (type)
            {
                case IType.Weapon:

                    if (tier < 5)
                    {
                        damage[0] += 5;
                        CalculateStats(true);
                    }

                    sw.ScaleWeapon(this, level);
                    CalculateStats(true);
                    break;

                case IType.Boots:
                case IType.Chestp:
                case IType.Helmet:
                case IType.Jewelry:
                case IType.Mask:
                case IType.Trousers:
                    sw.ScaleArmor(this, level);
                    CalculateStats(true);
                    break;

                case IType.Consumable:
                case IType.Healing:
                    return level;

                default:
                    return 1;
            }
            if (level != Math.Ceiling(tier / 5.00) * 5)
                Console.WriteLine($"Inaccurate {originalName} scaling: {t}:{level} => {tier}");
            return 1;
        }

        public static Item LoadItem(string name, params string[] tables)
        {
            if (tables.Length < 1)
                tables = new string[] { "Item", "Skavi", "Unique Item", "Event Items" };

            Item i = null;
            for (int k = 0; k < tables.Length && i == null; k++)
                i = Database.LoadRecord(tables[k], MongoDatabase.FilterEqual<Item, string>("_id", name));

            if (i != null)
            {
                i.LoadPerk();
                if (i.CanBeEquip() && i.tier < 20)
                {
                    i.durability += 20 - i.tier;
                    i.CalculateStats(true);
                }
                return i;
            }
            Log.LogS($"Item {name} was not found");
            return null;
        }
        #endregion

        #region Temp Schematic
        public static Item NewTemporarySchematic(Item item)
        {
            if (item.schematic == null)
                return null;
            Item tempschem = new Item(false)
            {
                name = $"Schematic : {item.originalName}",
                originalName = $"Schematic : {item.originalName}",
                tier = item.baseTier,

                type = IType.Schematic,
                schematic = item.schematic,
                description = $"A schematic too complex to learn for {item.name}. [You must \"Use\" this item to craft it.]",
            };
            if (!item.isUnique)
                tempschem.baseValue = item.baseValue / 2;
            if (tempschem.schematic.path == null && Database.IdExists<Item, string>("Item", item.originalName))
                tempschem.schematic.path = item.originalName;

            return tempschem;
        }
        public static Item NewTemporarySchematic(string itemName) => NewTemporarySchematic(LoadItem(itemName));

        public static Item NewTemporarySchematic(int randomItemTier, string table = "Skavi")
        {
            List<Item> choices = null;
            int minT = randomItemTier - 2;
            int maxT = randomItemTier + 2;
            while (choices == null || choices.Count < 1)
            {
                minT--;
                maxT++;
                choices = Database.LoadRecords(table,
                    MongoDatabase.FilterLtAndGt<Item, int>("tier", maxT, minT));
            }
            return NewTemporarySchematic(choices[Program.rng.Next(choices.Count)]);
        }
        #endregion

        #region Usables

        public static Item CreateRune(int level)
        {
            level = Verify.MinMax(level, 10, 1);
            return new Item(level, "Rune " + NumbersM.GetLevelMark(level), IType.Rune);
        }

        public static Item CreateRepairKit(int level)
        {
            level = Verify.MinMax(level, 10, 1);
            return new Item(level * 10, "Repair Kit " + NumbersM.GetLevelMark(level), IType.RepairKit);
        }

        public static Item CreateEssenseVial(int tier)
        {
            tier = Verify.MinMax(tier, 1);
            string name = AMI.Neitsillia.Items.Abilities.Specter.Get(tier);
            int level = tier * AMI.Neitsillia.Items.Abilities.Specter.TierLevel;
            return CreateEssenseVial(name, level);
        }

        public static Item CreateEssenseVial(string name, int level = -1)
        {
            if (level < 0) level = Ability.Load(name).tier;
            return new Item(true)
            {
                originalName = name,
                name = $"{name} Essence Vial",
                type = IType.EssenseVial,

                baseTier = level,
                tier = level,
                durability = level + 1,
                condition = level + 1
            };
        }

        #endregion

        internal static Item BuildingSchematic(string v)
        {
            int[] i = ArrayM.GetArrayIndex(AMI.Neitsillia.Areas.Strongholds.Building.AvailableBuildingSchematics, v);
            if (i[0] > -1)
            {
                return new Item(false)
                {
                    name = "Building Schematic :" + v,
                    originalName = "Building Schematic :" + v,
                    description = $"The blueprints to build a {v} in your stronghold. Deposit this item in your stronghold's storage to allow {v} construction in the stronghold.",
                    type = IType.BuildingBlueprint,
                    baseValue = i[0] + i[1],
                };
            }
            return null;
        }

        internal void LoadPerk()
        {
            Random rng = Program.rng;

            if (perk != null)
            {
                if (perk.name == null || perk.name.Length < 1)
                { perk = null; return; }
                if (perk.name == "-Random")
                {
                    switch (type)
                    {
                        case IType.Weapon: perk = PerkLoad.RandomPerk(perk.rank, "Weapon"); break;
                        case IType.Jewelry:
                            {
                                if (Program.rng.Next(101) <= 50)
                                    perk = PerkLoad.RandomPerk(perk.rank, "Weapon");
                                else
                                    perk = PerkLoad.RandomPerk(perk.rank, "Armor");
                            }
                            break;
                        case IType.Helmet:
                        case IType.Boots:
                        case IType.Chestp:
                        case IType.Mask:
                        case IType.Trousers:
                            perk = PerkLoad.RandomPerk(perk.rank, "Armor");
                            break;
                        case IType.Consumable:
                        case IType.Healing:
                            perk = PerkLoad.RandomPerk(perk.rank, "Status");
                            break;
                    }
                }
                else switch (type)
                    {
                        case IType.Weapon:
                        case IType.Jewelry:
                        case IType.Helmet:
                        case IType.Boots:
                        case IType.Chestp:
                        case IType.Mask:
                        case IType.Trousers:
                            perk = PerkLoad.Load(perk.name);
                            break;
                        case IType.Consumable:
                        case IType.Healing:
                            perk = PerkLoad.Effect(perk.name, tier / 2, tier);
                            break;
                    }

            }
        }

        internal bool VerifyItem(bool isNew = false)
        {
            bool issue = false;
            if (isNew && CanBeEquip())
            {
                originalName = name;
                condition = durability;
            }
            else
            {
                if (originalName == null)
                    originalName = name;
                if (perk != null &&
                    (perk.name == null || perk.name.Trim() == ""))
                    perk = null;
            }
            if (schematic != null && schematic.exists)
            {
                if (schematic.name == null || schematic.name.Trim() == "")
                    schematic.name = name; issue = true;
                if (schematic.path != originalName)
                    schematic.path = originalName;
            }
            if (damage == null)
                damage = new long[ReferenceData.DmgType.Length];
            if (resistance == null)
                resistance = new int[ReferenceData.DmgType.Length];
            while (damage.Length < ReferenceData.DmgType.Length)
            { damage = ArrayM.AddItem(damage, 0); issue = true; }
            while (resistance.Length < ReferenceData.DmgType.Length)
            { resistance = ArrayM.AddItem(resistance, 0); issue = true; }

            if (isNew)
            {
                CalculateStats();
                baseTier = tier;
            }
            return issue;
        }
        public void SaveItem(string table = "Item")
        {
            if (isUnique)
                table = "Unique Item";
            Database.UpdateRecord(table, MongoDatabase.FilterEqual<Item, string>("_id", originalName), this);
            Console.WriteLine(type + " : " + name + " Verified, Updated and Registered");
        }
        public async System.Threading.Tasks.Task SaveItemAsync(string table = "Item")
        {
            if (table == "Item" && isUnique) table = "Unique Item";
            await Database.UpdateRecordAsync(table, MongoDatabase.FilterEqual<Item, string>
                ("_id", originalName), this);
            Console.WriteLine(type + " : " + name + " Verified, Updated and Registered");
        }
        //
        public static Item RandomGear(int tier, bool jewelry = false)
            => RandomItem(tier, Program.rng.Next(jewelry ? 5 : 6, 12));
        public static Item RandomItem(int tier, int type = -1, bool cap = true)
        {
            if (tier < 10) tier = 10;
            else if (cap) tier = Math.Min(tier, 38 * 5);

            if (type < 0) type = Utils.RandomElement(0, 1, 2, 5, 6, 7, 8, 9, 10, 11);

            Item i = null;
            List<Item> choices = null;

            if (type >= 5)
            {
                choices = Database.LoadSortRecords<Item>("Item", $"{{type:{type}}}", "{tier:-1}");
                if (tier > choices[0].tier)
                {
                    choices = Database.LoadRecords("Item",
                            MongoDatabase.FilterEqual<Item, int>("type", type));
                }
                else
                {
                    choices = null;
                    int minT = tier - 25;
                    int maxT = tier;
                    while (choices == null || choices.Count < 1)
                    {
                        choices = Database.LoadRecords("Item",
                            MongoDatabase.FilterEqualAndLtAndGt<Item, int, int>("type", type, "tier",
                            tier, minT));

                        if (minT > 0) minT -= 5;
                        else maxT++;
                    }
                }

                i = Utils.RandomElement(choices);
                i.LoadPerk();
                i.Scale(tier);
            }
            else
            {
                choices = Database.LoadRecords("Item",
                MongoDatabase.FilterEqual<Item, int>("type", type));
                i = Utils.RandomElement(choices);
            }

            
            if (i.condition == 0) i.condition = 1;
            if (i.durability == 0) i.durability = 1;

            return i;
        }

        public bool CanBeEquip()
        {
            return ((int)type <= 11 && (int)type >= 5);
        }
        public long GetValue()
        {
            long newPrice = baseValue;
            if (CanBeEquip())
                newPrice = Convert.ToInt32(baseValue * Exponential.ItemValue(condition, durability));
            return newPrice;
        }

        #region Stats and info
        public EmbedBuilder EmdebInfo(EmbedBuilder embed)
        {
            VerifyItem(false);
            bool fullView = !(type == IType.Consumable || type == IType.Healing);
            string stats = null;
            embed.WithTitle(name);
            if (description == null || description == "")
                description = "This item's origins are unknown.";
            embed.AddField("Description", $"__{type.ToString()}__" + Environment.NewLine + description);
            //
            if (tier != 0)
                stats += $"Rank: {tier}";
            if (CanBeEquip() && tier > baseTier && fullView)
                stats += $"/{baseTier + 15}";
            stats += Environment.NewLine;
            if (fullView)
            {
                for (int i = 0; i < ReferenceData.DmgType.Length; i++)
                {
                    long res = resistance[i];
                    long dmg = damage[i];
                    if (dmg > 0 || res != 0)
                    {
                        stats += $"{EUI.GetElement((ReferenceData.DamageType)i)}: ";

                        if (dmg > 0)
                            stats += $"{EUI.attack}: {dmg} |-";
                        if (res != 0)
                            stats += $"-| {EUI.shield}: {res}";

                        stats += Environment.NewLine;
                    }
                }
                if (durability != 0 && fullView)
                    stats += "Durability: " + durability + Environment.NewLine;
            }
            if (healthBuff != 0)
                stats += "Health: " + healthBuff + Environment.NewLine;
            if (staminaBuff != 0)
                stats += "Stamina: " + staminaBuff + Environment.NewLine;
            if (fullView)
            {
                if (agility != 0)
                    stats += "Agility: " + agility + Environment.NewLine;
                if (critChance != 0)
                    stats += "Critical Chance: " + critChance + '%' + Environment.NewLine;
                if (critMult != 0)
                    stats += "Critical Damage: " + critMult + '%' + Environment.NewLine;
            }
            embed.AddField("Stats", stats +
                "Value: " + GetValue() + "~~K~~" + Environment.NewLine, true);
            //
            string itemrecipe = null;
            if (schematic != null)
                itemrecipe = schematic.GetRecipe();
            embed.AddField("Crafting", itemrecipe ?? "This item cannot be crafted", true);
            if (perk != null)
                embed.AddField(perk.name, perk.desc, true);
            return embed;
        }
        public string StatsInfo()
        {
            bool fullView = !(type == IType.Consumable || type == IType.Healing || type == IType.Schematic);
            string stats = null;
            if (tier != 0)
                stats += $"Rank: {tier}/{baseTier + 15}" + Environment.NewLine;
            if (fullView)
            {
                if(damage != null)
                for (int i = 0; i < ReferenceData.DmgType.Length; i++)
                    if (damage[i] != 0)
                        stats += ReferenceData.DmgType[i] + " Damage: " + damage[i] + Environment.NewLine;
                if (durability != 0)
                    stats += "Durability: " + durability + Environment.NewLine;
                if(resistance != null)
                for (int i = 0; i < ReferenceData.DmgType.Length; i++)
                    if (resistance[i] != 0)
                        stats += ReferenceData.DmgType[i] + " Resistance: " + resistance[i] + Environment.NewLine;
            }
            if (healthBuff != 0)
                stats += "Health: " + healthBuff + Environment.NewLine;
            if (staminaBuff != 0)
                stats += "Stamina: " + staminaBuff + Environment.NewLine;
            if (fullView)
            {
                if (agility != 0)
                    stats += "Agility: " + agility + Environment.NewLine;
                if (critChance != 0)
                    stats += "Critical Chance: " + critChance + '%' + Environment.NewLine;
                if (critMult != 0)
                    stats += "Critical Damage: " + critMult + '%' + Environment.NewLine;
            }
            if (perk != null)
                stats += $"Perk: {perk.name} {Environment.NewLine} {perk.desc}";

            if (stats == null)
                stats = "Error, all stats = 0";
            return stats;
        }

        public List<float> GetStatList(bool isForWeapon)
        {
            VerifyItem();
            var l = new List<float>
            {healthBuff, durability, agility,
            (float)critChance, (float)critMult,
            staminaBuff};
            for (int i = 0; i < damage.Length; i++)
            {
                if(isForWeapon)
                    l.Add(damage[i]);
                else
                    l.Add(resistance[i]);
            }
            return l;
        }
        internal int CalculateStats(bool rebase = false)
        {
            int oldRank = this.tier;
            long total = 0;
            //damage
            for (int i = 0; i < damage.Length; i++)
                total += damage[i] * sw.damage;
            //resistance
            for (int i = 0; i < resistance.Length; i++)
                total += resistance[i] * sw.resistance;
            //stats
            total += (healthBuff * sw.health) + durability
            + agility + (staminaBuff*sw.stamina) +
            Convert.ToInt64(critChance) + Convert.ToInt64(critMult);
            //
            if (perk != null)
                total += (sw.perk * perk.tier) + sw.perk;
            baseValue = total;
            //
            tier = Convert.ToInt32(total * 0.07);
            if (rebase)
                baseTier = tier;
            return oldRank;
        }

        internal string CompareTo(AMI.Neitsillia.Collections.Equipment equipment)
        {
            Item eq = equipment.GetGear(type);
            return CanBeEquip() && equipment != null ?

                eq == null || tier > eq.tier ? EUI.greaterthan
                : tier < eq.tier ? EUI.lowerthan
                : EUI.equalStats

                : null;
        }
        internal EmbedBuilder CompareTo(Item compared, int cIsEquipped = -1)
        {
            EmbedBuilder embed = new EmbedBuilder();
            VerifyItem(false);
            bool fullView = !(type == IType.Consumable || type == IType.Healing);
            string stats = null;
            embed.WithTitle($"{name}{(cIsEquipped == 0 ? " [Equipped]" : null)} -> {compared.name}{(cIsEquipped == 1 ? " [Equipped]" : null)}");

            embed.WithDescription($"__{type.ToString()}__ -> __{compared.type.ToString()}__");
            //
            if (tier != 0)
                stats += CompareStat("Rank", tier, compared.tier) + Environment.NewLine;

            if (fullView)
            {
                for (int i = 0; i < ReferenceData.DmgType.Length; i++)
                    if (damage[i] != 0 || compared.damage[i] != 0)
                        stats += CompareStat($"{EUI.attack} {EUI.GetElement(i)}", damage[i], compared.damage[i]) + Environment.NewLine;

                if (durability != 0 || compared.durability != 0)
                    stats += CompareStat("Durability", durability, compared.durability) + Environment.NewLine;

                for (int i = 0; i < ReferenceData.DmgType.Length; i++)
                    if (resistance[i] != 0 || compared.resistance[i] != 0)
                        stats += CompareStat($"{EUI.shield} {EUI.GetElement(i)}", resistance[i], compared.resistance[i]) + Environment.NewLine;
            }

            if (healthBuff != 0 || compared.healthBuff != 0)
                stats += CompareStat("Health", healthBuff, compared.healthBuff) + Environment.NewLine;
            if (staminaBuff != 0 || compared.staminaBuff != 0)
                stats += CompareStat("Stamina", staminaBuff, compared.staminaBuff) + Environment.NewLine;

            if (fullView)
            {
                if (agility != 0 || compared.agility != 0)
                    stats += CompareStat("Agility", agility, compared.agility) + Environment.NewLine;
                if (critChance != 0 || compared.critChance != 0)
                    stats += CompareStat("Critical Chance", critChance, compared.critChance) + '%' + Environment.NewLine;
                if (critMult != 0 || compared.critMult != 0)
                    stats += CompareStat("Critical Damage", critMult, compared.critMult) + '%' + Environment.NewLine;
            }
            embed.AddField("Stats", stats +
                CompareStat("Value", GetValue(), compared.GetValue()) + "~~K~~" + Environment.NewLine, true);

            return embed;
        }
        string CompareStat(string statName, int t, int c)
        {
            return $"{statName}: {t} {(t > c ? EUI.lowerthan : t == c ? EUI.equalStats : EUI.greaterthan)} {c}";
        }
        string CompareStat(string statName, long t, long c)
        {
            return $"{statName}: {t} {(t > c ? EUI.lowerthan : t == c ? EUI.equalStats : EUI.greaterthan)} {c}";
        }
        string CompareStat(string statName, double t, double c)
        {
            return $"{statName}: {t} {(t > c ? EUI.lowerthan : t == c ? EUI.equalStats : EUI.greaterthan)} {c}";
        }
        #endregion

        #region Upgrading
        internal int Upgrade(int buff, params int[] v)
        {
            VerifyItem();
            for (int i = 0; i < v.Length; i++)
                switch (v[i])
                {
                    case -1: break;
                    case 0: //Health
                            healthBuff += buff;
                        break;
                    case 1: //Durability
                        {
                            int plus = buff;
                            durability += plus;
                            if(plus > 0)
                                condition += plus;
                        }
                        break;
                    case 2: //Agility
                            agility += buff;
                        break;
                    case 3: //CC
                            critChance += buff;
                        break;
                    case 4: //CM
                            critMult += buff;
                        break;
                    case 5: //CM
                        staminaBuff += buff;
                        break;
                    default:
                        {
                            int c = v[i] - 6;
                            if (c > -1)
                            {
                                if (type == IType.Weapon)
                                    damage[c] += buff;
                                else
                                    resistance[c] += buff;
                            }
                        }
                        break;
                }
                int or = CalculateStats();
            if(baseTier - tier <= 11)
                name = originalName +
                    " " + NumbersM.GetLevelMark(tier - baseTier);
            return or;
        }
        internal int Upgrade(int mult, Item giver, params int[] v)
        {
            VerifyItem(false);
            bool uped = true;
            for (int i = 0; i < v.Length; i++)
                switch (v[i])
                {
                    case -1: break;
                    case 0: //Health
                        healthBuff += giver.healthBuff * mult;
                        break;
                    case 1: //Durability
                        {
                            int plus = giver.durability * mult;
                            durability += plus;
                            if (plus > 0)
                                condition += plus;
                        }
                        break;
                    case 2: //Agility
                        agility += giver.agility * mult;
                        break;
                    case 3: //CC
                        critChance += giver.critChance * mult;
                        break;
                    case 4: //CM
                        critMult += giver.critMult * mult;
                        break;
                    case 5: //CM
                        staminaBuff += giver.staminaBuff * mult;
                        break;
                    default:
                        {
                            int c = v[i] - 6;
                            if (c > -1)
                            {
                                if (type == IType.Weapon)
                                    damage[c] += giver.damage[c] * mult;
                                else
                                    resistance[c] += giver.resistance[c] * mult;
                            }
                        }
                        break;
                }
            if (uped)
            {
                int or = CalculateStats();
                if (baseTier - tier <= 11)
                    name = originalName +
                    " " + NumbersM.GetLevelMark(tier - baseTier);
                return or;
            }
            return tier;
        }

        internal string Rename(string newname)
        {
            if(newname == "~rng")
            {
                newname = Methods.RandomName.ARandomName(6, true);
                int x = Program.rng.Next(101);
                switch(type)
                {
                    case IType.Weapon:
                        {
                            if(x > 50)
                            return Rename(newname + " Blade");
                            return Rename("The " + newname);
                        } 
                    case IType.Helmet:
                        {
                            return Rename(newname + " Helmet");
                        }
                        
                    case IType.Mask:
                        {
                            return Rename(newname + " Mask");
                        }
                        
                    case IType.Chestp:
                        {
                            if( x >= 50)
                            return Rename(newname + " Plates");
                            return Rename(newname + " Guard");
                        }
                        
                    case IType.Trousers:
                        {
                            return Rename(newname + " Trousers");
                        }
                        
                    case IType.Boots:
                        {
                            return Rename(newname + " Boots");
                        }
                    default: return Rename(newname);
                        
                }
            }
            else
            {
                name = newname;
                description = $"A custom modified version of the {originalName}";
            }
            return name;
        }
        #endregion
    }
}
