using AMI.Methods;
using AMI.Methods.Graphs;
using AMI.Neitsillia.Collections;
using AMI.Neitsillia.Items;
using AMI.Neitsillia.Items.Abilities;
using AMI.Neitsillia.Items.Abilities.Load;
using AMI.Neitsillia.Items.Perks.PerkLoad;
using AMYPrototype;
using Discord;
using MongoDB.Bson.Serialization.Attributes;
using AMI.Neitsillia.Items.ItemPartials;
using Neitsillia.Methods;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using static AMI.Neitsillia.ReferenceData;

namespace AMI.Neitsillia.NPCSystems
{
    [BsonIgnoreExtraElements]
    public partial class NPC : CharacterMotherClass
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
        public override string ToString() =>  $"{displayName} L:{level} R:{Rank()}";

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
            string[] d;
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
                || i == 1 )return;
            //skips secondary weapon as it is not yet implemented

            //If the slot is available and the slot is empty 
            if (HasGearSlot(i) && equipment.GetGear(i) == null)
            {
                //Finds the first item to fit the slot
                int index = inventory.inv.FindIndex(item => 
                    item.item.type == Equipment.GetItemType(i) 
                        &&
                    item.item.condition > 0
                    );
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
        public override long XpGain(long xpGain, int mod = 1)
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
                    + "|Rank: " + Rank() + Environment.NewLine
                    + $"|Kutsyei Coins: {Utils.Display(KCoins)} {Environment.NewLine}";
            if (basicStats)
            {
                info += $"|HP: {health}/{Health()} {Environment.NewLine}"
                    + $"|SP: {stamina}/{Stamina()} {Environment.NewLine}"
                    + $"|Experience: {experience}/{Quadratic.XPCalc(level + 1)} {Environment.NewLine}";
            }
            info += CharacterInfo(getStats, getInv, 
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
