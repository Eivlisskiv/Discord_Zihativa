using AMYPrototype;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AMI.Neitsillia.NPCSystems.Companions.Pets
{
    class Orphanage
    {
        static AMIData.MongoDatabase Database => Program.data.database;

        internal static void AddPet(NPC pet)
        {
            Database.UpdateRecord("PetOrphanage", AMIData.MongoDatabase.FilterEqual<NPC, string>("origin", pet.origin), pet);
        }
    }
}
