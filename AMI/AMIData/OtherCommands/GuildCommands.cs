using AMI.AMIData.Servers;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;

namespace AMI.AMIData.OtherCommands
{
    public class GuildCommands : ModuleBase<AMI.Commands.CustomSocketCommandContext>
    {
        [Command("View Permissions")]
        [Alias("View Perms")]
        async Task ViewPermissions(IUser user)
        {
            var perms = ((IGuildUser)user).GuildPermissions;
            var list = Enum.GetValues(typeof(GuildPermission));
            string result = null;
            foreach (var p in list)
                result += $"{(GuildPermission)p} : {perms.Has((GuildPermission)p)}" + Environment.NewLine;
            await ReplyAsync(result);
        }

        [Command("Prefix")]
        async Task Prefix(string newPrefix = null)
        {
            if (Context.guildSettings != null)
            {
                GuildSettings gs = Context.guildSettings;
                if (newPrefix == null)
                    await ReplyAsync("Current Prefix: " + ($"`{gs.prefix}`" ?? "No prefix set, default prefix: `~`"));
                else if ((Context.User as IGuildUser).GuildPermissions.ManageChannels)
                {
                    gs.prefix = newPrefix;
                    gs.SaveSettings();

                    var bot = Context.Client.CurrentUser;
                    try
                    {
                        await Context.Guild.GetUser(bot.Id).ModifyAsync(u =>
                        {
                            u.Nickname = bot.Username + $" [{newPrefix}]";
                        });
                    }
                    catch (Exception) { }

                    await ReplyAsync($"Prefix changed to `{newPrefix}`");
                }
                else
                    await ReplyAsync("Manage Channels permission is required to change prefix");
            }
            else await ReplyAsync("No prefix is required for Direct Messaging");
        }

        [RequireUserPermission(GuildPermission.Administrator)]
        [Command("Memberships")]
        async Task Memberships(string isAdd, IRole role, int price = 1)
        {
            switch(isAdd.ToLower())
            {
                case "add":
                case "true":
                    if(price < 0)
                    {
                        await ReplyAsync("role price may not be below 0");
                        return;
                    }
                    await ReplyAsync(Context.guildSettings?.UpdateTiers(role, true, price));
                    break;
                case "remove":
                case "rem":
                case "false":
                    await ReplyAsync(Context.guildSettings?.UpdateTiers(role, false, 0));
                    break;
                default:
                    await ReplyAsync("Incorrect first argument, must be \"add\" or \"remove\"");
                    break;
            }
        }
    }
}
