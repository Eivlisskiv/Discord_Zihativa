using AMI.AMIData;
using AMI.Methods;
using AMI.Methods.Graphs;
using AMI.Module;
using AMI.Neitsillia.Areas;
using AMI.Neitsillia.Collections;
using AMI.Neitsillia.Encounters;
using AMI.Neitsillia.Items;
using AMI.Neitsillia.Items.Abilities;
using AMI.Neitsillia.Items.Perks.PerkLoad;
using AMI.Neitsillia.Items.Quests;
using AMI.Neitsillia.NeitsilliaCommands;
using AMI.Neitsillia.NeitsilliaCommands.Social.Dynasty;
using AMI.Neitsillia.NPCSystems;
using AMI.Neitsillia.NPCSystems.Companions;
using AMI.Neitsillia.User.UserInterface;
using Discord;
using Discord.WebSocket;
using MongoDB.Bson.Serialization.Attributes;
using AMI.Neitsillia.Items.ItemPartials;
using Neitsillia.Methods;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AMI.Neitsillia.User.PlayerPartials
{
    [BsonIgnoreExtraElements]
    public partial class Player : CharacterMotherClass
    {
        public string _id;
        public string version;
        public ulong userid;

        public ReferenceData.HumanoidRaces race;

        public int skillPoints;
        public Specialization.Specialization Specialization;
        public List<Quest> quests;

        public Sheet userSheet;
        public USettings userSettings;
        public Timers userTimers;
        public DuelData duel;

        private BotUser user;

        public enum IgnoreException {
            All, None, SetUp, Adventuring, Resting,
            Transaction, MiniGames
        }

        #region Player Loading
        /// <summary>
        /// Loads a JSON player file and sync in the player's Area data
        /// </summary>
        /// <param name="userID">A string of the user's ID to find the save file</param>
        /// <param name="ignore"> ignore possible errors</param>
        /// <returns></returns>
        public static Player Load(ulong userID, IgnoreException ignore = IgnoreException.None)
        {
            BotUser u = BotUser.Load(userID);
            string characterId = $"{userID}\\{u.loaded}";
            return Load(u, ignore);
        }

        internal static Player Load(User.BotUser u, IgnoreException ignore = IgnoreException.None)
        {
            Player player = Load($"{u._id}\\{u.loaded}", ignore);
            player.user = u;
            return player;
        }

        public static Player Load(string charPath, IgnoreException ignore = IgnoreException.None)
        {
            Player player = AMYPrototype.Program.data.database.LoadRecord("Character", MongoDatabase.FilterEqual<Player, string>("_id", charPath));
            if (player == null) throw NeitsilliaError.CharacterDoesNotExist();
            //
            player.LoadCheck(ignore, false);
            //
            player.VerifyPlayer();
            return player;
        }

        bool LoadCheck(IgnoreException ignore, bool skip = false)
        {
            if(ignore != IgnoreException.All && ignore != IgnoreException.Transaction
                && ui?.type == MsgType.ConfirmTransaction)
            {
                Shopping.PendingTransaction.Cancel(this, ui.data);
            }

            if (ignore == IgnoreException.All)
                return false;
            else if ((ignore != IgnoreException.SetUp) &&
                   !IgnoreIfUIType(IgnoreException.SetUp, skip) && level <= -1)
                throw NeitsilliaError.CharacterIsNotSetUp(this);
            else if (ignore != IgnoreException.Adventuring &&
                   !IgnoreIfUIType(IgnoreException.Adventuring, skip) && IsInAdventure)
                throw NeitsilliaError.CharacterAdventuring(this);
            else if (ignore != IgnoreException.Resting &&
                   !IgnoreIfUIType(IgnoreException.Resting, skip) && IsResting)
                throw NeitsilliaError.CharacterIsResting(this);
            else if (ignore != IgnoreException.MiniGames) LockingMinigames();

            return false;
        }
        internal void LoadCheck(bool reaction, params IgnoreException[] arg)
        {
            List<IgnoreException> ignores = arg.ToList();

            if (!ignores.Contains(IgnoreException.All) && !ignores.Contains(IgnoreException.Transaction)
            && ui?.type == MsgType.ConfirmTransaction)
            {
                Shopping.PendingTransaction.Cancel(this, ui.data);
            }

            if ((!ignores.Contains(IgnoreException.SetUp)) &&
                   !IgnoreIfUIType(IgnoreException.SetUp, reaction) && level <= -1)
                throw NeitsilliaError.CharacterIsNotSetUp(this);
            else if (!ignores.Contains(IgnoreException.Adventuring) &&
                   !IgnoreIfUIType(IgnoreException.Adventuring, reaction) && IsInAdventure)
                throw NeitsilliaError.CharacterAdventuring(this);
            else if (!ignores.Contains(IgnoreException.Resting) &&
                   !IgnoreIfUIType(IgnoreException.Resting, reaction) && IsResting)
                throw NeitsilliaError.CharacterIsResting(this);
            else if (!ignores.Contains(IgnoreException.MiniGames)) LockingMinigames();
        }
        bool IgnoreIfUIType(IgnoreException ignore, bool skip = true)
        {
            if (ui == null || !skip)
                return false;
            else
            {
                switch (ignore)
                {
                    case IgnoreException.SetUp:
                        switch (ui.type)
                        {
                            case MsgType.ConfirmSkills:
                            case MsgType.ChooseRace:
                            case MsgType.SetSkill:
                            case MsgType.StarterAbilities:
                            case MsgType.AutoNewCharacter:
                                return true;
                        }
                        break;
                    case IgnoreException.Adventuring:
                        return (ui.type == MsgType.Adventure);
                    case IgnoreException.Resting:
                        switch(ui.type)
                        {
                            case MsgType.EndRest:
                            case MsgType.Inventory:
                                return true;
                        }break;
                }
            }
            return false;
        }
        bool LockingMinigames()
        {
            if (ui == null) return false;
            switch(ui.type)
            {
                case MsgType.CardGame:
                    if(GamblingHandKey._id != default && ui.data.Split(';').Length == 1)
                    {
                        throw NeitsilliaError.ReplyUI("Complete or quit your card game.", ui.type, this);
                    }
                    break;
            }
            return false;
        }

        internal async Task<bool> LoadCheck(bool reaction, IMessageChannel chan, 
            params IgnoreException[] ignore)
        {
            try { LoadCheck(reaction, ignore); }
            catch (Exception e)
            {
                await NeitsilliaError.SpecialExceptions(e, chan, this);
                return false;
            }
            return true;
        }
        private void VerifyPlayer()
        {
            version = ReferenceData.currentVersion;
            if(quests == null)
            {
                quests = new List<Quest>()
                {
                    Quest.Load(new int[] { 0,0,0}),
                    Quest.Load(new int[] { 0,4,0}),
                };
            }
        }

        public Player(BotUser bUser, string newChar = null)
        {
            userid = bUser._id;
            if (newChar == null)
                newChar = RandomName.ARandomName();
            CreateSave(newChar);
            bUser.loaded = newChar;
            bUser.Save();
        }
        [JsonConstructor]
        public Player()
        { }
        #endregion

        public override string ToString()
        {
            return $"{name} L:{level} R:{Rank()}";
        }
        
        public static bool FileExists(string id) => AMYPrototype.Program.data.database.IdExists<Player, string>("Character", id);

        internal bool IsSolo => Party == null || Party.SoloPlayer;

        //
        public void CreateSave(string playerName)
        {
            _id = $"{userid}\\{playerName}";
            version = ReferenceData.currentVersion;
            level = -1;
            name = playerName;
            health = 5;
            AreaInfo = new AreaPath()
            { name = "Moceoy's Basement", path = "Neitsillia\\Casdam Ilse\\Central Casdam\\Moceoy's Basement\\Moceoy's Basement" };

            Area = Areas.AreaPartials.Area.Load(AreaInfo);

            equipment.weapon = Item.LoadItem("Wooden Spear");

            userSheet = new Sheet();
            userSheet.ModifyProperty("name", name);
            userSettings = new USettings(userid);
            userTimers = new Timers(userid);

            respawnArea = "Neitsillia\\Casdam Ilse\\Central Casdam\\Moceoy's Basement\\Moceoy's Basement";

            quests = new List<Quest>()
            {
                Quest.Load(new int[] {0,0,0}),
                Quest.Load(new int[] {0,4,0}),
            };

            SaveFileMongo();
        }

        internal bool IsPartyLeader()
        {
            return Party == null || Party.GetLeaderID() == userid;
        }

        public EmbedBuilder UserEmbedColor(EmbedBuilder embed = null)
        {
            int[] RGB = userSettings.RGB;
            return embed != null ? embed.WithColor(RGB[0], RGB[1], RGB[2]) : new EmbedBuilder().WithColor(RGB[0], RGB[1], RGB[2]);
        }
        //
        public void SaveFileMongo(bool saveEncounter = true)
        {
            if (Encounter != null && saveEncounter)
            {
                if(EncounterKey?._id != Encounter._id)
                    EncounterKey = new DataBaseRelation<string, Encounter>(Encounter._id, Encounter);
                else EncounterKey?.Save();
            }

            ToolsKey?.Save();
            EggPocketKey?.Save();
            PetListKey?.Save();
            ProgressDataKey?.Save();
            FaithKey?.Save();
            AdventureKey?.Save();

            AMYPrototype.Program.data.database.UpdateRecord("Character",
                "_id", _id, this);
        }

        internal async Task DeleteFileMongo()
        {
            //Clear data
            EndEncounter();
            if (Party != null)
            {
                Party.RemoveAllPets(this);
                if (IsSolo)
                {
                    if(AreaInfo.TempAreaType)
                    {
                        await AMYPrototype.Program.data.database.DeleteRecord<Areas.AreaPartials.Area>("Dungeons",
                            AreaInfo.path, "AreaId");
                    }

                    for(int i = 0; i < Party.NPCMembers.Count; i++)
                        Party.Remove(i, Area);
                    await PartyKey.Delete();
                }
                else await Party.Remove(this);
            }
            if (ui?.type == MsgType.ConfirmTransaction)
                Shopping.PendingTransaction.Cancel(this, ui.data);

            ToolsKey?.Delete();
            EggPocketKey?.Delete();
            PetListKey?.Delete();
            ProgressDataKey?.Delete();
            FaithKey?.Delete();
            AdventureKey?.Delete();

            var dynastyData = await Dynasty.Load(this);
            if (dynasty != null)
            {
                await dynastyData.Item1.RemoveMember(this);
            }

            //Delete entries
            await AMYPrototype.Program.data.database.DeleteRecord<Player>("Character", _id, "_id");
        }

        public bool CollectItem(Item item, int amount, bool totry = false)
        {
            bool res = inventory.Add(item, amount, InventorySize());
            if (res && !totry)
                SaveFileMongo(false);
            return res;
        }
        public bool CollectItem(StackedItems st, bool totry = false)
        {
            return CollectItem(st.item,st.count,totry);
        }

        #region XP and Level
        public long XpGain(NPC mob)
        {
            long x = Convert.ToInt64(mob.XPDrop(level));
            if (x < 1) x = 1;
            return XpGain(x);
        }
        public long XpGain(double xpgain)
        {
            long xp = NumbersM.NParse<long>(xpgain);
            return XpGain(xp);
        }
        public override long XpGain(long xpGain, int mod)
        {
            xpGain /= (2 + mod);
            return XpGain(xpGain);
        }
        public long XpGain(long xpGain)
        {
            double multiplier = PerkLoad.CheckPerks(this, Perk.Trigger.GainXP, ReferenceData.xprate);
            long gain = Convert.ToInt64(xpGain * multiplier);
            experience += gain;
            long reqXPToLVL = XpRequired();
            while(experience >= reqXPToLVL)
            {
                level++;
                LevelNotifications();
                if (Specialization != null)
                    Specialization.specPoints++;
                experience -= reqXPToLVL;
                if (IsGainSkillPoint())
                    skillPoints++;
                reqXPToLVL = XpRequired();
            }
            SaveFileMongo();
            return gain;
        }
        void LevelNotifications()
        {
            if (level == 20)
            {
                _ = SendMessageToDM($"{name} has reached level 20 and unlocked " +
                    $"Specializations. To select a specialization, do ``~xp`` " +
                    $"and react with {EUI.pickSpec}. You may only select one " +
                    $"specialization and it may not be changed later.");
            }

            if(user == null) user = BotUser.Load(userid);
            if(user.referrer != 0)
                _ = Social.Mail.Mail.ReferenceReward(this, user.referrer);
        }
        #endregion

        public bool HasAbility(string abilityName, out int index)
        {
            abilityName = abilityName.ToLower();
            if (specter?.essence?.name.ToLower() == abilityName) index = abilities.Count;
            else index = abilities.FindIndex(item => item.name.ToLower() == abilityName);
            return index > -1;
        }
        internal Ability GetAbility(string abilityName, bool allowNullReturn = false)
        {
            if (abilityName == null) return null;
            if (abilityName.StartsWith("~")) return null;

            if (HasAbility(abilityName, out int index)) return GetAbility(index);

            if (allowNullReturn) return null;

            throw NeitsilliaError.ReplyError($"{name} does not have the ability: {abilityName}");
        }
        internal Ability GetAbility(int i) => i >= abilities.Count ? specter?.essence : abilities[i];

        internal int HasPerk(string perkName)
            => perks.FindIndex(item => item.name == perkName);

        

        internal bool IsLeader => IsSolo || Party.GetLeaderID() == userid;

        public bool IsResting => userTimers.restTime.Year != 1 ||
                (ui != null && (ui.type == MsgType.Rest || ui.type == MsgType.EndRest)
                && ui.data != null && ui.data != "");

        internal bool IsExhausted(int drain)
        {
            if (stamina >= drain)
            {
                stamina -= drain;
                return false;
            }
            stamina = 0;
            return true;
        }

        internal bool IsQuestAvailable(int[] id)
        {
            return !(ProgressData.QuestIsCompleted(id) || quests.Exists(q => 
            q.id[0] == id[0] &&
            q.id[1] == id[1] )); //quest line only, not objective
        }

        public string FullInfo(bool basicinfo = true, bool getStats = true,
        bool getInv = true, bool getEq = true, bool getschems = true, bool getAbility = true)
        {
            int l = 38;
            string strname = $"| {name} |";
            if (strname.Length < l)
            {
                int stars = (l - strname.Length) / 2;
                for (int i = 0; i < stars; i++)
                    strname = $"*{strname}*";
            }
            string info = $"{strname}{Environment.NewLine}";
            if (basicinfo)
            {
                info +=
                    $"| {userSheet.age} Year Old {userSheet.gender} {race}" + Environment.NewLine + Environment.NewLine
                    + "|Level: " + level + Environment.NewLine
                    + $"|Experience: {experience}/{Quadratic.XPCalc(level + 1)} {Environment.NewLine}"
                    + $"|Kutsyei Coins: {KCoins} {Environment.NewLine}"
                    //+ "|Profession: " + profession + Environment.NewLine
                    + "|Rank: " + Rank() + Environment.NewLine;
            }
            info += base.CharacterInfo(getStats, getInv, getEq, getschems, getAbility);
            return info;
        }
        internal async Task<string> Respawn(bool leaveParty, bool setArea = true)
        {
            EndEncounter();
            health = Health();
            stamina = Stamina();
            status.Clear();

            if(leaveParty && Party != null) await Party.Remove(this);

            if (respawnArea == null)
                respawnArea =  "Neitsillia\\Casdam Ilse\\Central Casdam\\Atsauka\\Atsauka";
            if(setArea)
                SetArea(Areas.AreaPartials.Area.LoadArea(respawnArea, "Atsauka")).Wait();
            return respawnArea;
        }

        internal async Task<IMessageChannel> DMChannel()
            => await AMYPrototype.Program.clientCopy.GetUser(userid).GetOrCreateDMChannelAsync();
        internal async Task<IUserMessage> SendMessageToDM(string message = null, EmbedBuilder embed = null,
            ISocketMessageChannel backup = null)
        {
            try
            {
                return await AMYPrototype.Program.clientCopy.GetUser(userid).SendMessageAsync(message, embed: embed?.Build());
            }
            catch(Exception e)
            {
                Log.LogS("Failed to DM " + userid);
                Log.LogS(e);
                if(backup != null)
                    return await backup.SendMessageAsync(message, embed: embed?.Build());
            }
            return null;
        }

        internal bool IsPremium(int v)
        {
            return User.BotUser.Load(userid).membershipLevel >= v || (userid == 201875246091993088);
        }
        internal bool IsRequiredLevel(int tier)
        {
            if(level < 5)
            {
                return tier <= 20;
            }
            return tier <= level * 5;
        }

        #region Objectives

        /// <summary>
        /// Party shared
        /// </summary>
        /// <param name="trigger"></param>
        /// <param name="argument"></param>
        public void QuestTrigger(Quest.QuestTrigger trigger, string argument = null, bool save = true)
        {
            if (Party != null)
            {
                switch(trigger)
                {
                    case Quest.QuestTrigger.EnterFloor:
                    case Quest.QuestTrigger.Enter:
                    //case Quest.QuestTrigger.ClearDungeon:
                    case Quest.QuestTrigger.RecruitNPC:

                        Party.QuestProgress(this, trigger, argument);
                        return;
                    default:
                        Quest_Trigger(trigger, argument, save); break;
                }

            }
            else Quest_Trigger(trigger, argument, save);
        }
        /// <summary>
        /// Not party shared
        /// </summary>
        /// <param name="trigger"></param>
        /// <param name="argument"></param>
        internal void Quest_Trigger(Quest.QuestTrigger trigger, string argument = null, bool save = true)
        {
            if (quests == null) quests = new List<Quest>();
            for (int i = 0; i < quests.Count;)
            {
                if (quests[i].trigger == trigger)
                    i = quests[i].Triggered(i, this, argument);
                else  i++;
            }
            if(save) SaveFileMongo();
        }

        public void EggPocketTrigger(Egg.EggChallenge challenge)
        {
            if (Party != null)
            {
                switch (challenge)
                {
                    case Egg.EggChallenge.Exploration:
                        Party.EggPocketTrigger(this, challenge);
                        return;
                }
            } else EggPocket_Trigger(challenge);
        }

        internal void EggPocket_Trigger(Egg.EggChallenge challenge)
        {
            if (EggPocket == null || EggPocket.egg == null || EggPocket.egg.challenge != challenge) return;

            if (EggPocket.HasFreePetSlot(PetList) && EggPocket.Progress(challenge))
                EggPocket.Hatch(this);
        }

        #endregion
    }
}
