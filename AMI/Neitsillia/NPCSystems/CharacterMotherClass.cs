using AMI.Methods;
using AMI.Methods.Graphs;
using AMI.Module;
using AMI.Neitsillia.Collections;
using AMI.Neitsillia.Crafting;
using AMI.Neitsillia.Items;
using AMI.Neitsillia.Items.Abilities;
using AMI.Neitsillia.Items.Abilities.Load;
using AMI.Neitsillia.Items.Perks.PerkLoad;
using AMI.Neitsillia.User.UserInterface;
using AMYPrototype;
using AMYPrototype.Commands;
using Discord;
using AMI.Neitsillia.Items.ItemPartials;
using System;
using System.Collections.Generic;

namespace AMI.Neitsillia.NPCSystems
{
    public abstract class CharacterMotherClass
    {
        #region Public Vars
        public long health;
        public int stamina;
        public int level;
        public string name;
        
        //
        public Stats stats = new Stats();
        public Equipment equipment = new Equipment();
        public List<Ability> abilities = new List<Ability>() { LoadAbility.Brawl("Brawl") };

        public Specter specter;

        public List<Perk> perks = new List<Perk>();
        public List<Perk> status = new List<Perk>();
        //
        public Inventory inventory = new Inventory();
        public List<Schematic> schematics = new List<Schematic>();
        public long KCoins { get; set; }
        public long experience;
        #endregion


        /// <summary>
        /// 
        /// </summary>
        /// <param name="name">name of the perk</param>
        /// <param name="turn">rank that will be given to the perk and will be used 
        /// as a turn counter until the perk is no longer in effect</param>
        /// <param name="intensity">will be set as the perk's tier, if applicable,
        /// will increase the effect of the perk</param>
        /// <returns>-1: no changes, 0: perk was updated, 1: perk was added</returns>
        internal int Status(string aname, int turn = 1, int intensity = 1)
        {
            int index = status.FindIndex(item => item.name == aname);
            if (index > -1)
                return Status(index, turn, intensity);
            return Status(PerkLoad.Effect(aname, turn, intensity), true);
        }
        private int Status(int index, int turn = 1, int intensity = 1)
        {
            bool changed = false;
            if (status[index].rank < turn)
            {
                status[index].rank = turn;
                if (status[index].maxRank < turn)
                    status[index].maxRank = turn;
                changed = true;
            }
            if (status[index].tier < intensity)
            {
                status[index].tier = intensity;
                changed = true;
            }
            if (changed)
            {
                status[index].desc = PerkLoad.Effect(status[index].name, status[index].rank,
                      status[index].tier).desc;
                return 0;
            }
            return -1;
        }
        internal int Status(Perk perk, bool justAdd = false)
        {
            if (!justAdd)
            {
                int index = status.FindIndex(item => item.name == perk.name);
                if (index > -1)
                    return Status(index, perk.rank, perk.tier);
            }
            status.Add(perk);
            return 1;
        }
        internal int StaminaE(int amount)
        {
            if (amount != 0)
            {
                amount = PerkLoad.CheckPerks(this, Perk.Trigger.StaminaAmount, amount);
                PerkLoad.CheckPerks(this, Perk.Trigger.Stamina, this);
                stamina += (
                    amount < 0 ? Verify.Min(amount, -stamina) :
                    Verify.Max(amount, Stamina() - stamina));
            }
            return amount;
        }
        internal int StaminaE(double percentage)
        {
            if (percentage != 0)
            {
                int maxSP = Stamina();
                if (percentage > 0 && stamina >= maxSP)
                    return 0;
                else if (percentage < 0 && stamina <= 0)
                    return 0;
                return StaminaE(Math.Max(1, NumbersM.NParse<int>(maxSP * percentage)));
            }
            return 0;
        }

