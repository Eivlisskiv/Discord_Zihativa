namespace AMI.Neitsillia.Areas
{
    class SandBoxStats
    {
        public int MaximumPopulation;
        public int StorageSpace;
        public long Defenses;

        public SandBoxStats()
        { SetDefaults(); }

        internal void SetDefaults()
        {
            MaximumPopulation = 5;
            StorageSpace = 15;
            Defenses = 0;
        }

        internal void Add(SandBoxStats stats)
        {
            MaximumPopulation += stats.MaximumPopulation;
            StorageSpace += stats.StorageSpace;
            Defenses += stats.Defenses;
        }
        internal void Remove(SandBoxStats stats)
        {
            MaximumPopulation -= stats.MaximumPopulation;
            StorageSpace -= stats.StorageSpace;
            Defenses -= stats.Defenses;
        }
    }
}
