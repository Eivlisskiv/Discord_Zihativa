namespace AMI.Neitsillia.Collections
{
    public class StackedObject<T, C>
    {
        public T item;
        public C count;

        public StackedObject(bool json) { }

        public StackedObject((T obj, C amount) stack)
        {
            this.item = stack.obj;
            this.count = stack.amount;
        }

        public StackedObject(T obj, C amount)
        {
            this.item = obj;
            this.count = amount;
        }

        public override string ToString()
        {
            return count.ToString() + "x " + item.ToString();
        }
    }
}
