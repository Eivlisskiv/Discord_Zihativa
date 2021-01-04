using AMI.Methods;
using AMI.Neitsillia.Areas;
using AMI.Neitsillia.Areas.AreaPartials;
using Discord;
using System;
using System.Threading.Tasks;

namespace AMI.Neitsillia.User.PlayerPartials
{
    partial class Player
    {
        public string respawnArea;

        public AreaPath areaPath;
        internal AreaPath AreaInfo => Party?.areaKey ?? areaPath;

        private Area _area;
        internal Area Area
        {
            get => _area ?? LoadAreaPath();
            private set => _area = value;
        }

        public async Task SetArea(Area argArea, int toFloor = 0)
        {
            var pathDestination = new AreaPath(argArea, toFloor);

            switch (argArea.type)
            {
                case AreaType.Dungeon:
                case AreaType.Arena:
                    {
                        pathDestination.path = $"{userid}\\{name}\\{argArea.type}";
                        argArea.AreaId = $"{userid}\\{name}\\{argArea.type}";
                        pathDestination.table = AreaPath.Table.Dungeons;
                        if (argArea.junctions != null)
                            argArea.junctions.Clear();
                        await argArea.UploadToDatabase();

                        pathDestination.table = AreaPath.Table.Dungeons;
                    }
                    break;
                case AreaType.Nest:
                    {
                        pathDestination.data = "Nest";
                        pathDestination.floor = areaPath.floor;

                        //pathDestination.table = AreaPath.Table.Nest;
                    }
                    break;
                case AreaType.Town:
                case AreaType.Stronghold:
                    respawnArea = pathDestination.path;
                    pathDestination.table = AreaPath.Table.Area;
                    break;

                default:
                    pathDestination.table = AreaPath.Table.Area; break;
            }
            //Current Area is a nest, when leaving the nest, return to old floor
            if (Area.type == AreaType.Nest) pathDestination.floor = areaPath.floor;

            this.areaPath = pathDestination;
            this.Area = argArea;
            Party?.SyncArea(areaPath);
        }

        public Area LoadAreaPath()
        {
            if (areaPath == null)
            {
                areaPath = new AreaPath()
                {
                    name = respawnArea?.Split('\\')[4] ?? "Atsauka",
                    path = respawnArea ?? "Neitsillia\\Casdam Ilse\\Central Casdam\\Atsauka\\Atsauka",
                    floor = 0
                };
            }

            Area = Area.Load(areaPath);

            //Area.FloorEffects(areaPath.floor);
            if (Area == null)
            {
                if (areaPath.data == "Nest")
                {
                    string[] data = areaPath.path.Split('\\');
                    areaPath = new AreaPath()
                    {
                        name = data[3],
                        path = $"{data[0]}\\{data[1]}\\{data[2]}\\{data[3]}\\{data[3]}",
                        floor = 0
                    };
                    Area = Area.Load(areaPath);
                    if (Area != null)
                    {
                        _ = SetArea(Area);
                        return Area;
                    }
                }

                areaPath = new AreaPath()
                {
                    name = respawnArea?.Split('\\')[4] ?? "Atsauka",
                    path = respawnArea ?? "Neitsillia\\Casdam Ilse\\Central Casdam\\Atsauka\\Atsauka",
                    floor = 0
                };

                Area = Area.Load(areaPath);
                if (Area == null)
                {
                    areaPath = new AreaPath()
                    {
                        name = "Atsauka",
                        path = "Neitsillia\\Casdam Ilse\\Central Casdam\\Atsauka\\Atsauka",
                        floor = 0
                    };
                }

                Log.LogS($"{Environment.NewLine}{Environment.NewLine} {userid}\\{name} is being sent back to {areaPath.name} " +
                    $"after failing to load Floor {areaPath.floor} of {areaPath.name} -> {areaPath.path} {Environment.NewLine}");
                SetArea(Area.Load(areaPath)).Wait();
                AMYPrototype.Program.clientCopy.GetUser(userid).SendMessageAsync(
                    $"Your character {name} was sent back to {areaPath.name} due to error while loading Area. " +
                    $"If this error persists or progression was lost, please contact an administrator.").Wait();
                SaveFileMongo();
            }

            return Area;
        }
        internal async Task AreaData(string s)
        {
            if (Party == null)
                areaPath.data = s;
            else
            {
                Party.areaKey.data = s;
                await Party.SaveData();
            }
        }
    }
}
