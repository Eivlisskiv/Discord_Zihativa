using AMI.Methods;
using Discord;
using Discord.Commands;
using System;
using System.Threading.Tasks;

namespace AMI.Neitsillia.NeitsilliaCommands
{
    public class LeaderBoardsCommands : ModuleBase<AMI.Commands.CustomCommandContext>
    {
        static string[] options = { "stronghold" };
        [Command("LeaderBoard")]
        async Task LeaderBoard(string arg, int page = 1)
        {
            Context.WIPCheck();

            EmbedBuilder em = null;
            FindLeaderBoardType(arg.ToLower(), page-1);
            if (em == null)
            {
                em = new EmbedBuilder();
                em.WithTitle("LeaderBoards");
                em.WithDescription("Entered argument invalid" + Environment.NewLine + 
                    "Options: " + Environment.NewLine);
                foreach (string s in options)
                    em.Description += s + Environment.NewLine;
            }
            await ReplyAsync(embed: em.Build());
        }
        EmbedBuilder FindLeaderBoardType(string r, int indexPage)
        {
            foreach (string s in options)
                if (r.Equals(s))
                    return Utils.RunMethod<EmbedBuilder>(s + "_LeaderBoard", this, indexPage);
            return null;
        }
        public EmbedBuilder Stronghold_Leaderboard(int page)
        {
            EmbedBuilder e = new EmbedBuilder();
            e.WithTitle("Strongholds LeaderBoard");
            e.WithDescription("Currently unavailable");
            return e;
        }
    }
}