        internal long TakeDamage(long damage)
        {
            return -Healing(-damage, true);
        }
        internal long TakeDamage(long damage, int type)
        {
            return -Healing(
                -Combat.NPCCombat.ElementalResistance(
                Resistance(type), damage)
                , true);
        }
        internal long TakeDamage(long damage, ReferenceData.DamageType type)
        => TakeDamage(damage, (int)type);
        internal long Healing(long heal, bool canOverHeal = false)
        {
            if (heal != 0)
            {
                if (!canOverHeal)  heal = Verify.Max(heal, Verify.Min(Health() - health, 0));
                heal = PerkLoad.CheckPerks<long>(this, Perk.Trigger.Healing, heal);
                PerkLoad.CheckPerks(this, Perk.Trigger.Health, this);
                health += heal;
            }
            return heal;
        }
        internal long PercentHealing(int percent, bool canOverheal = false)
        {
            if (percent > 0)
                return Healing(NumbersM.NParse<long>(Health() * (percent / 100.00)), canOverheal);
            return 0;
        }
        internal long PercentHealing(double percent, bool canOverheal = false)
        {
            if (percent > 0)
                return Healing(NumbersM.NParse<long>(Health() * percent), canOverheal);
            return 0;
        }

        public long Health(bool bonus = true)
        { return stats.MaxHealth() + equipment.Health() + (bonus ? PerkLoad.CheckPerks<long>(this, Perk.Trigger.MaxHealth, 0) : 0 ); }
        public int Rank()
        {
            return equipment.Rank() + (this is NPC n ? ((level - n.baseLevel) / 5) : 0);
        }
        public int Agility() => Convert.ToInt32(stats.agility + equipment.Agility());
        public int Stamina() => Math.Max(Convert.ToInt32(Math.Floor(Agility() * .8)) + equipment.Stamina() + stats.stamina, 1);

        public double CritChance() 
            => stats.CritChance() * (1 + (equipment.CritChance()/100));

        public double CritMult() 
            => stats.CritMult() * (1 + (equipment.CritMult()/100) );


        public int Resistance(int i)
        {
            while(stats.resistance.Length < ReferenceData.DmgType.Length)
                stats.resistance = ArrayM.AddItem(stats.resistance, 0);
            return equipment.Resistance(i) + stats.resistance[i];
        }
        public long Damage(int i)
        {
            while(stats.damage.Length < ReferenceData.DmgType.Length)
                stats.damage = ArrayM.AddItem(stats.damage, 0);
            if (i == 0)
                return Convert.ToInt32((equipment.Damage(i) + stats.damage[i]) * stats.PhysicalDMG());
            return equipment.Damage(i) + stats.damage[i];
        }
        public long[] Damage()
        {
            long[] damages = new long[ReferenceData.DmgType.Length];
            for (int i = 0; i < damages.Length; i++)
                damages[i] = Damage(i);
            return damages;
        }
        public long TotalBaseDamage()
        {
            long damage = 0;
            for (int i = 0; i < ReferenceData.DmgType.Length; i++)
                damage += Damage(i);
            return damage;
        }

        public double Efficiency() => stats.Efficiency();
        public long PowerLevel()
        {
            long power = Health() + Stamina() + Agility() + NumbersM.NParse<long>(CritChance() +
                CritMult());
            for (int i = 0; i < ReferenceData.DmgType.Length; i++)
                power += Damage(i) + Resistance(i);
            if (stats.intelligence / 2 > 1)
                power *= stats.intelligence / 2;
            power += perks.Count * 20;
            for (int i = 0; i < 9; i++)
            {
                var g = equipment.GetGear(i);
                if (g != null && g.perk != null)
                    power += 20;

            }
            return power;
        }
        public int InventorySize() => 30 + stats.strength * Stats.invSizePerStr;

        public long XPDrop(int mod) 
            => (Quadratic.XPCalc(level) + experience) / (1 + mod);

