using AMI.Methods;
using AMI.Methods.Graphs;
using AMI.Neitsillia.Areas;
using AMI.Neitsillia.Areas.AreaExtentions;
using AMI.Neitsillia.Collections;
using AMI.Neitsillia.Combat;
using AMI.Neitsillia.Crafting;
using AMI.Neitsillia.Items;
using AMI.Neitsillia.Items.Perks.PerkLoad;
using AMYPrototype;
using AMYPrototype.Commands;
using Discord;
using MongoDB.Bson.Serialization.Attributes;
using Neitsillia.Items.Item;
using Neitsillia.Methods;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using static AMI.Neitsillia.ReferenceData;

namespace AMI.Neitsillia.NPCSystems
{
    [BsonIgnoreExtraElements]
    partial class NPC : CharacterMotherClass
    {
        static AMIData.MongoDatabase Database => Program.data.database;

        #region Public Vars
        public Profession profession;
        public Reputation.Faction.Factions faction;
        public CombatRole role = CombatRole.None;
        public string displayName;
        public string origin;
        public string desc;
        public int baseLevel;
        public string race;
        //
        public bool hasweapon;
        public bool hashelmet;
        public bool hasmask;
        public bool haschestp;
        public bool hasjewelry;
        public bool hastrousers;
        public bool hasboots;
        //
        //
        public List<string>[] drops;
        public double xpDropBuff = 1;
        #endregion

        public override bool Equals(object obj)
        {
            if(obj is NPC npc)
            {
                return displayName == npc.displayName
                    && name == npc.name;
            }
            return false;
        }
        public override string ToString()
        {
            return $"{displayName} L:{level} R:{Rank()}";
        }

        public static Predicate<NPC> FindWithName(string argName)
        {
            return delegate (NPC item) { return item.name == argName; };
        }
        public static Predicate<NPC> FindWithDisName(string argName)
        {
            return delegate (NPC item) { return item.displayName == argName; };
        }

        #region Loading NPC

        [JsonConstructor]
        public NPC(bool JSON)
        { }
        public static NPC TrainningDummy(int level, int i = 1)
        {

            var dummy = new NPC(true)
            {
                profession = ReferenceData.Profession.Creature,
                displayName = "Training Dummy " + NumbersM.GetLevelMark(i),
                name = "Training Dummy",
                xpDropBuff = 0,

                desc = "Dummy used for training",
                baseLevel = 0,
                level = 0,

                stats = new Stats()
                {
                    endurance = 100,
                    intelligence = 0,
                    strength = 0,
                    charisma = 0,
                    dexterity = 0,
                    perception = 0,

                    maxhealth = 100 * level,
                    stamina = 10 * level,
                },

                abilities = new List<Ability>()
                {
                    LoadAbility.Taunt("Taunt"),
                    LoadAbility.Brawl("Heal")
                }
            };

            dummy.health = dummy.Health();
            dummy.stamina = dummy.Stamina();

            return dummy;
        }
        public static NPC GenerateNPC(int level, string name)
        {
            NPC mob = Database.LoadRecord("Creature", AMIData.MongoDatabase.FilterEqual<NPC, string>(
                "displayName", name));
            if (mob == null)
            {
                string path = FindItemPath(name);
                if (path != null)
                    mob = FileReading.LoadJSON<NPC>(path);
            }
            if(mob != null)
                return GenerateNPC(level, mob);
            return null;
        }
        public static NPC GenerateNPC(int level, NPC mob)
        {
            mob.level = Verify.Min(level, mob.baseLevel);
            if (mob.level > mob.baseLevel)
                mob.ScaleNPC();
            mob.NPCSetUp();
            return mob;
        }
        internal void ScaleNPC(double mult = 1)
        {
            double buffPercent = 
                //level/(double)baseLevel* 2
                Exponential.CreatureScale(baseLevel, level)
                * mult ;

            //Stats
            stats.maxhealth = NumbersM.CeilParse<long>(stats.maxhealth *  (1 + buffPercent * 5));
            stats.stamina = NumbersM.CeilParse<int>(stats.stamina *  (1 + buffPercent * 2));
            health = Health();
            stamina = Stamina();
            for (int i = 0; i < DmgType.Length; i++)
            {
                if(stats.damage[i] > 0)
                    stats.damage[i] = NumbersM.CeilParse<long>(stats.damage[i] * (1 + buffPercent/3));
                if (stats.resistance[i] > 0)
                    stats.resistance[i] = NumbersM.CeilParse<int>(stats.resistance[i] * (1 + (buffPercent/5)));
            }

            //Extra Drops
            if(Program.rng.Next(101) <= 1)
            {

                Item tempSchem = SkaviDrops.DropSchematic(race);
                if (tempSchem != null)
                    AddItemToInv(tempSchem);
            }
        }
        public static NPC NewNPC(int level, string profession,  string race)
        {
            string[] availableRaces = { "Human"};
            if (profession == null)
                profession = "Child";
            if (race == null)
                race = availableRaces[Program.rng.Next(availableRaces.Length)];
            NPC npc = GenerateNPC(Verify.Min(level, 0), $"{race} {profession}");
            if (npc != null)
            {
                npc.faction = Reputation.Faction.Factions.Civil;

                switch(npc.displayName.Split(' ')[0].ToLower())
                {
                    case "stranger":
                    case "human":
                        npc.name = RandomName.ARandomName() + " " + RandomName.ARandomName();
                        break;
                }
                return npc;
            }
            return null;
        }
        void NPCSetUp()
        {
            GearNPC();
            int p = 0;
            for (int k = baseLevel; k < level;)
            {
                k += p += 5;
                AddStats();
            }
            //for (int i = 0; i < abilities.Count; i++)
            //    abilities[i] = Ability.Load(abilities[i].name);
            for (int i = 0; i < perks.Count; i++)
            { 
                if(perks[i].name == "-Random")
                    perks[i] = PerkLoad.RandomPerk(perks[i].tier, "Character");
                else
                    perks[i] = PerkLoad.Load(perks[i].name);
            }
            switch (race)
            {
                case "Human":
                    perks.Add(PerkLoad.Load("Human Adaptation"));
                        break;
                case "Tsiun":
                    perks.Add(PerkLoad.Load("Tsiun Trickery"));
                        break;
                case "Uskavian":
                    perks.Add(PerkLoad.Load("Uskavian Learning"));
                        break;
                case "Miganan":
                    perks.Add(PerkLoad.Load("Migana Skin"));
                        break;
                case "Ireskian":
                    perks.Add(PerkLoad.Load("Ireskian Talent"));
                        break;
            }
            health = Health();
            stamina = Stamina();
            if (level >= 20)
                GetCombatRole();
        }
        #endregion

