using AMI.AMIData;
using AMI.Methods;
using AMI.Methods.Graphs;
using AMI.Neitsillia.Crafting;
using AMI.Neitsillia.Items.Perks.PerkLoad;
using AMI.Neitsillia.User.UserInterface;
using AMYPrototype;
using Discord;
using MongoDB.Bson.Serialization.Attributes;
using Neitsillia.Methods;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace AMI.Neitsillia.Items.ItemPartials
{
    [BsonIgnoreExtraElements]
    public partial class Item
    {
        internal static MongoDatabase Database => Program.data.database;
        internal static StatsWeights sw = new StatsWeights();

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
        public int rarity;

        public Schematic schematic;
        public Perk perk;

        public string Rarity => EUI.ItemRarity(this);

        public string Name => $"{Rarity} {name}";
        #endregion

        public enum IType {
            Material,//0
            Healing, Consumable, //1,2
            Usable, Mysterybox, //3,4
            Jewelry, Helmet, Trousers, Mask, Chest, Boots, //5-10
            Weapon, //11
            notfound, //12
            Schematic,
            //14 - 17
            Scroll, RepairKit, Rune, EssenseVial, 
        };

        public override string ToString() 
            => CanBeEquip() ? $"{name} |" 
            + (condition > 0 ? ((condition * 100) / durability).ToString() + '%' : "Broken") : name;

        public string TypeToString()
            =>  type switch
            {
                IType.Chest => "Chest Piece",
                _ => type.ToString(),
            };

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
                case IType.Chest:
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
            rarity = tier / t;
            return 1;
        }

        private void RebaseDurability(int targetTier)
        {
            durability += targetTier - tier;
            condition = durability;
            CalculateStats(true);
        }
        #endregion

        internal void LoadPerk()
        {
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
                        case IType.Chest:
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
                        case IType.Chest:
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

        public bool CanBeEquip() 
            => ((int)type <= 11 && (int)type >= 5);

        public long GetValue() => CanBeEquip() ?
                Convert.ToInt32(baseValue * Exponential.ItemValue(condition, durability))
                : baseValue;

        #region Stats and info
        public EmbedBuilder EmdebInfo(EmbedBuilder embed)
        {
            VerifyItem(false);
            bool fullView = !(type == IType.Consumable || type == IType.Healing);
            string stats = null;
            embed.WithTitle(Name);
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
                embed.AddField(perk.name ?? "No perk", perk.desc ?? "No description", true);
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

        internal string CompareTo(Collections.Equipment equipment)
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
            embed.WithTitle($"{Name}{(cIsEquipped == 0 ? " [Equipped]" : null)} -> {compared.Name}{(cIsEquipped == 1 ? " [Equipped]" : null)}");

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
                newname = RandomName.ARandomName(6, true);
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
                        
                    case IType.Chest:
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