        void AddStats(int stat = 0)
        {
            if (stat == 0)
                stat = Program.rng.Next(1, 7);
            switch (stat)
            {
                case 1: stats.endurance++; break;
                case 2: stats.intelligence++; break;
                case 3: stats.strength++; break;
                case 4: stats.charisma++; break;
                case 5: stats.dexterity++; break;
                case 6: stats.perception++; break;
            }
        }
        public bool IsGainSkillPoint(int atLevel = -1)
        {
            if (atLevel == -1)
                atLevel = level;
            int p = 0;
            int i = 5;
            while(i < atLevel)
                i += p+= 5;
            if (atLevel == i)
                return true;
            return false;
        }
        public int KnowsSchematic(string originalName, string name = null)
        {
            int i = schematics.FindIndex(delegate (Schematic item)
            { return item.name == originalName; });
            if(name != null && i < 0)
                i = schematics.FindIndex(delegate (Schematic item)
                { return item.name == name; });
            return i;
        }
        internal bool IsDead() => health <= 0;
        
        /// <summary>
        /// Return the health status
        /// </summary>
        /// <param name="result">Health status string</param>
        /// <param name="getLiveAprox">Get the name of the living state instead of exact hp</param>
        /// <returns>
        /// >= 0 Is alive
        /// -1 is Down, -2 Fainted, -3 Unconscious, -4 Dead, -5 Vaporized
        /// </returns>
        internal int HealthStatus(out string result, bool getLiveAprox = false)
        {
            var mhp = Health();
            double percent = ((double)health / mhp) * 100.00;
            if (IsDead())
            {
                
                result = EUI.brokenhealth;
                if (percent <= -100)
                {
                    result += " Vaporized";
                    return -5;
                }
                else if (percent <= -80)
                {
                    result += " Dead";
                    return -4;
                }
                else if (percent <= -60)
                {
                    result += " Unconscious";
                    return -3;
                }
                else if (percent <= -45)
                {
                    result += " Fainted";
                    return -2;
                }
                else
                {
                    result += " Down";
                    return -1;
                }
            }
            else
            {
                if (getLiveAprox)
                    result = GetAproxHealth(mhp);
                else
                    result = $"{EUI.health} {Utils.Display(health)}/{Utils.Display(mhp)}";
            }
            return NumbersM.FloorParse<int>(percent / 10);
        }
        string GetAproxHealth(long? mhp = null)
        {
            mhp = mhp ?? Health();

            if (health < mhp * 0.10)
                return "Dying";
            else if (health < mhp * 0.6)
                return "Hurt";
            else if (health < mhp * 0.8)
                return "Bruised";
            return "Healthy";
        }
        internal string StaminaStatus() =>
            stamina <= 0 ? EUI.tired + " Exhausted" : $"{EUI.stamina} {stamina}/{Stamina()}";

        #region Experience
        internal long XpRequired() => Quadratic.XPCalc(level + 1);
        internal string DetailedXP()
        {
            long req = XpRequired();
            return $"**{Utils.Display(experience)}/{Utils.Display(req)}** | **{Utils.Display(req - experience)}** To next level";
        }

        public abstract long XpGain(long xpGain, int mod = 1);
        #endregion

        public string CharacterInfo(bool getStats, bool getInv = true, bool getEq = true, bool getschems = true,
            bool getAbility = true, bool getPerks = true)
        {
            int l = 38;
            string info = "";
            if (getStats)
            {
                info += GetInfo_MainSkills();

                info += GetInfo_DmgRes();

                info += "| Agility: " + Agility()
                    + Environment.NewLine + $"| Critical Chance: {CritChance()}%"
                    + Environment.NewLine + $"| Critical Damage: {CritMult()}%" + Environment.NewLine;
            }
            if (getEq)
            {
                string npcEq = "|**Equipment**" + Environment.NewLine + GearList("| ");
                info += npcEq;
            }
            if (getInv)
            {
                string npcInv = "|**Inventory**" + Environment.NewLine;
                if (inventory != null)
                    for (int i = 0; i < inventory.Count; i++)
                    {
                        if (inventory.GetItem(i) != null)
                            npcInv += $"| {i}| {inventory.inv[i]}" + Environment.NewLine;
                    }
                if (inventory == null || inventory.Count == 0)
                    npcInv += "Empty" + Environment.NewLine;
                info += npcInv;
            }
            if (getschems)
            {
                string schList = null;
                if (schematics != null)
                    foreach (var s in schematics)
                        schList += $"| {s}" + Environment.NewLine;
                if (schList != null)
                    info += $"|**Schematics** {Environment.NewLine}{schList}";
            }
            if (getAbility)
            {
                string strability = null;
                if (abilities != null)
                    foreach (var a in abilities)
                        strability += $"| {a}{Environment.NewLine}";
                if (strability != null)
                    info += $"|**Abilities** {Environment.NewLine}{strability}";
            }
            if(getPerks)
            {
                info += "**Perks**" + Environment.NewLine;
                foreach (var p in perks)
                    info += $"| {p}{Environment.NewLine}";
            }
            for (int i = 0; i < l; i++)
                info += "-";
            return info;
        }

