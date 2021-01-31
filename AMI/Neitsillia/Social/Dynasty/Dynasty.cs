using AMI.Module;
using AMI.Neitsillia.NPCSystems;
using AMI.Neitsillia.User;
using AMI.Neitsillia.User.PlayerPartials;
using AMYPrototype.Commands;
using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AMI.Neitsillia.NeitsilliaCommands.Social.Dynasty
{
    public partial class Dynasty
    {
        public const long DYNASTY_COST = 1_000_000;
        const string TABLE_NAME = "Dynasty";
        static AMIData.MongoDatabase Database => AMYPrototype.Program.data.database;

        public static async Task<Dynasty> CreateDynasty(Player founder, string name)
        {
            if (founder.dynasty != null) throw NeitsilliaError.ReplyError(
                "You are already part of a dynasty.");
            if(founder.KCoins < DYNASTY_COST) throw NeitsilliaError.ReplyError
                    ($"You are missing the required funds: {founder.KCoins}/{DYNASTY_COST}");

            Dynasty dan = new Dynasty(founder, name);
            await dan.Save();
            founder.SaveFileMongo();
            return dan;
        }

        public static async Task<bool> Exist(string name)
        {
            var result = Database.LoadRecord<Dynasty>(TABLE_NAME, $"{{ name: {{ $regex: \"^{name}$\", $options: '-i' }} }}");

            return result != null;
        }

        public static async Task<(Dynasty, DynastyMember, string)> Load(Player player)
        {
            if (player.dynasty == null) return (null, null, "You are not in a Dynasty");
            Dynasty dan = await Load(player.dynasty.id);
            if (dan == null) 
            {
                player.dynasty = null;
                player.SaveFileMongo();
                return (dan, null, "Your dynasty was disbanded"); 
            }
            DynastyMember membership = dan.GetMember(player);
            if (membership == null)
            {
                player.dynasty = null;
                player.SaveFileMongo();
                return (dan, membership, "You were removed from the Dynasty " + dan.name);
            }

            return (dan, membership, null);
        }

        public static async Task<Dynasty> Load(Guid id)
            => await Database.LoadRecordAsync<Dynasty, Guid>(TABLE_NAME, id);


        /*
         * Action/Permissions
         * > Members <
         * Invite
         * Kick
         * Promote
         * Demote
         * > Dynasty <
         * Change description
         * Change MOTD
         * > Strongholds <
         * Destroy Stronghold
         * Destroy Buildings
         * Build Buildings
         * UpgradeBuildings
         * Take from storage
         * Collect
         
        */

        public Guid _id;
        public string name;

        public ulong mainServerId;
        public string headquartersId;

        public string description;
        public string messageOfTheDay;

        public string[] rankNames;

        public List<DynastyMember> members;

        public int tier;

        private Dynasty(Player founder, string name)
        {
            _id = new Guid();
            mainServerId = 0;
            this.name = name;
            DynastyMember fMember = new DynastyMember(founder.userid, founder.name, 0);
            members = new List<DynastyMember>()
            { fMember };

            rankNames = new string[] 
            {
                "Head", "Duke", "Baron", "Lord",
                "High Knight", "Knight", "Adventurer", 
                "Squire", "Peasant"
            };

            founder.dynasty = new DynastyTicket(this, fMember);
            founder.KCoins -= DYNASTY_COST;
        }

        public EmbedBuilder ToEmbed(params EmbedField[] fields)
         => DUtils.BuildEmbed($"The {name} Dynasty",
                $"{description ?? "No description"}" +
                (messageOfTheDay != null ?
                $"```Message: {messageOfTheDay}```" : "")+
                $"{Environment.NewLine}Member Count: {members.Count}/{(1 + tier) * 4}",
                null, default, fields);

        public EmbedField MemberField(DynastyMember membership)
            => DUtils.NewField($"{rankNames[membership.rank]} of the {name} Dynasty",
                $"Member since {membership.joined.ToShortDateString()}"
                ).Build();

        public DynastyMember GetMember(string playerId)
            => members.Find(m => m.PlayerId == playerId);

        public DynastyMember GetMember(Player player)
        {
            DynastyMember member = GetMember(player._id);
            if(member != null) player.dynasty.Update(this, member.rank);
            return member;
        }

        //DataSetting
        internal async Task SetMainServer(ulong serverid)
        {
            mainServerId = serverid;
            await Save();
        }

        internal async Task SetRankName(int rank, string name)
        {
            rankNames[rank] = name;
            await Save();
        }

        public async Task<DynastyMember> AddMemeber(Player player)
        {
            DynastyMember member = new DynastyMember(player.userid, player.name, rankNames.Length - 1);
            player.dynasty = new DynastyTicket(this, member);
            members.Add(member);
            await Save();
            player.SaveFileMongo();
            return member;
        }

        public async Task RemoveMember(Player player)
        {
            members.Remove(GetMember(player._id));
            player.dynasty = null;
            await (members.Count > 0 ? Save() : Delete());
            player.SaveFileMongo();
        }

        //Database
        internal async Task Save()
            => await Database.UpdateRecordAsync(TABLE_NAME, 
                AMIData.MongoDatabase.FilterEqual<Dynasty, Guid>("_id", _id), this);

        internal async Task Delete()
        {
            await Database.DeleteRecord<Dynasty, Guid>(TABLE_NAME, _id);
        }
    }
}
