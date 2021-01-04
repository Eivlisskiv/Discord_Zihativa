using AMI.Neitsillia.NPCSystems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AMI.Neitsillia.Areas.AreaExtentions
{
    class Population
    {
        static AMIData.MongoDatabase Database => AMYPrototype.Program.data.database;

        public enum Type { Population, Bounties }

        public static Population Load(Type type, string id) =>
            Database.LoadRecord(type.ToString(), AMIData.MongoDatabase.FilterEqual<Population, string>("_id", id)) ?? new Population(type, id);

        public static async Task Delete(Type type, string id) =>
            await Database.DeleteRecord<Population>(type.ToString(), id);

        public string _id; //aka area id
        public Type type;
        public List<NPC> population;

        public int Count => population.Count;
        public NPC this[int i] => population[i];

        public Population(Type t, string id)
        {
            _id = id;
            type = t;

            population = new List<NPC>();
        }

        public void Add(NPC npc, bool save = true)
        {
            population.Add(npc);
            if(save) Save();
        }


        public NPC Splice(int index)
        {
            if (index < 0 || index >= Count) return null;

            NPC target = population[index];
            population.RemoveAt(index);
            Save();
            return target;
        }

        public NPC Random() =>
            Splice(AMYPrototype.Program.rng.Next(Count));

        public void Save() =>
            Database.UpdateRecord(type.ToString(), "_id", _id, this);

        public async Task Delete() =>
            await Delete(type, _id);
    }
}
