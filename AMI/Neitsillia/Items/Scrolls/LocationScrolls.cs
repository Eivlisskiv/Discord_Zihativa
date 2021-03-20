using AMI.Neitsillia.Areas.AreaPartials;
using AMI.Neitsillia.User.PlayerPartials;
using AMYPrototype;
using Discord;
using System.Threading.Tasks;

namespace AMI.Neitsillia.Items.Scrolls
{
    public partial class Scrolls
    {
        protected static async Task<bool> InCombat(Player player, IMessageChannel chan)
        {
            if (player.IsEncounter("Combat"))
            {
                await chan.SendMessageAsync("You may not use this scroll while in combat");
                return true;
            }
            return false;
        }

        private static async Task<Area> TeleportToArea(Player player, string id)
        {
            player.EndEncounter();
            Area area = Area.LoadArea(id);
            await player.SetArea(area);

            return area;
        }

        public static async Task Scroll_Of_Homecoming(Player player, int slot, IMessageChannel channel)
        {
            if (await InCombat(player, channel)) return;

            player.respawnArea ??= "Neitsillia\\Casdam Ilse\\Central Casdam\\Atsauka\\Atsauka";

            Area current = player.Area;

            if(current.AreaId == player.respawnArea)
            {
                await channel.SendMessageAsync("You are already in the area this scroll would take you to.");
                return;
            }

            if(current.IsDungeon)
                await Program.data.database.DeleteRecord<Area>("Dungeons", current.AreaId, "AreaId");

            Area area = await TeleportToArea(player, player.respawnArea);
            player.inventory.Remove(slot, 1);

            await player.NewUI(null, area.AreaInfo(0).Build(), channel, User.UserInterface.MsgType.Main);
        }
    }
}
