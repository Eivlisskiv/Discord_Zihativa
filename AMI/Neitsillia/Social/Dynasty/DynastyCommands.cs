using AMI.Methods;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AMI.Neitsillia.NeitsilliaCommands.Social.Dynasty
{
    class DynastyCommands : ModuleBase<AMI.Commands.CustomSocketCommandContext>
    {
        [Command("Create Dynasty")]
        public void CreateDynasty(params string[] dynastyName)
        {
            Context.WIPCheck();

            string name = StringM.UpperAt(ArrayM.ToString(dynastyName));
        }
    }
}
