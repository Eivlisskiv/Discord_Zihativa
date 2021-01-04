using AMI.Neitsillia.User.PlayerPartials;
using Newtonsoft.Json;

namespace AMI.Neitsillia.NeitsilliaCommands
{
    class PartyMember
    {
        public ulong id; 
        public string characterName;

        internal Player player;

        [JsonConstructor]
        public PartyMember(bool json) { }
        public PartyMember(ulong argId, string argName)
        {
            id = argId; characterName = argName;
        }

        public string Path => $"{id}\\{characterName}";

        internal Player LoadPlayer()
        {
            return player ?? (player = Player.Load(Path, Player.IgnoreException.All));
        }
    }
}
