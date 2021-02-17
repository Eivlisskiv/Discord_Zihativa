using AMI.Neitsillia;
using AMI.Neitsillia.Areas.AreaPartials;
using AMI.Neitsillia.User.PlayerPartials;
using Newtonsoft.Json;
using System;

namespace NeitsilliaEngine
{
    public class Junction
    {
        public string destination;
        public int floorRequirement;
        public string filePath;
        public int returnfloor;

        [JsonConstructor]
        public Junction(bool json)
        { }
        public Junction(Area area, int argFloor, int retFloor)
        {
            destination = area.name; floorRequirement = argFloor;
            returnfloor = retFloor;
            if (area.AreaId == null)
                area.AreaId = area.GeneratePath();
            filePath = area.AreaId;
        }
        public Junction(string argDes, int argFR, string path = null)
        {
            destination = argDes;
            floorRequirement = argFR;
            filePath = path;
        }
        public override string ToString()
        {
            if(floorRequirement > 0)
                return destination + " |F: " + floorRequirement;
            return destination;
        }
        internal Area PassJunction(Player player)
        {
            player.EndEncounter();
            Area area = Area.LoadArea(filePath, destination);
            if (area == null)
                throw AMI.Module.NeitsilliaError.ReplyError($"Error while loading Area: {filePath} | {destination}; " +
                    $"Please contact an administrator.");

            player.SetArea(area, returnfloor).Wait();
            return area;
        }
        internal static Predicate<Junction> FindName(string name)
        {
            return delegate (Junction x) { return x.destination == name; };
        }
    }
}
