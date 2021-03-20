using AMI.Methods;
using AMI.Neitsillia.Areas.AreaPartials;
using AMI.Neitsillia.NPCSystems;
using AMYPrototype;
using AMI.Neitsillia.Items.ItemPartials;
using System.Collections.Generic;

namespace AMI.Neitsillia.Areas
{
    static class Dungeons
    {
        const int DUNGEON_FLOORS = 4;
        //
        internal static Area Generate(int floor, Area fromArea, bool auto = true)
        {
            int f = Verify.Max(floor, fromArea.floors - DUNGEON_FLOORS);
            Area dungeon = new Area(false)
            {
                level = fromArea.level + Program.rng.Next(6),
                floors = f + DUNGEON_FLOORS,
                name = $"{fromArea.name} Dungeon",
                type = AreaType.Dungeon,
                //
                eMobRate = 70,
                ePassiveRate = 30,
                passiveEncounter = new string[]{"Floor"},
                //
                realm = fromArea.realm,
                continent = fromArea.continent,
                kingdom = fromArea.kingdom,
                parent = fromArea.name,
            };
            if (!auto) return dungeon;
            return DungeonType(fromArea, dungeon);
        }
        public static NPC GetBoss(Area dungeon)
        {
            NPC boss = Utils.RunMethod<NPC>(dungeon.name.Split(':')[1].Replace(" ", "") + "_Boss", 
                typeof(Dungeons), dungeon);
            boss.level = dungeon.GetAreaFloorLevel(Program.rng);
            boss.Evolve(2, true, false);
            if (Program.Chance(50))
                boss.AddItemToInv(Item.RandomItem(boss.level, 5));
            return boss;
        }
        static Area DungeonType(Area fromArea, Area dungeon)
        {
            string creatureRace = fromArea.GetAMob(Program.rng, 0).race;
            var method = Utils.GetFunction(typeof(Dungeons), creatureRace.Replace(" ", "") + "_Dungeon", true);
            if (method == null)
                method = MobType(fromArea, ref creatureRace);
            dungeon.name += $" : {creatureRace}";
            return (Area)method.Invoke(null, new object[] { dungeon });
        }
        internal static Area ManualDungeon(string creatureRace, int floor, Area parent)
        {
            var dungeon = Generate(floor, parent, false);
            var method = Utils.GetFunction(typeof(Dungeons), creatureRace.Replace(" ", "") + "_Dungeon", true);
            if (method == null)
                return null;
            dungeon.name += $" : {creatureRace}";
            return (Area)method.Invoke(null, new object[] { dungeon });
        }

        internal static System.Reflection.MethodInfo MobType(Area fromArea, ref string creatureName)
        {
            foreach (var tier in fromArea.mobs)
                foreach (string mob in tier)
                {
                    NPC creature = NPC.GenerateNPC(0, mob);
                    var m = Utils.GetFunction(typeof(Dungeons), creature.race + "_Dungeon", true);
                    creatureName = creature.race;
                    if (m != null)
                        return m;
                }
            creatureName = "Vhoizuku";
            return Utils.GetFunction(typeof(Dungeons), "Vhoizuku_Dungeon");
        }
        //Goq
        //Vhoizukus
        public static Area Vhoizuku_Dungeon(Area dungeon)
        {
            dungeon.mobs = new List<string>[]
            {
                new List<string>{
                    "Young Vhoizuku",
                    "Vhoizuku",
                    "Vhoizuku",
                    "Vhoizuku Warrior",
                    "Vhoizuku Warrior",
                    "Vhoizuku Warrior",
                    "Vhoizuku Mother",
                },
            };
            dungeon.description += "A large Vhoizuku nest.";
            return dungeon;
        }
        public static NPC Vhoizuku_Boss(Area dungeon)
        {
            return NPC.GenerateNPC(dungeon.level, 
                "Vhoizuku King");
        }
        //Cevharhu
        public static Area Cevharhu_Dungeon(Area dungeon)
        {
            dungeon.mobs = new List<string>[]
            {
                new List<string>{
                    "Cevharhu",
                    "Toxic Cevharhu",
                    "Sparked Cevharhu",
                    "Blue Cevharhu",
                    "Fiery Cevharhu",
                    "Toxic Cevharhu",
                    "Sparked Cevharhu",
                    "Blue Cevharhu",
                    "Fiery Cevharhu",
                    "Cevharhu Queen",
                },
            };
            dungeon.description += "Cevharhu's habitats are humid areas with moss growing everywhere and a few odd plants growing in dark corners.";
            return dungeon;
        }
        public static NPC Cevharhu_Boss(Area dungeon)
        {
            return NPC.GenerateNPC(dungeon.level,
                "Rainbow Cevharhu");
        }
        //Tsuu Bear
        public static Area TsuuBear_Dungeon(Area dungeon)
        {
            dungeon.mobs = new List<string>[]
            {
                new List<string>{
                    "Tsuu Cub",
                    "Tsuu Bear",
                    "Tsuu Bear",
                    "Armored Tsuu Bear",
                    "Rotting Tsuu Bear",
                    "Tsuu Bear Beta",
                    "Tsuu Bear Beta",
                },
            };
            dungeon.description += "A Tsuu Bear Den.";
            return dungeon;
        }
        public static NPC TsuuBear_Boss(Area dungeon)
        {
            return NPC.GenerateNPC(dungeon.level,
                "Tsuu Bear Alpha");
        }
        //Octopus
    }
}
