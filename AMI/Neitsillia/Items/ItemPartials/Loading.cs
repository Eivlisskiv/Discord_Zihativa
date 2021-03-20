using AMI.AMIData;
using AMI.Methods;
using AMI.Neitsillia.Items.Abilities;
using AMYPrototype;
using System;
using System.Collections.Generic;

namespace AMI.Neitsillia.Items.ItemPartials
{
    public partial class Item
    {
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
                    i.RebaseDurability(20);
                return i;
            }
            Log.LogS($"Item {name} was not found");
            return null;
        }

        public static Item RandomGear(int tier, bool jewelry = false)
            => RandomItem(tier, Program.rng.Next(jewelry ? 5 : 6, 12));
        public static Item RandomItem(int tier, int type = -1, bool cap = true)
        {
            if (tier < 10) tier = 10;
            else if (cap) tier = Math.Min(tier, 38 * 5);

            if (type < 0) type = Utils.RandomElement(0, 1, 2, 5, 6, 7, 8, 9, 10, 11);

            Item i;
            List<Item> choices;

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
                            maxT, minT));

                        if (minT > 0) minT -= 5;
                        else maxT++;
                    }
                }

                i = Utils.RandomElement(choices);
                i.LoadPerk();
                if (i.CanBeEquip() && i.tier < 20) i.RebaseDurability(20);
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
    }
}
