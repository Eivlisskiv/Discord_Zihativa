using AMI.Neitsillia.Areas.AreaPartials;

namespace AMI.Neitsillia.Areas
{
    public class AreaPath
    {
        public enum Table {
            Area, Dungeons,
           // Nest
        }

        public int floor;
        public string name;
        public string path;
        public string data;

        public Table table;

        public bool TempAreaType => table == Table.Dungeons;

        public AreaPath(){}

        public AreaPath(Area area, int toFloor = 0)
        {
            name = area.name;
            path = area.AreaId;
            floor = toFloor;
        }
    }
}
