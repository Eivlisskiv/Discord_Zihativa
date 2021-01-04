using AMI.Neitsillia.Areas.AreaPartials;
using System.Collections.Generic;

namespace AMI.Neitsillia.Areas
{
    static class ChildrenArea
    {
        static void SetJunctions(Area child, Area parent, bool save)
        {
            parent.junctions.Add(new NeitsilliaEngine.Junction(child, 0, 0));
            child.junctions.Add(new NeitsilliaEngine.Junction(parent, 0, 0));
            if(save)
            {
                child.UploadToDatabase(true).Wait();
                parent.UploadToDatabase(true).Wait();
            }
        }

        public static Area Tavern(Area parent, string tavernName, bool saveToDatabse = false)
        {
            Area child = new Area(AreaType.Tavern, tavernName, parent)
            {
                description = "A tavern built in " + parent.name,
                passiveEncounter = new string[]
                {
                     "Npc"
                },
                ePassiveRate = 100,
            };

            SetJunctions(child, parent, saveToDatabse);

            return child;
        }

        public static Area PetShop(Area parent, string name, bool saveToDatabse = false)
        {
            Area child = new Area(AreaType.BeastMasterShop, name, parent)
            {
                description = "These stores specialize in taming creatures and hatching eggs to grow companions. Use the `Services` command.",
            };

            SetJunctions(child, parent, saveToDatabse);

            return child;
        }

        internal static Area Arena(Area parent, string childName, string arguments, bool save)
        {
            Area child = new Area(AreaType.ArenaLobby, childName, parent)
            {
                description = "An arena built in " + parent.name,
            };

            SetJunctions(child, parent, save);

            return child;
        }

        internal static Area Shrine(Area parent, string childName, bool save)
        {
            Area child = new Area(AreaType.Shrine, childName, parent)
            {
                description = "An old shrine in " + parent.name,
            };

            SetJunctions(child, parent, save);

            return child;
        }
    }
}
