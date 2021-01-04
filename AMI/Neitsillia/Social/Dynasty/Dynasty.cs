using AMI.Neitsillia.NPCSystems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AMI.Neitsillia.NeitsilliaCommands.Social.Dynasty
{
    partial class Dynasty
    {
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
         
        
        //*/

        static string[] DynastyMemberRanksDescription = new string[]
        {
            //Head
            "This role is only for the owner of the Dynasty and possess all permissions.",
            //Duke //Duchess
            "Dukes can manage Strongholds, ",
            //Baron //Baroness
            //High Knight
            //Knight
            //Squire
            //Peasant
        };

        public Guid _id;
        public string Name;

        public ulong mainServerId = 0;
        public string description;
        public string messageOfTheDay;

        public List<DynastyMember> Members;
        public List<NPC> Guards;

        public Dynasty(ulong ownerId, string name)
        {
            _id = new Guid();
            Name = name;
            Members = new List<DynastyMember>()
            {
                new DynastyMember(ownerId, DynastyMemberRanks.Head)
            };
        }

        //Main server
        internal void SetMainServer(ulong serverid)
        {
            mainServerId = serverid;
            Save().Wait();
        }

        //Database
        internal async Task Save()
        {
            await AMYPrototype.Program.data.database.SaveRecordAsync("Dynasty", this);
        }

        internal async Task Delete()
        {
            await AMYPrototype.Program.data.database.
                DeleteRecord<Dynasty, Guid>("Dynasty", _id);
        }
    }
}
