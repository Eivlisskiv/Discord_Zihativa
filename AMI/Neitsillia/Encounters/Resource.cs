using AMI.Methods;
using AMI.Neitsillia.Areas;
using AMI.Neitsillia.Areas.AreaPartials;
using AMI.Neitsillia.Collections;
using AMI.Neitsillia.User.PlayerPartials;
using AMYPrototype;
using Discord;
using Neitsillia.Items.Item;
using System;

namespace AMI.Neitsillia.Encounters
{
    public static class Resource
    {
        internal static void EmbedBuilder(EmbedBuilder embed, string tool, string item)
        {
            embed.WithTitle($"{item} resource vein");
            embed.WithDescription(
                $"You've encountered a resource vein containing {item}." +
                Environment.NewLine + $" Use your {tool} to exploit it: `tuse {tool}`");
        }

        public static string Exploit(Player player, string tool, string item)
        {
            Tools tools = player.Tools;
            int level = player.Area.GetAreaFloorLevel(Program.rng, player.AreaInfo.floor);
            (int tier, int tlevel, string name) = tools.CanUse(player, tool);

            player.NewEncounter(Encounter.Names.Loot, true);
            double mult = Collect(player, item, level, tier * tlevel);

            long xp = NumbersM.FloorParse<long>((player.level + level) * mult);
            tools.AfterCollect(player, tool, xp);

            player.SaveFileMongo();
            return $"+{xp} {name} xp";
        }

        private static double Collect(Player player, string ressource, int level, int bonus)
        {
            Loot(player.Encounter.loot, new StackedItems(Item.LoadItem(ressource), 1), level);

            if(Program.Chance((bonus - level) * 5))
            {
                Loot(player.Encounter.loot, new StackedItems(Item.RandomItem(level * 5), 1), level);
                return 2;
            }

            return 1;
        }

        private static void Loot(Inventory inventory, StackedItems st, int level)
        {
            if (st.item.CanBeEquip()) st.item.Scale(level);
            else st.count = Math.Max(1, level / st.item.tier);
            inventory.Add(-1, st);
        }

        public static string Get(Area area) => area.type switch
        {
            AreaType.Caves => Utils.RandomElement<Func<string>>(Mushrooms, Pickaxe)(),
            AreaType.Mines => Pickaxe(),
            AreaType.Ruins => Utils.RandomElement<Func<string>>(Sickle, Pickaxe)(),
            AreaType.Wilderness => Utils.RandomElement<Func<string>>(Sickle, Axe)(),
            _ => Utils.RandomElement<Func<string>>(Sickle, Axe, Pickaxe)()
        };

        private static string Mushrooms()
            => $"Sickle;Mui Mush";

        private static string Sickle()
            => $"Sickle;Mui Mush";

        private static string Axe()
            => $"Axe;Wood";

        private static string Pickaxe()
            => $"Pickaxe;Metal Scrap";
    }
}
