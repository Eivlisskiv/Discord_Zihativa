using AMI.Neitsillia.Areas.Arenas;
using AMI.Neitsillia.User.PlayerPartials;
using Discord;

namespace AMI.Neitsillia.Areas.AreaPartials
{
    public partial class Area
    {
        public Arena arena;

        public EmbedBuilder ExploreArena(Player player, EmbedBuilder embed)
        {
            if(arena != null)
            {
                return arena.Explore(player, embed);
            }

            return embed;
        }
    }
}
