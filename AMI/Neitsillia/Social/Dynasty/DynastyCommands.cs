using AMI.Methods;
using AMI.Module;
using AMI.Neitsillia.User.PlayerPartials;
using AMI.Neitsillia.User.UserInterface;
using AMYPrototype.Commands;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AMI.Neitsillia.NeitsilliaCommands.Social.Dynasty
{
    [Name("Dynasty")]
    public class DynastyCommands : ModuleBase<AMI.Commands.CustomSocketCommandContext>
    {
        public static async Task<(Dynasty dan, DynastyMember membership)> GetDynasty(Player player)
        {
            (Dynasty dan, DynastyMember membership, string error) = await Dynasty.Load(player);
            if (error != null) throw NeitsilliaError.ReplyError(error);
            return (dan, membership);
        }

        [Command("Dynasty Info", true)]
        [Alias("dinfo")]
        public async Task DynsatyHub()
        {
            Player player = Context.GetPlayer(Player.IgnoreException.Resting);
            (Dynasty dan, DynastyMember membership) = await GetDynasty(player);
            await DynastyHub(player, dan, membership, Context.Channel);
        }

        public static async Task DynastyHub(Player player, Dynasty dynasty, DynastyMember membership, ISocketMessageChannel chan)
        {
            membership ??= dynasty.GetMember(player);
            await chan.SendMessageAsync(embed: dynasty.ToEmbed(
                dynasty.MemberField(membership)
                ).Build());
        }

        [Command("Create Dynasty")]
        public async Task CreateDynasty([Remainder]string dynastyName)
        {
            Context.WIPCheck();

            if(await Dynasty.Exist(dynastyName))
            {
                await ReplyAsync($"Dyansty name {dynastyName} is already in use");
                return;
            }

            StringM.RegexName(dynastyName, 5);

            Player player = Context.Player;

            if(player.userid != 201875246091993088 && player.level < 20)
            {
                await ReplyAsync("Character must be minimum level 20 to create a Dynasty.");
                return;
            }

            await Dynasty.Load(player);

            if (player.dynasty != null) await ReplyAsync("You are already in a dynasty");
            else await player.NewUI(await ReplyAsync(
                $"Form the {dynastyName} Dynasty for {Dynasty.DYNASTY_COST} Kutsyei Coins?"),
                MsgType.DynastyUpgrade, dynastyName);
        }


        [Command("Dynasty Invite", true)]
        [Alias("dinv")]
        public async Task InviteToDynasty(IUser user)
        {
            Player player = Context.Player;
            (Dynasty dan, DynastyMember _) = await GetDynasty(player);

            Player invited = Player.Load(user.Id, Player.IgnoreException.All);
            if (invited.level < 20) await ReplyAsync($"{invited.name} must be minimum level 20 to join a Dynasty.");
            else if (invited.dynasty != null) await ReplyAsync($"{invited.name} is already in the {invited.dynasty.dynastyName} Dynasty");
            else await invited.NewUI(await ReplyAsync(
                $"{user.Mention}, your character {invited.name} was invited to be apart of the {dan.name} Dynasty. Join?"),
                MsgType.DynastyInvite, dan._id.ToString());
        }

        [Command("Leave Dynasty", true)]
        [Alias("dleave")]
        public async Task LeaveDynasty()
        {
            Player player = Context.Player;
            (Dynasty dan, DynastyMember _) = await GetDynasty(player);

            await player.NewUI(await ReplyAsync(
                $"{player.name}, areyou sure you wish to leave the {dan.name} Dynasty?"),
                MsgType.DynastyInvite);
        }

        [Command("Dynasty Edit")]
        [Alias("dedit")]
        public async Task DynastyEdit(string field, [Remainder]string value)
        {
            Player player = Context.Player;
            (Dynasty dan, DynastyMember _) = await GetDynasty(player);

            switch (field?.ToLower())
            {
                case "desc":
                case "description":
                    if(value.Length > 240)
                    {
                        await ReplyAsync("Description may not be longer than 240 characters");
                        return;
                    }
                    dan.description = value;
                    await dan.Save();
                    break;
                case "message":
                    if (value.Length > 120)
                    {
                        await ReplyAsync("Description may not be longer than 120 characters");
                        return;
                    }
                    dan.messageOfTheDay = value;
                    await dan.Save();
                    break;
                case "name":
                    if (await Dynasty.Exist(value))
                    {
                        await ReplyAsync($"Dyansty name {value} is already in use");
                        return;
                    }
                    StringM.RegexName(value, 5);
                    dan.name = value;
                    await dan.Save();
                    break;
            }
        }

        [Command("Dynasty User")]
        [Alias("duser")]
        [Summary("View or manage a user's players in this Dynasty")]
        public async Task DynastyUser(IUser user = null)
        {
            user ??= Context.User;
            await DynastyUser(Context.Player, user.Id, Context.Channel, null);
        }

        public static async Task DynastyUser(Player player, ulong user, ISocketMessageChannel chan, string playerId)
        {
            bool listing = playerId == null && user != 0;
            bool manage = false;
            (Dynasty dan, DynastyMember manager) = await GetDynasty(player);
            EmbedBuilder embed = listing ? DynastyUserEmbed(dan, user, out playerId) 
                : DynastyMemberEmbed(dan, manager, playerId, out manage);

            embed.WithColor(player.userSettings.Color());

            await player.EnUI(!listing, null, embed.Build(), chan, 
                manage ? MsgType.DynastyMembership : MsgType.DynastyMember, 
                playerId);
        }

        static EmbedBuilder DynastyUserEmbed(Dynasty dan, ulong user, out string memberCount)
        {
            DynastyMember[] members = dan.members.FindAll(m => m.userId == user).ToArray();
            if (members.Length == 0)
                throw NeitsilliaError.ReplyError("This user has no characters in your Dynasty");

            memberCount = members.Join(";", (m, i) => m.PlayerId);

            return DUtils.BuildEmbed($"{dan.name} Dynasty User",
                $"<@{user}> has {members.Length} characters in this dynasty",
                null, default, DUtils.NewField("Characters",
                    members.Join(Environment.NewLine, (m, i) =>
                        $"{EUI.GetNum(i + 1)} {m.name}")));
        }

        static EmbedBuilder DynastyMemberEmbed(Dynasty dan, DynastyMember manager, string playerId, out bool manage)
        {
            manage = false;
            DynastyMember member = dan.GetMember(playerId);
            if (member == null)
                throw NeitsilliaError.ReplyError($"This character was not found in the {dan.name} Dynasty");
            return DynastyMemberEmbed(dan, manager, member, out manage);
        }

        public static EmbedBuilder DynastyMemberEmbed(Dynasty dan, DynastyMember manager, DynastyMember member, out bool manage)
        {
            manage = false;
            EmbedBuilder embed = member.ToEmbed(dan);

            if (manager != null && manager.rank < member.rank && manager.rank < 4)
            {
                embed.AddField("Manage",
                    $"{EUI.greaterthan} Promote" + Environment.NewLine +
                    $"{EUI.lowerthan} Demote" + Environment.NewLine +
                    $"{EUI.cancel} Kick");
                manage = true;
            }

            return embed;
        }
    }
}
