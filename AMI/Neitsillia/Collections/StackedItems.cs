using Neitsillia.Items.Item;
using System;

namespace AMI.Neitsillia.Collections
{
    class StackedItems
    {
        public Item item;
        public int count;

        public StackedItems() { }

        public StackedItems(string it, int count1)
        {
            this.item = Item.LoadItem(it);
            this.count = count1;
        }
        public StackedItems(Item it, int count1)
        {
            this.item = it;
            this.count = count1;
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
        {
            if (item.CanBeEquip() && item.durability > 0)
                return $"{item.name} |CND: " + ((item.condition * 100) / item.durability) + '%';
            else if (item.CanBeEquip() && item.durability <= 0)
                return item.name + " |CND: Broken";
            else
                return $"{count}x {item.name}";
        }
    }
}
