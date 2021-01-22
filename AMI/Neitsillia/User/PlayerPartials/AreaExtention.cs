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
        internal AreaPath AreaInfo 
        {
            get => Party?.areaKey ?? areaPath;
            set
            {
                areaPath = value;
                if (Party != null)
                    _ = Party.SyncArea(value);
            }
        }

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
                        pathDestination.floor = AreaInfo.floor;

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
            if (Area.type == AreaType.Nest) pathDestination.floor = AreaInfo.floor;

            AreaInfo = pathDestination;
            Area = argArea;
        }

        public Area LoadAreaPath()
        {
            if (AreaInfo == null)
            {
                AreaInfo = new AreaPath()
                {
                    name = respawnArea?.Split('\\')[4] ?? "Atsauka",
                    path = respawnArea ?? "Neitsillia\\Casdam Ilse\\Central Casdam\\Atsauka\\Atsauka",
                    floor = 0
                };
            }

            Area = Area.Load(AreaInfo);

            //Area.FloorEffects(areaPath.floor);
            if (_area == null)
            {
                if (AreaInfo.data == "Nest")
                {
                    string[] data = AreaInfo.path.Split('\\');
                    AreaInfo = new AreaPath()
                    {
                        name = data[3],
                        path = $"{data[0]}\\{data[1]}\\{data[2]}\\{data[3]}\\{data[3]}",
                        floor = 0
                    };
                    Area = Area.Load(AreaInfo);
                    if (Area != null)
                    {
                        _ = SetArea(Area);
                        return Area;
                    }
                }

                AreaInfo = new AreaPath()
                {
                    name = respawnArea?.Split('\\')[4] ?? "Atsauka",
                    path = respawnArea ?? "Neitsillia\\Casdam Ilse\\Central Casdam\\Atsauka\\Atsauka",
                    floor = 0
                };

                Area = Area.Load(AreaInfo);
                if (_area == null)
                {
                    AreaInfo = new AreaPath()
                    {
                        name = "Atsauka",
                        path = "Neitsillia\\Casdam Ilse\\Central Casdam\\Atsauka\\Atsauka",
                        floor = 0
                    };
                }

                Log.LogS($"{Environment.NewLine}{Environment.NewLine} {userid}\\{name} is being sent back to {AreaInfo.name} " +
                    $"after failing to load Floor {AreaInfo.floor} of {AreaInfo.name} -> {AreaInfo.path} {Environment.NewLine}");
                SetArea(Area.Load(AreaInfo)).Wait();
                AMYPrototype.Program.clientCopy.GetUser(userid).SendMessageAsync(
                    $"Your character {name} was sent back to {AreaInfo.name} due to error while loading Area. " +
                    $"If this error persists or progression was lost, please contact an administrator.").Wait();
                SaveFileMongo();
            }

            return Area;
        }
        internal async Task AreaData(string s)
        {
            if (Party == null)
                AreaInfo.data = s;
            else
            {
                Party.areaKey.data = s;
                await Party.SaveData();
            }
        }
    }
}
