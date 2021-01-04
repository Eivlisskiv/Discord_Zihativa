using System.Collections.Generic;

namespace AMI.Neitsillia.Collections
{
    class ObjectInventory<T, C>
    {
        public int Count => list.Count;
        List<StackedObject<T, C>> list = new List<StackedObject<T, C>>();
        public StackedObject<T, C> this[int index] => list[index];

        public void Add(T t, C c) => list.Add(new StackedObject<T, C>(t, c));
    }
}