        public static string FindItemPath(string aname)
        {
            if (File.Exists(aname))
                return aname;
            string itempath = ReferenceData.oldmobPath;
            string pathFound = null;
            DirectoryInfo[] typesD = new DirectoryInfo(itempath).GetDirectories();
            bool found = false;
            for (int t = 0; t < typesD.Length && !found; t++)
            {
                DirectoryInfo[] raceD = new DirectoryInfo(itempath + @"\" + typesD[t]).GetDirectories();
                for (int i = 0; i < raceD.Length && !found; i++)
                {
                    FileInfo[] itemD = new DirectoryInfo(raceD[i].FullName).GetFiles();
                    for (int k = 0; k < itemD.Length && !found; k++)
                        if (itemD[k].Name == aname)
                        {
                            pathFound = itemD[k].FullName;
                            found = true;
                        }
                }
            }
            return pathFound;
        }
        public string[] MobDrops(int lootCount)
        {
            string[] looted = new string[lootCount];
            if (drops != null)
            {
                for (int i = 0; i < lootCount; i++)
                {
                    int t = ArrayM.IndexWithRates(drops.Length, Program.rng);
                    looted[i] = drops[t][ArrayM.IndexWithRates(drops[t].Count, Program.rng)];
                }
                return looted;
            }
            return null;
        }
        void AddStats(int stat = 0)
        {
            if (stat == 0)
                stat = Program.rng.Next(1, 7);
            switch(stat)
            {
                case 1: stats.endurance++; break;
                case 2: stats.intelligence++; break;
                case 3: stats.strength++; break;
                case 4: stats.charisma++; break;
                case 5: stats.dexterity++; break;
                case 6: stats.perception++; break;
            }
        }

        #region Inventory

        internal bool HasGearSlot(Item.IType t)
        {
            switch (t)
            {
                case Item.IType.Weapon: return hasweapon;
                case Item.IType.Helmet: return hashelmet;
                case Item.IType.Mask: return hasmask;
                case Item.IType.Chestp: return haschestp;
                case Item.IType.Jewelry: return true;
                case Item.IType.Trousers: return hastrousers;
                case Item.IType.Boots: return hasboots;
                default: return false;
            }
        }

        internal bool HasGearSlot(int t)
        {
            switch (t)
            {
                case 0: return hasweapon;
                case 1: return false;
                case 2: return hashelmet;
                case 3: return hasmask;
                case 4: return haschestp;
                case 5:
                case 6:
                case 7: return true;
                case 8: return hastrousers;
                case 9: return hasboots;
                default: return false;
            }
        }

        internal Item SpawnItem(string name)
        {
            Item i = Item.LoadItem(name);
            if (!i.CanBeEquip())
                return i;

            try { i.Scale(level * 5); }
            catch (Exception e) {  _ = Handlers.UniqueChannels.Instance.SendToLog($"Failed to Scale mob drop: {i.originalName} => T {i.tier} > {level * 5} {Environment.NewLine} {e.Message}");  }

            return i;
        }

        public void GearNPC()
        {
            if (level < 1)
                level = 1;
            //Get koins
            int average = Convert.ToInt32(Exponential.KoinsGeneratedWithMob(level));
            int maxK = Convert.ToInt32(average * 5);
            int minK = Convert.ToInt32(average * 2);
            KCoins = Program.rng.Next(minK, maxK);
            //Inventory
            int itemCount = Program.rng.Next(3, 8);
            string[] icollection = MobDrops(itemCount);
            //add inventory
            if (icollection != null)
            foreach (string i in icollection)
            {
                    if(i != null)
                AddItemToInv(SpawnItem(i), 1, false);
            }
        }
        public void AddItemToInv(Item item, int amount = 1, bool ignoreInterest = true)
        {
            if (item == null) return;

            int result = IsInterested(item, true);
            if (inventory == null)  inventory = new Inventory();

            if (
                (result == 2 || (ignoreInterest && result != 1))
                                || 
                (result == 1 && !EquipItem(item))
               )
            {
                UpgradeGear(item);
                inventory.Add(item, amount, -1);
            }
                
        }
        public void RemoveInvItem(int i, int amount = 1) => inventory.Remove(i, amount);
        /// <summary>
        /// Return -1 to ignore the item, 1 to equip and 2 to store
        /// </summary>
        /// <param name="item"> the item of interest</param>
        /// <param name="equip"> if the npc will even consider equipping it</param>
        /// <returns></returns>
        public int IsInterested(Item item, bool equip)
        {
            if (item.CanBeEquip())
            {
                Item against = equipment.GetGear(item.type);
                if (against == null) return 1;

                double iCond = item.condition / item.durability;

                /*if (profession == Profession.Merchant && !equip && 
                   (iCond >= 0.30 || (iCond *100 + (item.tier - against.tier) > 5)))
                        return 2;//*/
                return against.tier < item.tier && iCond >= 0.5 ? 1: //Equip
                        iCond >= 0.35 || (iCond + (item.tier - against.tier) > 0.20) ? 2 //Store
                        : -1; //ditch
            }
            else 
            {
                if (Rank() < 20)
                    return 2;
                else if (item.type == Item.IType.Consumable)
                {
                    if (profession == Profession.Peasant)
                        return 2;
                    else if (health < Health())
                        return 2;
                }
                else if ((Rank() / 8) < item.tier)
                    return 2;
            }
            return -1;
        }
        bool EquipItem(int index, bool ignoreInterest = false)
        {
            StackedItems st = inventory.inv[index];
            if(st.item.CanBeEquip() && 
                (ignoreInterest || IsInterested(st.item, true) == 1))
            {
                EquipItem(st.item);
                inventory.Remove(index, 1);
                return true;
            }
            return false;
        }
        bool UpgradeGear(Item item)
        {
            int upgradeCost = 250;

            if (!item.CanBeEquip() || KnowsSchematic(item.originalName, item.name) == -1
                || KCoins < upgradeCost) return false;

            if (item.tier < level * 5)
            {
                int tiers = (item.tier - (level * 5)) * upgradeCost > KCoins ?
                    (int)(KCoins / upgradeCost) : item.tier - (level * 5);

                item.Scale(tiers + item.tier);
                KCoins -= tiers * upgradeCost;

                _ = Handlers.UniqueChannels.Instance.SendMessage("Population", $"{displayName} Upgraded {item.originalName} to {item.name} reaching rank {item.tier}");

                return true;
            }
            return false;
        }
        #endregion

        internal void Respawn()
        {
            health = Health();
            Areas.AreaPartials.Area birthPlace = Areas.AreaPartials.Area.LoadArea(origin);
            if(birthPlace == null)
            {
                origin = "Neitsillia\\Casdam Ilse\\Central Casdam\\Atsauka\\Atsauka";
                birthPlace = Areas.AreaPartials.Area.LoadArea(origin);
            }
            PopulationHandler.Add(birthPlace, this);
        }
        internal bool IsPet()
        {
            string[] d = null;
            return profession == Profession.Creature && (faction == Reputation.Faction.Factions.Pet || 
                (d = origin?.Split('\\'))?.Length > 1 && d != null && ulong.TryParse(d[0], out _));
        }

        #region Equipment

        bool EquipItem(Item item, bool upgrade = true)
        {
            if (!HasGearSlot(item.type)) return false;

            //Equip the new item and return what was unequipped
            Item toRem = equipment.Equip(item, 0);

            //NPC upgrades the gear bc why not?
            if (upgrade) UpgradeGear(item);

            if (toRem != null) inventory.Add(toRem, 1, -1);//Return the unequipped item to inventory
            return true;
        }
        internal void SelfGear()
        {
            for(int i = 0; i <= Equipment.gearCount; i++)
                SelfGear(i);
        }
        internal void SelfGear(int i)
        {
            if (i < 0 || i > Equipment.gearCount
                || i == 1 
                )return;
            //skips secondary weapon as it is not yet implemented

            Item gear = null;
            //If the slot is available and the slot is empty 
            if (HasGearSlot(i) && (gear = equipment.GetGear(i)) == null)
            {
                //Finds the first item to fit the slot
                int index = inventory.FindIndex(Equipment.GetItemType(i));
                //Checks if an item was found and if it can be equipped
                if (index > -1 && EquipItem(inventory.GetItem(index)))
                    inventory.Remove(index, 1); //if equipped, remove from inventory
            }
        }
        #endregion

        internal bool ConsumeHealing()
        {
            int index = inventory.FindIndex(Item.IType.Healing);
            if(index > -1)
            {
                Item meds = inventory.GetItem(index);
                long missingHP = Health() - health;
                int m = NumbersM.CeilParse<int>(missingHP / (meds.healthBuff + 0f));
                int amount = Verify.Max(m, inventory.GetCount(index));

                Healing(meds.healthBuff * amount);
                StaminaE(meds.staminaBuff * amount);
                inventory.Remove(index, amount);
                return true;
            }
            return false;
        }
        //XP
        public long XPGain(long xpGain, int mod = 1)
        {
            if (mod < 1)
                mod = Verify.Min(level, 1);
            experience += xpGain = NumbersM.CeilParse<long>(Verify.Min((xpGain * ReferenceData.xprate) / mod, 10));
            long reqXPToLVL = Quadratic.XPCalc(level + 1);
            while (experience >= reqXPToLVL)
            {
                _ = Handlers.UniqueChannels.Instance.SendMessage("Population", displayName + " leveled up!");
                level++;
                KCoins += level * 10;
                stats.maxhealth += 5;
                experience -= reqXPToLVL;
                reqXPToLVL = Quadratic.XPCalc(level + 1);
                if (role == CombatRole.None && level >= 20)
                    GetCombatRole();
                if (IsGainSkillPoint())
                    AddStats();
            }
            return xpGain;
        }
        public new long XPDrop(int mod)
        {
            return Convert.ToInt64(
                base.XPDrop(mod) * xpDropBuff);
        }

        private void GetCombatRole()
        {
            int[] rolls = new int[Enum.GetValues(typeof(CombatRole)).Length];
            for (int i = 1; i < rolls.Length; i++)
                rolls[i] = Program.rng.Next(1, 101);
            //Stat var
            rolls[1] += stats.GetINT();
            rolls[2] += stats.GetSTR();
            //Get Highest roll
            int c = 0;
            for (int j = 0; j < rolls.Length; j++)
                if (rolls[j] > rolls[c])
                    c = j;
            //Apply new role
            role = (CombatRole)c;
            string[] avPerks = null;
            switch(role)
            {
                case CombatRole.Healer:
                    abilities.Add(Ability.Load("Healing Spore", Math.Min(level, 40)));
                    avPerks = new[]{ "Adrenaline", "Energizing Touch",
                    "Adaptation",};
                    
                    stats.intelligence += 3;
                    break;
                case CombatRole.Fighter:
                    abilities.Add(Ability.Load("Execute", Math.Min(level/2, 25)));
                    avPerks = new[] {"Unstoppable Force", "Precision Enhancement",
                    "Fighting Spirit"};

                    stats.strength += 3;
                    break;
            }
            if (avPerks != null)
                perks.Add(PerkLoad.Load(Utils.RandomElement(avPerks)));
        }

        //Evolves
        public void Evolve(int intensity = 1, bool inventory = true, bool perkGain = true, string skaviCat = null)
        {
            for(int i = 0; i < intensity; i++)
                AddStats();
            ScaleNPC(intensity / 100.0);
            if (inventory)
            {
                string[] icollection = MobDrops(Program.rng.Next(intensity, 5 + intensity));
                if (icollection != null)
                    foreach (string i in icollection)
                    {
                        if (i != null)
                            AddItemToInv(SpawnItem(i), 1, false);
                    }
                int dropType = -1;
                int x = Program.rng.Next(101) - intensity;
                if (x <= 10)
                    AddItemToInv(SkaviDrops.FromArea(skaviCat));
                else
                {
                    if (x <= 30)
                        dropType = 5;
                    AddItemToInv(Item.RandomItem(level, dropType), 1);
                }

                if (Program.Chance(20))
                    AddItemToInv(Item.CreateRepairKit(1), Program.rng.Next(1, 6));
            }
            if (perkGain && Program.rng.Next(101) <= 15)
                perks.Add(PerkLoad.RandomPerk(0, "Character"));
        }
        public void GetsAProfession(Profession p)
        {
            if(profession == Profession.Child 
                && level >= 13)
            {
                int rng = Program.rng.Next(101);
                if(rng <= 30) //30% chance to get the Profession
                {
                    profession = p;
                    faction = Reputation.Faction.Factions.Civil;
                    _ = Handlers.UniqueChannels.Instance.SendMessage("Population", $"{displayName} Learned the trade of {p}");
                }
            }
        }
        //
        public EmbedBuilder NPCInfo(EmbedBuilder embed, bool basicinfo = true, bool getStats = true, 
            bool getInv = true, bool getEq = true)
        {
            if (embed == null)
            {
                embed = new EmbedBuilder();
                embed.WithTitle(name);
            }
            embed.AddField("Stats", NPCInfo(basicinfo, getStats));
            if (getEq)
            {
                embed.AddField("Equipment", GearList(), true);
            }
            if (getInv)
            {
                embed.AddField("Inventory", CharacterInfo(false, true, false, false, false, false), true);
            }
            return embed;
        }

        public string NPCInfo(bool basicinfo = true, bool basicStats = true, bool getStats = false,
    bool getInv = false, bool getEq = false, bool getschems = false, bool getAbility = false, bool getPerks = false)
        {
            int l = 38;
            string strname = $"| **{displayName}** |";
            if (strname.Length < l)
            {
                int stars = (l - strname.Length) / 2;
                for (int i = 0; i < stars; i++)
                    strname = $"-{strname}-";
            }
            string info = $"{strname}{Environment.NewLine}";
            if(basicinfo)
            info += "|Level: " + level + Environment.NewLine
                    + "|Profession: " + profession + Environment.NewLine
                    + "|Combat Role: " + role.ToString() + Environment.NewLine
                    + "|Rank: " + Rank() + Environment.NewLine;
            if (basicStats)
            {
                info += $"|HP: {health}/{Health()} {Environment.NewLine}"
                    + $"|SP: {stamina}/{Stamina()} {Environment.NewLine}"
                    + $"|Experience: {experience}/{Quadratic.XPCalc(level + 1)} {Environment.NewLine}"
                    + $"|Kutsyei Coins: {KCoins} {Environment.NewLine}";
            }
            info += base.CharacterInfo(getStats, getInv, 
                getEq,
                getschems, getAbility, getPerks);
            return info;
        }

        internal string GetInfo_NPCGeneral()
        {
            return "Level: " + level + Environment.NewLine
                    + "Profession: " + profession + Environment.NewLine
                    + "Combat Role: " + role.ToString() + Environment.NewLine
                    + "Rank: " + Rank() + Environment.NewLine;
        }

        internal EmbedBuilder StatEmbed() => 
            StatEmbed(displayName, profession == Profession.Creature && !IsPet() ? desc : null, GetInfo_NPCGeneral());
    }
}
