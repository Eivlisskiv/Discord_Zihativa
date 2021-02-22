using Neitsillia.Items.Item;
using System;

namespace AMI.Neitsillia.Collections
{
    public class StackedItems
    {
        public Item item;
        public int count;

        public StackedItems() { }

        public StackedItems(string it, int count1)
        {
            item = Item.LoadItem(it);
            count = count1;
        }
        public StackedItems(Item it, int count1)
        {
            item = it;
            count = count1;
        }
        public static Predicate<StackedItems> FindWithRank(int rank)
        {
            return delegate (StackedItems sitem) { return sitem.item.tier == rank; };
        }
        public static Predicate<StackedItems> FindWithName(Item argItem)
        {
            return delegate (StackedItems sitem) { return sitem.item.name == argItem.name; };
        }
        public static Predicate<StackedItems> FindWithName(string argItem)
        {
            return delegate (StackedItems sitem) { return sitem.item.name == argItem; };
        }

        internal static Predicate<StackedItems> FindWithType(Item.IType type)
        {
            return delegate (StackedItems sitem) { return sitem.item.type == type; };
        }

        public override string ToString()
            => item.CanBeEquip() ? $"{item.Rarity}`{item.name} |CND: " +
            (item.durability > 0 ? $"{(item.condition * 100) / item.durability}%`" : "Broken`") 
            : $"`{count}x {item.name}`";
    }
}
