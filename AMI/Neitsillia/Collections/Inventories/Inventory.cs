using AMI.Methods;
using AMI.Module;
using AMI.Neitsillia.User.UserInterface;
using Discord;
using Neitsillia.Items.Item;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AMI.Neitsillia.Collections
{
    public class Inventory
    {
        public static string Transfer(Inventory from, Inventory to, int toSize, string slotxamount)
        {
            (int index, int amount) = Verify.IndexXAmount(slotxamount);
            index--;
            if (index < 0 || index > from.Count) throw NeitsilliaError.ReplyError("Invalid slot");
            StackedItems si = from.Splice(index, amount);
            if(!to.Add(si, toSize)) throw NeitsilliaError.ReplyError("There is not enough inventory space for this transfer.");
            return si.ToString();
        }

        const int ITEM_PER_PAGE = 15;

        public List<StackedItems> inv = new List<StackedItems>();

        public int Count { get => inv.Count; }

        internal StackedItems this[int index] => inv[index];

        public Inventory() { }

        public Inventory(params StackedItems[] items)
            => inv.AddRange(items);

        public bool Add(Inventory i, int size)
        {
            bool b = false;
            foreach(var st in i.inv)
                b = Add(st.item, st.count, size);
            return b;
        }
        public bool Add(int size, params StackedItems[] items)
        {
            bool b = false;
            foreach (var st in items)
                b = Add(st.item, st.count, size);
            return b;
        }
        public bool Add(StackedItems i, int size)
        {
            return Add(i.item, i.count, size);
        }
        public bool Add(Item it, int amount, int size)
        {
            if (amount < 1) return true;
            if (!it.CanBeEquip())
            {
                int index = inv.FindIndex(StackedItems.FindWithName(it));
                if (index > -1)
                { inv[index].count += amount; return true; }
                else if (size <= -1 || inv.Count < size)
                {
                    inv.Add(new StackedItems(it, amount));
                    return true;
                }
            }
            else if (size <= -1 || inv.Count + amount <= size)
            {
                for (int i = 0; i < amount; i++)
                {
                    inv.Add(new StackedItems(it, 1));
                }
                return true;
            }
            return false;
        }

        public bool Remove(StackedObject<string, int> so)
        => Remove(FindIndex(so.item), so.count);
        public bool Remove(string item, int amount)
        => Remove(FindIndex(item), amount);
        public bool Remove(Item item, int amount)
        {
            int index = inv.FindIndex(StackedItems.FindWithName(item));
            return Remove(index, amount);
        }

        public bool Remove(int index, int amount)
        {
            if (index < 0) return false;
            else if (inv[index] == null || inv[index].item == null)
            { inv.RemoveAt(index); return true; }

            if (inv[index].count > amount) inv[index].count -= amount; 
            else inv.RemoveAt(index);

            return true;
        }

        internal StackedItems Splice(int index, int amount, bool remove = true)
        {
            StackedItems si = inv[index];
            if(si.count <= amount)
            {
                if (remove)
                {
                    inv.RemoveAt(index);
                    return si;
                }
                else
                {
                    new StackedItems(si.item, si.count);
                }
            }
            else if (remove) si.count -= amount;

            return new StackedItems(si.item, amount);
        }

        public Item GetItem(int index)
        {
            if (index > -1 && index < inv.Count)
            {
                //inv[index].item.VerifyItem(false);
                return inv[index].item;
            }
            return null;
        }
        public int GetCount(int index)
        {
            if (index < inv.Count)
                return inv[index].count;
            return 0;
        }

        public bool CanContain(Item it, int amount, int size)
        {
            if (!it.CanBeEquip())
            {
                int index = inv.FindIndex(StackedItems.FindWithName(it));
                if (index > -1)
                    return true;
                else if (size > -1 && inv.Count < size)
                    return true;
            }
            else if (size > -1 && inv.Count + amount <= size)
                return true;
            return false;
        }
        public bool CanContain(StackedItems t, int size) => CanContain(t.item, t.count, size);

        public int FindIndex(string matName) 
            => inv.FindIndex(StackedItems.FindWithName(matName));
        public int FindIndex(Item.IType type) 
            => inv.FindIndex(StackedItems.FindWithType(type));
        public int FindIndex(Item item)
            => inv.FindIndex(StackedItems.FindWithName(item));
        public int FindIndex(int rank)
            => inv.FindIndex(StackedItems.FindWithRank(rank));

        public IEnumerator GetEnumerator() 
            => inv.GetEnumerator();

        internal EmbedBuilder ToEmbed(ref int page, string inventoryName = "Inventory", int size = -1, Equipment comparison = null)
        {
            string filter = "none";
            return ToEmbed(ref page, ref filter, inventoryName, size, comparison);
        }
        internal EmbedBuilder ToEmbed(ref int page, ref string filter, string inventoryName = "Inventory", int size = -1, Equipment comparison = null)
        {
            
            int maxpage = Convert.ToInt32(Math.Ceiling((double)Count / ITEM_PER_PAGE));
            page = Verify.MinMax(page, maxpage - 1, 0);
            EmbedBuilder inventory = new EmbedBuilder();
            
            string itemList = null;
            filter = filter.ToLower();
            Func<Item, string, bool> filterFcuntion = GetFilter(ref filter);
            inventory.WithTitle(inventoryName + (filter == "all" ? null : $" ({filter})"));

            if (Count > 0)
            {
                for (int p = (ITEM_PER_PAGE * page), added = 0; 
                    added < ITEM_PER_PAGE && p < Count; p++)
                {
                    if (inv[p] == null) Remove(p, 1);
                    if (filterFcuntion == null || filterFcuntion(inv[p].item, filter))
                    {
                        itemList += $"{p + 1}| `{inv[p]}` "
                            + (comparison != null ? inv[p].item.CompareTo(comparison) : null)
                            + EUI.ItemType(inv[p].item.type)
                            + (added < ITEM_PER_PAGE && p < Count ? Environment.NewLine : null);
                        added++;

                        if(itemList.Length > 900)
                        {
                            inventory.AddField("Items", itemList ?? "Empty", false);
                            itemList = null;
                        }
                    }
                }
            }

            bool firstField = inventory.Fields.Count == 0;
            if(firstField || itemList != null)
                inventory.AddField(firstField ? "Items" : "...", itemList ?? "Empty", true);

            if (size == -1) size = Count;
            inventory.WithFooter($"{inventoryName} Capacity: {Count}/{size} | " +
                $"Page {(page + 1)}/{maxpage}");

            return inventory;
        }

        Func<Item, string, bool> GetFilter(ref string filter)
        {
            switch(filter)
            {
                case "mat":
                case "mats":
                    filter = "material";
                    return TypeFilter;
                case "health":
                    filter = "healing";
                    return TypeFilter;

                case "consumable":
                    return ConsumableFilter;

                case "essense":
                    filter = "essensevial";
                    return TypeFilter;

                case "material":
                case "healing":
                case "usable":
                case "jewelry":
                case "helmet":
                case "trousers":
                case "mask":
                case "chest":
                case "boots":
                case "weapon":
                case "schematic":
                case "repairkit": 
                case "rune":
                case "essensevial":
                    return TypeFilter;

                case "mysterybox":
                case "mystery box":
                    filter = "mysterybox";
                    return TypeFilter;

                case "bchematic":
                case "buildingblueprint":
                case "building blueprint":
                    filter = "buildingblueprint";
                    return TypeFilter;

                case "gear":
                case "equipment":
                    return IsGear;
                default:
                    filter = "all";
                    return null;
            }
        }

        internal bool Contains(StackedObject<string, int> so)
        => Contains(so.item, so.count);
        internal bool Contains(StackedItems si)
        => Contains(si.item.name, si.count);

        internal bool Contains(string matName, int count)
        {
            int i = FindIndex(matName);
            if (i < 0)
                return false;
            else return GetCount(i) >= count;
        }

        bool TypeFilter(Item i, string type)
         => i.type.ToString().ToLower().Equals(type.ToLower());

        bool ConsumableFilter(Item i, string type)
         => i.type == Item.IType.Healing || i.type == Item.IType.Consumable;

        bool IsGear(Item i, string t)
            => i.CanBeEquip();
    }
}
