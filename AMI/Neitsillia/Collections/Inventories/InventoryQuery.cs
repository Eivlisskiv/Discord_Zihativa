using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AMI.Neitsillia.Collections.Inventories
{
    public class InventoryQuery
    {
        private readonly Inventory inventory;

        private readonly string name;

        private int? _index;

        private int Index => _index ?? Find();

        public InventoryQuery(Inventory inv, string name)
        {
            inventory = inv;
            this.name = name;
        }

        private int Find()
        {
            _index = inventory.inv.FindIndex(i => 
                i.item.originalName.Equals(
                    name, StringComparison.OrdinalIgnoreCase));
            return _index.Value;
        }

        public bool Contains(int amount, out int containedAmount)
        {
            containedAmount = 0;
            return Index != -1 &&
            (containedAmount = inventory.GetCount(Index)) >= amount; 
        }

        public bool Remove(int amount)
            => inventory.Remove(Index, amount);
    }
}
