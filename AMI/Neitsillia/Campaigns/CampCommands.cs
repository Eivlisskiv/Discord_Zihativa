using AMI.AMIData.OtherCommands;
using AMI.Methods;
using AMI.Neitsillia.Campaigns;
using Discord.Commands;
using System.IO;
using System.Threading.Tasks;

namespace AMI.Module
{
    class CampCommands :  GameMaster
    {
        
        /*[Command("New Campaign")]
        [Alias("ncampg")]//*/
        public async Task New_Campaign([Remainder] string arg)
        {
            if(IsGMLevel(0).Result)
            {
                GM gm = GetGM(Context.User.Id);
                if (gm.campaignPath != null && File.Exists(gm.campaignPath))
                    await ReplyAsync("You are already hosting a campaign");
                else
                {
                    string name = StringM.UpperAt(arg);
                    Campaign campaign = Campaign.Load(name);
                    if(campaign == null && IsGMLevel(3).Result)
                    {

                    }
                    else if(campaign != null)
                    {

                    }
                }

            }
        }
    }
}
