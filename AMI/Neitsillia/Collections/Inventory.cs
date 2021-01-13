using AMI.Methods;
using AMI.Neitsillia.User.UserInterface;
using Discord;
using Neitsillia.Items.Item;
using System;
using System.Collections;
using System.Collections.Generic;

namespace AMI.Neitsillia.Collections
{
    public class Inventory
    {
        public List<StackedItems> inv = new List<StackedItems>();

        public int Count { get => inv.Count; }

        internal StackedItems this[int index] => inv[index];


        public Inventory() { }

        public Inventory(params StackedItems[] items)
        {
            inv.AddRange(items);
        }

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
            else
            {
                if (inv[index].count > amount) inv[index].count -= amount; 
                else inv.RemoveAt(index);

                return true;
            }
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
        public bool CanContain(StackedItems t, int size)
        {
            return CanContain(t.item, t.count, size);
        }

        public int FindIndex(string matName)
        {
            return inv.FindIndex(StackedItems.FindWithName(matName));
        }
        public int FindIndex(Item.IType type)
        {
            return inv.FindIndex(StackedItems.FindWithType(type));
        }
        public int FindIndex(Item item)
        {
            return inv.FindIndex(StackedItems.FindWithName(item));
        }
        public int FindIndex(int rank)
        {
            return inv.FindIndex(StackedItems.FindWithRank(rank));
        }

        public IEnumerator GetEnumerator()
        {
            return inv.GetEnumerator();
        }

        internal EmbedBuilder ToEmbed(ref int page, string inventoryName = "Inventory", int size = -1, Equipment comparison = null)
        {
            string filter = "none";
            return ToEmbed(ref page, ref filter, inventoryName, size, comparison);
        }
        internal EmbedBuilder ToEmbed(ref int page, ref string filter, string inventoryName = "Inventory", int size = -1, Equipment comparison = null)
        {
            const int itemPerPage = 15;
            int maxpage = Convert.ToInt32(Math.Ceiling((double)Count / itemPerPage));
            page = Verify.MinMax(page, maxpage - 1, 0);
            EmbedBuilder inventory = new EmbedBuilder();
            
            string itemList = null;
            Func<Item, string, bool> filterFcuntion = GetFilter(ref filter);
            inventory.WithTitle(inventoryName + (filter == "none" ? null : $" ({filter})"));

            if (Count > 0)
            {
                int max = (itemPerPage * (page + 1));
                for (int p = (itemPerPage * page); p < max
                      && p < Count; p++)
                {
                    if (inv[p] == null)
                        Remove(p, 1);
                    else if (filterFcuntion == null || filterFcuntion(inv[p].item, filter))
                        itemList += $"{p + 1}| `{inv[p]}` " 
                            + (comparison != null ? inv[p].item.CompareTo(comparison) : null) 
                            + EUI.ItemType(inv[p].item.type)
                            + (p < max && p < Count ? Environment.NewLine : null);

                }
                
            }

            inventory.AddField("Items", itemList ?? "Empty", true);

            if (size == -1) size = Count;
            inventory.WithFooter($"{inventoryName} Capacity: {Count}/{size} | " +
                $"Page {(page + 1)}/{maxpage}");
            return inventory;
        }

        Func<Item, string, bool> GetFilter(ref string filter)
        {
            switch(filter.ToLower())
            {
                case "mat":
                case "mats":
                    filter = "material";
                    return TypeFilter;
                case "health":
                    filter = "healing";
                    return TypeFilter;

                case "material":
                case "healing":
                case "consumable":
                case "usable":
                case "mysterybox":
                case "mystery box":
                case "jewelry":
                case "helmet":
                case "trousers":
                case "mask":
                case "chestp":
                case "boots":
                case "weapon":
                case "bchematic":
                case "buildingblueprint":
                case "building blueprint":
                    return TypeFilter;

                case "gear":
                case "equipment":
                    return IsGear;
                default:
                    filter = "none";
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
        {
            return i.type.ToString().ToLower().Equals(type.ToLower());
        }

        bool IsGear(Item i, string t)
        {
            return i.CanBeEquip();
        }
    }
}
