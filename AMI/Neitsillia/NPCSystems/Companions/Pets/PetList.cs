using AMI.AMIData;
using AMI.Neitsillia.User.PlayerPartials;
using AMI.Neitsillia.User.UserInterface;
using AMYPrototype;
using AMYPrototype.Commands;
using Discord;
using Discord.WebSocket;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AMI.Neitsillia.NPCSystems.Companions.Pets
{
    public class PetList
    {
        static MongoDatabase Database => Program.data.database;

        public string _id; //character id

        public List<Pet> Pets;

        public Pet this[int i]
        {
            get => Pets[i];
            set
            {
                Pets[i] = value;
                Save();
            }
        }
        public int Count => Pets.Count;

        public PetList(string id)
        {
            _id = id;
            Pets = new List<Pet>();
        }

        public static PetList Load(string id)
        {
            return Database.LoadRecord("PetList",
                MongoDatabase.FilterEqual<PetList, string>("_id", id))
                ?? new PetList(id);
        }

        public void Save()
        {
            Database.UpdateRecord("PetList", MongoDatabase.FilterEqual<PetList, string>("_id", _id), this);
        }

        internal async Task BuildUI(Player player, ISocketMessageChannel channel)
        {
            EmbedBuilder e = DUtils.BuildEmbed($"{player.name}'s Pets", Pets.Count > 0 ? null : $"{player.name} has no pets", null, player.userSettings.Color());
            if(Pets.Count > 0)
            {
                for (int i = 0; i < Pets.Count; i++)
                {
                    var (title, desc) = Pets[i].GetField(i);
                    e.AddField(DUtils.NewField(title, desc, true));
                }
            }
            await player.NewUI(null, e.Build(), channel, MsgType.PetList);
        }

        internal async Task PetControls(int i, Player player, ISocketMessageChannel channel)
        {
            var petslot = player.PetList[i];

            EmbedBuilder embed = petslot.pet.NPCInfo(player.UserEmbedColor(), getInv: false);
            embed.WithTitle(petslot.pet.displayName + $"[{petslot.status}]");

            await player.EditUI(null, embed.Build(), channel, MsgType.InspectPet,
                $"{i};{petslot.status == Pet.PetStatus.Idle}");
        }

        internal bool UpdatePet(NPC pet)
        {
            int index = Pets.FindIndex(p => p.id == pet.origin);
            if(index < 0)
            {
                _ = Handlers.UniqueChannels.Instance.SendToLog($"{pet.displayName}'s place was not found and was sent to the orphanage. origin: {pet.origin}");
                Orphanage.AddPet(pet);
                return false;
            }

            Pets[index].UpdatePet(pet);

            Save();
            return true;
        }
    }
}
