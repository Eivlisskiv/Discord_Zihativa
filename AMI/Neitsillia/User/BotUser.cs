using AMI.AMIData;
using AMI.Methods;
using AMI.Neitsillia.User.PlayerPartials;
using AMI.Neitsillia.User.UserInterface;
using AMYPrototype;
using Discord;
using Discord.WebSocket;
using MongoDB.Bson.Serialization.Attributes;
using Neitsillia.Items.Item;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AMI.Neitsillia.User
{
    [BsonIgnoreExtraElements]
    class BotUser
    {
        //private static Cache<ulong, BotUser> cache = new Cache<ulong, BotUser>();

        [BsonId]
        public ulong _id;

        public DateTime dateInscription;

        public string loaded;
        public UI ui;

        public int membershipLevel = 0;

        public DateTime LastVote;
        public int voteStreak;
        public int[] ResourceCrates;

        [JsonConstructor]
        public BotUser(bool json = false)
        { }
        public BotUser(ulong id)
        {
            _id = id;
            ResourceCrates = new int[5];
        }
        public void Save()
        {
            Program.data.database.UpdateRecord("User",
                    MongoDatabase.FilterEqual<BotUser, ulong>("_id", _id), this);

            //cache.Save(_id, this);
        }

        #region Characters
        public string ChangeCharacter(string charname)
        {
            if (charname == null) return null;

            if((charname = GetCharFiles(_id).Find(x => x.name.ToLower().Equals(charname.ToLower()))?.name) != null)
            {
                loaded = charname;
                Save();
                return $"Character {charname} Loaded";
            }
            else if (loaded != null)
                    return $"Character {charname} was not found. Currently loaded character: {loaded}";
            return $"Character {charname} not found. No character currently loaded. Make a new character using ``~New Character charnamehere``" +
                $" or view existing characters using ``~List Characters``";
        }

        public static List<Player> GetCharFiles(ulong id)
        {
            List<string> names = new List<string>();
            List<Player> characters = Program.data.database.LoadRecords<Player>(
                "Character", MongoDatabase.FilterEqual<Player, ulong>("userid", id));
            return characters;
        }
        #endregion

        internal static BotUser Load(ulong id)
            => Program.data.database.LoadRecord("User", MongoDatabase.FilterEqual<BotUser, ulong>("_id", id))
                    ?? new BotUser(id);

        internal void NewUI(IUserMessage userMessage, MsgType type,
            string adata = null)
        {
            ui = new UI(userMessage, type, null, argData: adata);
            Save();
        }

        internal async Task EditUI(string msg, Embed embed, MsgType type,
            string adata = null, IMessageChannel chan = null)
        {
            if (ui == null) NewUI(await chan.SendMessageAsync(msg, embed: embed), type, adata);
            else await ui.Edit(null, msg, embed, type, adata, true);
            Save();
        }

        #region Resource Crates
        public async Task NewVote()
        {
            if (ResourceCrates == null)
                ResourceCrates = new int[5];
            while (ResourceCrates.Length < 5)
                ResourceCrates = ArrayM.AddItem(ResourceCrates, 0);

            string rewards = null;
            int amount = Program.dblAPI.CrateAmount() * ReferenceData.CrateRate;

            rewards += ProcessVotes(amount);

            Save();

            await SendMessageToDM($"Your vote was registered." +
                $" You've received: {Environment.NewLine + rewards} with a current streak of **{voteStreak}**." 
                + Environment.NewLine + "Use command `Crates` to open resource crates."
            );
        }
        public string ProcessVotes(int amount)
        {
            string s = null;
            if (LastVote.AddDays(1.2) < DateTime.UtcNow)
                voteStreak = 0;
            for (int i = 0; i < amount; i++)
            {
                s += $"**1x {GrantResourceCrate()} Resource Crate** {Environment.NewLine}";
                voteStreak++;
            }
            LastVote = DateTime.UtcNow.AddHours(-1);
            return s;
        }
        public ReferenceData.ResourceCrate GrantResourceCrate()
        {
            ReferenceData.ResourceCrate type = GetResourceCrate();
            ResourceCrates[(int)type]++;
            return type;
        }
        public ReferenceData.ResourceCrate GetResourceCrate()
        {
            if(voteStreak < 5)
                return ReferenceData.ResourceCrate.Wooden;
            if (Utils.Divisible(voteStreak, 60))
                return ReferenceData.ResourceCrate.Platinum;
            else if (Utils.Divisible(voteStreak, 25))
                return ReferenceData.ResourceCrate.Golden;
            else if (Utils.Divisible(voteStreak, 12))
                return ReferenceData.ResourceCrate.Silver;
            else if (Utils.Divisible(voteStreak, 5))
                return ReferenceData.ResourceCrate.Bronze;

            return ReferenceData.ResourceCrate.Wooden;
        }

        public async Task CratesListUI(ISocketMessageChannel chan)
        {
            if (ResourceCrates == null)
                ResourceCrates = new int[5];
            while (ResourceCrates.Length < 5)
                ResourceCrates = ArrayM.AddItem(ResourceCrates, 0);

            EmbedBuilder em = AMYPrototype.Commands.DUtils.BuildEmbed("Resource Crates",
                "Select a Crate type to open." + Environment.NewLine +
                $"{EUI.GetNum(0)} {ResourceCrates[0]} x Wooden Crates" + Environment.NewLine +
                $"{EUI.GetNum(1)} {ResourceCrates[1]} x Bronze Crates" + Environment.NewLine +
                $"{EUI.GetNum(2)} {ResourceCrates[2]} x Silver Crates" + Environment.NewLine +
                $"{EUI.GetNum(3)} {ResourceCrates[3]} x Golden Crates" + Environment.NewLine +
                $"{EUI.GetNum(4)} {ResourceCrates[4]} x Platinum Crates");

            NewUI(await chan.SendMessageAsync(embed: em.Build()), MsgType.ResourceCrateList);
        }

        public async Task CharListForCrate(ISocketMessageChannel chan, int crateNum)
        {
            string desc = null;
            var chars = GetCharFiles(_id);
            if (chars.Count < 1)
                await chan.SendMessageAsync("No characters were found for this user, create a character using `new character`");
            else
            {
                for (int i = 0; i < chars.Count; i++)
                    desc += EUI.GetNum(i) + " " + chars[i].ToString() + Environment.NewLine;

                EmbedBuilder em = AMYPrototype.Commands.DUtils.BuildEmbed($"Opening {(ReferenceData.ResourceCrate)crateNum} Crates",
                    "Select a character to reward crate content to." + Environment.NewLine + desc);

                await EditUI(null, em.Build(), MsgType.ResourceCrateOpening, crateNum + ";" + chars.Count, chan);
            }
        }

        public string OpenCrate(int crateType, int charIndex, out string givenTo)
        {
            var chars = GetCharFiles(_id);
            Player player = chars[charIndex];
            long koins = player.level + voteStreak;
            int gearCount = 1;
            int rareChance = 0;
            int tier = player.level * 3;
            int scaleMult = 2;
            givenTo = null;

            switch (crateType)
            {
                case 0: break;
                case 1:
                    koins *= 2;
                    rareChance = 5;
                    break;
                case 2:
                    koins *= 5;
                    rareChance = 10;
                    tier = player.level * 4;
                    break;
                case 3:
                    koins *= 10;
                    rareChance = 20;
                    gearCount = 2;
                    tier = player.level * 5;
                    scaleMult = 4;
                    break;
                case 4:
                    koins *= 20;
                    rareChance = 35;
                    gearCount = 3;
                    tier = player.level * 5;
                    scaleMult = 5;
                    break;
            }

            if (player.inventory.Count + gearCount - 1 >= player.InventorySize())
                return "Player inventory may not contain crate rewards.";
            else if (player.level <= 0)
                return "Character must be level 1 or higher to receive crate rewards.";

            Random rng = Program.rng;

            string content = $"+{koins} Kutsyei Coins" + Environment.NewLine;
            player.KCoins += koins;
            for(int i = 0; i < gearCount; i++)
            {
                int type = Program.Chance(rareChance) ? 5 :
                        rng.Next(6) + 6;
                Item item = Item.RandomItem(player.level * scaleMult, type);
                content += item.ToString() + Environment.NewLine;
                if (!player.CollectItem(item, 1))
                    return $"Player inventory may not contain crate rewards.";
            }

            ResourceCrates[crateType]--;
            Save();

            player.SaveFileMongo();
            givenTo = player.name;
            return content;
        }
        #endregion

        internal async Task SendMessageToDM(string message = null, EmbedBuilder embed = null,
            ISocketMessageChannel backup = null)
        {
            try
            {
                await AMYPrototype.Program.clientCopy.GetUser(_id).SendMessageAsync(message, embed: embed?.Build());
            }
            catch (Exception e)
            {
                Log.LogS("Failed to DM " + _id);
                Log.LogS(e);
                if(backup != null)
                await backup.SendMessageAsync(message, embed: embed?.Build());
            }
        }
    }
}
