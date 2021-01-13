using AMI.Module;
using AMI.Neitsillia.NPCSystems.Companions.Pets;
using AMI.Neitsillia.User.PlayerPartials;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;

namespace AMI.Neitsillia.NPCSystems.Companions
{
    public class EggPocket
    {
        public string _id;

        //1 tier here is 10 creature level
        public int Tier;

        public Egg egg;

        public int hatchCount;

        public EggPocket(string id)
        {
            _id = id;
        }

        internal bool Progress(Egg.EggChallenge challenge)
        {
            if (egg == null) return false;

            if (egg.challenge == challenge)
                egg.hatchProgress++;
            return egg.hatchProgress >= egg.RequiredProgress;
        }

        public NPC Hatch(Player player)
        {
            if (egg == null) throw NeitsilliaError.ReplyError("Pocket empty");
            NPC baby = egg.Hatch();
            egg = null;
            hatchCount++;

            baby.origin = $"{player.userid}\\{player.name}";
            player.PetList.Pets.Add(new Pet(baby, Pet.PetStatus.Idle));
            player.PetList.Save();

            player.EggPocketKey.Save();

            _ = player.SendMessageToDM($"Your egg has hatched! View your pets using the `Pets` command.");

            return baby;
        }

        public async Task EquippEgg(Egg aegg, Player player, ISocketMessageChannel chan = null)
        {
            if (egg != null)
                throw NeitsilliaError.ReplyError("You already have an egg in your Egg Pocket.");
            if(Tier < aegg.Tier)
                throw NeitsilliaError.ReplyError("Your Egg Pocket is too low tier to care for this egg.");
            egg = aegg;

            player.Quest_Trigger(Items.Quests.Quest.QuestTrigger.FillEggPocket, $"{egg.Tier}");

            if(chan != null)
                await CompanionCommands.PocketUi(player, chan);
        }

        public string GetInfo(string id)
        {
            return egg == null ? "Pocket is Empty" : egg.GetInfo() + 
                (HasFreePetSlot(id) ? null : Environment.NewLine + "This egg may not hatch until you free up a Pet Slot.");
        }

        internal bool HasFreePetSlot(string id)
        {
            return PetList.Load(id).Pets.Count < 5;
        }
        internal bool HasFreePetSlot(PetList petList)
        {
            return petList.Pets.Count < 5;
        }

    }
}
