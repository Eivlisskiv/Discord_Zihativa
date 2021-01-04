using AMI.Methods;
using AMYPrototype;
using Discord;
using Discord.Commands;
using Neitsillia.Methods;
using System;
using System.Threading.Tasks;

namespace AMI.AMIData.OtherCommands
{
    public class GenerateCommands : ModuleBase<AMI.Commands.CustomSocketCommandContext>
    {
        [Command("generate random name")]
        public async Task generateRName(int nameL = 0) { await Context.Channel.SendMessageAsync(RandomGeneratedName(nameL)); ; }
        [Command("rng name")]
        public async Task rngName(int nameL = 0) { await Context.Channel.SendMessageAsync(RandomGeneratedName(nameL)); ; }
        public string RandomGeneratedName(int nameL = 0)
        {
            if (nameL < 1)
                nameL = Program.rng.Next(3, 11);
            string name = RandomName.ARandomName(Verify.Max(nameL, 35));
            return name;
        }
        [Command("set random nickname")]
        [RequireUserPermission(GuildPermission.ChangeNickname)]
        [RequireBotPermission(GuildPermission.ManageNicknames)]
        public async Task RandomNickname(IGuildUser user = null, int nameL = 0)
        {
            user = (IGuildUser)Context.User;
            string name = RandomGeneratedName(nameL);
            await user.ModifyAsync(x => {
                x.Nickname = name;
            });

        }
    }
}