        internal string GetInfo_Stats()
        {
            return  $"Health: {health}/{Health()}"
                    + Environment.NewLine + $"Stamina: {stamina}/{Stamina()}"
                    + Environment.NewLine + $"Agility: {Agility()}"
                    + Environment.NewLine + $"Critical Chance: {CritChance()}%"
                    + Environment.NewLine + $"Critical Damage: {CritMult()}%";
        }

        internal string GetInfo_MainSkills(string seperator = "| ")
        {
            return  $"END:{stats.endurance}{GameCommands.SkillBuff(0, stats, null)}{seperator}" +
                    $"INT:{stats.intelligence}{GameCommands.SkillBuff(1, stats, null)}{seperator}" +
                    $"STR:{stats.strength}{GameCommands.SkillBuff(2, stats, null)}{seperator}" +
                    $"CHA:{stats.charisma}{GameCommands.SkillBuff(3, stats, null)}{seperator}" +
                    $"DEX:{stats.intelligence}{GameCommands.SkillBuff(4, stats, null)}{seperator}" +
                    $"PER:{stats.perception}{GameCommands.SkillBuff(5, stats, null)}{seperator}";
        }

        internal string GetInfo_DmgRes()
        {
            string s = null;
            for (int i = 0; i < ReferenceData.DmgType.Length; i++)
            {
                long res = Resistance(i);
                long dmg = Damage(i);
                if (dmg > 0 || res != 0)
                {
                    s += $"{EUI.GetElement((ReferenceData.DamageType)i)}: ";

                    if (dmg > 0)
                        s += $"{EUI.attack}: {dmg} |-";
                    if (res != 0)
                        s += $"-| {EUI.shield}: {res}";

                    s += Environment.NewLine;
                }
            }
            return s;
        }

        internal string GearList(string start = null)
        {
            string str = null;
            for (int i = 0; i < Equipment.gearCount; i++)
            {
                if (i != 1)
                {
                    Item gear = equipment.GetGear(i);
                    if (gear != null)
                        str += start + gear.ToString() + Environment.NewLine;
                    else if(!(this is NPC n) || n.HasGearSlot(i))
                        str += start + $"*[No {Equipment.SlotName(i)}]*" + Environment.NewLine;
                }
            }
            return str ?? "Equipment Unavailable" + Environment.NewLine;
        }

        internal string GetInfo_General()
        {
            return "Level: " + level + Environment.NewLine
                    + "Rank: " + Rank() + Environment.NewLine;
        }

        internal EmbedBuilder StatEmbed(string name = null, string desc = null, string general = null)
        {
            return DUtils.BuildEmbed(
                name ?? this.name, desc , null, Color.DarkRed,

                DUtils.NewField("General", general ?? GetInfo_General() ?? "Null", true),
                DUtils.NewField("Skills", GetInfo_MainSkills(Environment.NewLine) ?? "Null", true),
                DUtils.NewField("Stats", GetInfo_Stats() ?? "Null", true),
                DUtils.NewField($"{EUI.attack} & {EUI.shield}", GetInfo_DmgRes() ?? "Null", true)
                );
        }
    }
}
