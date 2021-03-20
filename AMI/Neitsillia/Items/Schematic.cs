using AMI.Neitsillia.Collections;
using AMI.Neitsillia.Items.Perks.PerkLoad;
using AMI.Neitsillia.NPCSystems;
using AMI.Neitsillia.User.PlayerPartials;
using AMI.Neitsillia.Items.ItemPartials;
using System;

namespace AMI.Neitsillia.Crafting
{
    public class Schematic
    {
        public string name;
        public bool exists = true;
        public string path;

        public bool reusable = true;

        public string underMat;//0
        public int underAmount;
        public string mainMat;//1
        public int mainAmount;
        public string tyingMat;//2
        public int tyingAmount;
        public string secondaryMat;//3
        public int secondaryAmount;
        public string specialMat;//4
        public int specialAmount;
        public string skaviMat;//5
        public int skaviAmount;

        public Schematic()
        { }
        public static Predicate<Schematic> FindWithName(string argItem)
        {
            return delegate (Schematic item) { return item.name == argItem; };
        }
        public string CheckPlayerInventoryForMaterial(Player player, int materialAmountModifier = 1)
        {
            string result = null;
            for (int i = 0; i < 5; i++)
            {
                StackedObject<string, int> mats = GetMaterial(i);
                mats.count *= materialAmountModifier;
                if (mats.count > 0 && FindMaterial(player, mats) < 0)
                    result += $"Missing required {mats.count}x {mats.item} {Environment.NewLine}";
            }
            return result;
        }
        internal void ConsumeSchematicItems(Player player, int materialMultiplier)
        {
            for (int i = 0; i < 5; i++)
            {
                StackedObject<string, int> mats = GetMaterial(i);
                if (mats.count > 0)
                    player.inventory.Remove(FindMaterial(player, mats), mats.count);
            }
        }
        public StackedItems Craft(Player player, int amount, out string details)
        {
            details = null;
            Item item = null;
            if (player.schematics.Find(FindWithName(name)) != null)
            {
                details = CheckPlayerInventoryForMaterial(player);
                item = Item.LoadItem(path);
                if (item.CanBeEquip())
                    amount = 1;
                if (!player.inventory.CanContain(item, amount, player.InventorySize()))
                    details += "Inventory Full";
                //To finish
                if (details == null)
                {
                    item = CraftItem(player, item);
                    details = name + " Crafted";
                }
            }
            else
                details = "You do not possess this schematic";
            return new StackedItems(item, amount);
        }
        public Item Craft(Player player, ref string details)
        {
            details = null;
            Item item = null;
            if (player.schematics.Find(FindWithName(name)) != null)
            {
                details = CheckPlayerInventoryForMaterial(player);
                if (!player.inventory.CanContain((item = Item.LoadItem(path)), 1, player.InventorySize()))
                {
                    details += "Inventory Full";
                    return null;
                }
                if (details == null)
                {
                    details = name + " Crafted";
                    return CraftItem(player, item);
                    
                }
            }
            else
                details = "You do not possess this schematic";
            return null;
        }
        private Item CraftItem(Player player, Item crafted)
        {
            ConsumeSchematicItems(player, 1);
            crafted = PerkLoad.CheckPerks(player,
                Items.Perk.Trigger.Crafting, crafted);

            if (crafted.isUnique) crafted.schematic = null;
            crafted.CalculateStats(true);

            player.CollectItem(crafted, 1);
            return crafted;
        }
        private int FindMaterial(Player player, StackedObject<string, int> materials)
        {
            int index = player.inventory.FindIndex(materials.item);
            if (index > -1 && player.inventory.GetCount(index) >= materials.count)
                return index;
            return -1;
        }
        public string GetRecipe()
        {
            string recipe = null;
            if (mainAmount > 0)
                recipe += mainAmount + "x " + mainMat + Environment.NewLine;
            if(underAmount > 0)
                recipe += underAmount + "x " + underMat + Environment.NewLine;
            if (tyingAmount > 0)
                recipe += tyingAmount + "x " + tyingMat + Environment.NewLine;
            if (secondaryAmount > 0)
                recipe += secondaryAmount + "x " + secondaryMat + Environment.NewLine;
            if (specialAmount > 0)
                recipe += specialAmount + "x " + specialMat + Environment.NewLine;
            if (skaviAmount > 0)
                recipe += skaviAmount + "x " + skaviMat + Environment.NewLine;

            if(recipe == null)
                return "This Item has no crafting recipe.";
            return recipe;
        }
        public override string ToString()
        {
            return name;
        }
        ///NPC
        public bool Craft(NPC npc, ref string details)
        {
            details = null;
            bool canCraft = false;
            if (npc.schematics.Find(FindWithName(name)) != null)
            {
                if (underAmount > 0 && FindMaterial(npc, underMat, underAmount) < 0)
                    details += "Missing required " + underMat + Environment.NewLine;
                if (mainAmount > 0 && FindMaterial(npc, mainMat, mainAmount) < 0)
                    details += "Missing required " + mainMat + Environment.NewLine;
                if (tyingAmount > 0 && FindMaterial(npc, tyingMat, tyingAmount) < 0)
                    details += "Missing required " + tyingMat + Environment.NewLine;
                if (secondaryAmount > 0 && FindMaterial(npc, secondaryMat, secondaryAmount) < 0)
                    details += "Missing required " + secondaryMat + Environment.NewLine;
                if (specialAmount > 0 && FindMaterial(npc, specialMat, specialAmount) < 0)
                    details += "Missing required " + specialMat + Environment.NewLine;
                if (skaviAmount > 0 && FindMaterial(npc, skaviMat, skaviAmount) < 0)
                    details += "Missing required " + skaviMat + Environment.NewLine;
                if (details == null)
                {
                    canCraft = true;
                    CraftItem(npc);
                    details = name + " Crafted";
                }
            }
            else
                details = "You do not possess this schematic";
            return canCraft;
        }
        private int FindMaterial(NPC npc, string matName, int matAmount)
        {
            int index = npc.inventory.FindIndex(matName);
            if (index > -1 && npc.inventory.GetCount(index) >= matAmount)
                return index;
            return -1;
        }
        void CraftItem(NPC npc)
        {
            if (underAmount > 0)
                npc.RemoveInvItem(FindMaterial(npc, underMat, underAmount), underAmount);
            if (mainAmount > 0)
                npc.RemoveInvItem(FindMaterial(npc, mainMat, mainAmount), mainAmount);
            if (tyingAmount > 0)
                npc.RemoveInvItem(FindMaterial(npc, tyingMat, tyingAmount), tyingAmount);
            if (secondaryAmount > 0)
                npc.RemoveInvItem(FindMaterial(npc, secondaryMat, secondaryAmount), secondaryAmount);
            if (specialAmount > 0)
                npc.RemoveInvItem(FindMaterial(npc, specialMat, specialAmount), specialAmount);
            if (skaviAmount > 0)
                npc.RemoveInvItem(FindMaterial(npc, skaviMat, skaviAmount), skaviAmount);
            npc.AddItemToInv(Item.LoadItem(path));
            npc.XpGain(100 * npc.level);
        }
        //
        internal StackedObject<string, int> GetMaterial(int index)
        {
            switch(index)
            {
                case 0: return new StackedObject<string, int>(underMat, underAmount);
                case 1: return new StackedObject<string, int>(mainMat, mainAmount);
                case 2: return new StackedObject<string, int>(tyingMat, tyingAmount);
                case 3: return new StackedObject<string, int>(secondaryMat, secondaryAmount);
                case 4: return new StackedObject<string, int>(specialMat, specialAmount);
                case 5: return new StackedObject<string, int>(skaviMat, skaviAmount);
            }
            return null;
        }
    }
}
