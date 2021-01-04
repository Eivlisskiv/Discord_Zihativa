using AMI.Neitsillia.Areas.AreaExtentions;

namespace AMI.Neitsillia.Areas.AreaPartials
{
    partial class Area
    {
        private Population _bounties;
        private Population _population;

        public Population GetPopulation(Population.Type t)
        {
            string id = (type == AreaType.Dungeon || type == AreaType.Arena || type == AreaType.Nest) ?
                GeneratePath(false) + parent
                : AreaId;
            
            switch (t)
            {
                case Population.Type.Bounties: return _bounties ?? (_bounties = Population.Load(t, id) ?? new Population(t, id));
                case Population.Type.Population: return _population ?? (_population = Population.Load(t, id) ?? new Population(t, id));
                default: return null;
            }
        }
    }

}
