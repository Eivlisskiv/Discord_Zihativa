using Discord;
using Newtonsoft.Json;

namespace AMI.AMIData.Servers
{
    class Channel
    {
        public ulong id;
        public string name;

        public Channel(IChannel channel)
        {
            id = channel.Id;
            name = channel.Name;
        }
        public override string ToString()
        {
            return $"<#{id}>";
        }
        [JsonConstructor]
        public Channel(bool JSON)
        { }
    }
}
