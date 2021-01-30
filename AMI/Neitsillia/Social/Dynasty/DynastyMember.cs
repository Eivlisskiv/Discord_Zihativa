using AMI.Methods;
using AMYPrototype.Commands;
using Discord;
using System;

namespace AMI.Neitsillia.NeitsilliaCommands.Social.Dynasty
{
    [MongoDB.Bson.Serialization.Attributes.BsonIgnoreExtraElements]
    public class DynastyMember
    {
        //for list of members
        public ulong userId;
        public string name;
        public int rank;

        public DateTime joined;

        public string PlayerId => $"{userId}\\{name}";

        public DynastyMember(ulong id, string name, int rank)
        {
            userId = id;
            this.name = name;
            this.rank = rank;
            joined = DateTime.UtcNow;
        }

        internal EmbedBuilder ToEmbed(Dynasty dan, DynastyMember manager = null)
        {
            return DUtils.BuildEmbed($"{name}, {dan.rankNames[rank]} of {dan.name}",
                $"", 
                null, default);
        }
    }
}
