using AMI.Neitsillia.NeitsilliaCommands.Social.Dynasty;
using AMYPrototype.Commands;
using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AMI.Neitsillia.User.UserInterface
{
    public partial class UI
    {
        static void InitO_Dynasty()
        {
            OptionsLoad.Add(MsgType.DynastyMembership, ui =>
                ui.options = new List<string>()
                { EUI.greaterthan, EUI.lowerthan, EUI.cancel }
            );

            OptionsLoad.Add(MsgType.DynastyMember, ui => {
                if(ui.data != null)
                {
                    string[] nums = ui.data?.Split(';');
                    if (nums.Length > 0)
                    {
                        ui.options = new List<string>();
                        for (int i = 0; i < nums.Length; i++)
                            ui.options.Add(EUI.GetNum(i + 1));
                    }
                }
            });
        }

        public async Task DynastyUpgrade(SocketReaction reaction, IUserMessage msg)
        {
            switch (reaction.Emote.ToString())
            {
                case EUI.ok:
                    _ = await Dynasty.Load(player);
                    if (player.dynasty == null)
                    {
                        if (Dynasty.Exist(data))
                        {
                            await Channel.SendMessageAsync(
                                $"Dyansty name {data} is already in use");
                            return;
                        }
                        Dynasty dan = await Dynasty.CreateDynasty(player, data);
                        await DynastyCommands.DynastyHub(player, dan, null, Channel);
                    }
                    else
                    {
                        //Upgrade
                    }
                    break;
                case EUI.cancel:
                    await TryMSGDel(msg);
                    break;
            }
        }

        public async Task DynastyInvite(SocketReaction reaction, IUserMessage msg)
        {
            switch (reaction.Emote.ToString())
            {
                case EUI.ok:
                    if (data != null)
                    {
                        Guid id = Guid.Parse(data);
                        Dynasty dan = await Dynasty.Load(id);
                        var mem = await dan.AddMemeber(player);
                        await DynastyCommands.DynastyHub(player, dan, mem, Channel);
                    }
                    else
                    {
                        (Dynasty dan, DynastyMember _, string _) = await Dynasty.Load(player);
                        if(player.dynasty != null)
                        {
                            await dan.RemoveMember(player);
                            await Channel.SendMessageAsync($"You've left the {dan.name} Dynasty");
                        }
                    }
                    break;
                case EUI.cancel:
                    await TryMSGDel(msg);
                    break;
            }
        }

        public async Task DynastyMember(SocketReaction reaction, IUserMessage _)
        {
            int index = EUI.GetNum(reaction.Emote.ToString()) - 1;
            if(index > -1)
            {
                string[] ids = data?.Split(';');
                await DynastyCommands.DynastyUser(player, 0, Channel, ids[index]);
            }
        }

        public async Task DynastyMembership(SocketReaction reaction, IUserMessage msg)
        {
            (Dynasty dan, DynastyMember manager) = await DynastyCommands.GetDynasty(player);
            if(manager.PlayerId == data)
            {
                await Channel.SendMessageAsync("You may not perform these actions on yourself");
                return;
            }
            DynastyMember target = dan.GetMember(data);
            if (manager.rank > 3 || manager.rank > target.rank)
            {
                await Channel.SendMessageAsync("You do not have the authority for this.");
                return;
            }
            switch (reaction.Emote.ToString())
            {
                case EUI.greaterthan:
                    if (target.rank < 2)
                        await Channel.SendMessageAsync("You may not promote this member further");
                    else
                    {
                        target.rank--;
                        await dan.Save();
                        DUtils.DeleteMessage(await Channel.SendMessageAsync(
                                $"{target.name} was promoted to {dan.rankNames[target.rank]}"));
                        _ = msg.ModifyAsync(m => m.Embed = DynastyCommands.DynastyMemberEmbed(
                            dan, manager, target, out _).WithColor(player.userSettings.Color).Build());
                    }
                    break;
                case EUI.lowerthan:
                    if (target.rank > dan.rankNames.Length - 1)
                        await Channel.SendMessageAsync("You may not demote this member further");
                    else
                    {
                        target.rank++;
                        await dan.Save();
                        DUtils.DeleteMessage(await Channel.SendMessageAsync(
                            $"{target.name} was demoted to {dan.rankNames[target.rank]}"));
                        _ = msg.ModifyAsync(m => m.Embed = DynastyCommands.DynastyMemberEmbed(
                           dan, manager, target, out _).WithColor(player.userSettings.Color).Build());
                    }
                    break;
                case EUI.cancel:
                    
                    break;
            }
        }
    }
}
