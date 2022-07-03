using AMI.Methods;
using AMI.Methods.Graphs;
using AMI.Neitsillia.Collections;
using AMI.Neitsillia.Items;
using AMI.Neitsillia.Items.Abilities;
using AMI.Neitsillia.Items.Abilities.Load;
using AMI.Neitsillia.Items.ItemPartials;
using AMI.Neitsillia.Items.Perks.PerkLoad;
using AMI.Neitsillia.User.PlayerPartials;
using AMYPrototype;
using Neitsillia.Methods;
using System.Collections.Generic;

namespace AMI.Neitsillia.NPCSystems
{
	public partial class NPC
	{
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

        public static NPC NewNPC(int level, string profession = "Child", string race = null)
        {
            string[] availableRaces = { "Human" };
            profession ??= "Child";
            race ??= availableRaces[Program.rng.Next(availableRaces.Length)];

            NPC npc = GenerateNPC(Verify.Min(level, 0), $"{race} {profession}");
            if (npc == null) return null;
            npc.faction = Reputation.Faction.Factions.Civil;

            switch (npc.displayName.Split(' ')[0].ToLower())
            {
                case "stranger":
                case "human":
                    npc.name = RandomName.ARandomName() + " " + RandomName.ARandomName();
                    break;
            }

            return npc;
        }


        public static NPC GenerateNPC(int level, string name)
        {
            NPC mob = Database.LoadRecord("Creature", AMIData.MongoDatabase.FilterEqual<NPC, string>(
                "displayName", name));
            return mob != null ? GenerateNPC(level, mob) : null;
        }

        public static NPC GenerateNPC(int level, NPC mob)
        {
            mob.level = Verify.Min(level, mob.baseLevel);
            if (mob.level > mob.baseLevel) mob.ScaleNPC();

            mob.NPCSetUp();
            return mob;
        }

        public NPC(Player p) : base(p)
        {
            //Shared
            name = RandomName.ARandomName() + " " + RandomName.ARandomName();

            profession = ReferenceData.Profession.Peasant;
            faction = Reputation.Faction.Factions.Civil;

            if (level >= 20)
                GetCombatRole();

            origin = p.Area.parent ?? p.Area.name;
            displayName = name + " Of " + origin;

            baseLevel = p.level;
            race = p.race.ToString();

            hasweapon = true;
            hashelmet = true;
            hasmask = true;
            haschestp = true;
            hasjewelry = true;
            hastrousers = true;
            hasboots = true;
        }

        private void ScaleNPC(double mult = 1)
        {
            double buffPercent =
                //level/(double)baseLevel* 2
                Exponential.CreatureScale(baseLevel, level)
                * mult;

            //Stats
            stats.maxhealth = NumbersM.CeilParseLong(stats.maxhealth * (1 + buffPercent * 5));
            stats.stamina = NumbersM.CeilParseInt(stats.stamina * (1 + buffPercent * 2));
            health = Health();
            stamina = Stamina();
            for (int i = 0; i < ReferenceData.DmgType.Length; i++)
            {
                if (stats.damage[i] > 0)
                    stats.damage[i] = NumbersM.CeilParseLong(stats.damage[i] * (1 + buffPercent / 3));
                if (stats.resistance[i] > 0)
                    stats.resistance[i] = NumbersM.CeilParseInt(stats.resistance[i] * (1 + (buffPercent / 5)));
            }

            //Extra Drops
            if (Program.rng.Next(101) <= 1)
            {

                Item tempSchem = SkaviDrops.DropSchematic(race);
                if (tempSchem != null)
                    AddItemToInv(tempSchem);
            }
        }

        private void NPCSetUp()
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
                if (perks[i].name == "-Random")
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
    }
}
