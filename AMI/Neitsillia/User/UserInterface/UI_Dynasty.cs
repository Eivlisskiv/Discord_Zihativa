using AMI.Neitsillia.NeitsilliaCommands.Social.Dynasty;
using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
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
        }

        public async Task DynastyUpgrade(SocketReaction reaction, IUserMessage msg)
        {
            switch (reaction.Emote.ToString())
            {
                case EUI.ok:
                    (Dynasty dan, DynastyMember _, string _) = await Dynasty.Load(player);
                    if (player.dynasty == null)
                    {
                        if (await Dynasty.Exist(data))
                        {
                            await reaction.Channel.SendMessageAsync(
                                $"Dyansty name {data} is already in use");
                            return;
                        }
                        dan = await Dynasty.CreateDynasty(player, data);
                        await DynastyCommands.DynastyHub(player, dan, null, reaction.Channel);
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
                        await DynastyCommands.DynastyHub(player, dan, mem, reaction.Channel);
                    }
                    else
                    {
                        (Dynasty dan, DynastyMember _, string _) = await Dynasty.Load(player);
                        if(player.dynasty != null)
                        {
                            await dan.RemoveMember(player);
                            await reaction.Channel.SendMessageAsync($"You've left the {dan.name} Dynasty");
                        }
                    }
                    break;
                case EUI.cancel:
                    await TryMSGDel(msg);
                    break;
            }
        }

        public async Task DynastyMembership(SocketReaction reaction, IUserMessage msg)
        {
            (Dynasty dan, DynastyMember manager) = await DynastyCommands.GetDynasty(player);
            if(manager.PlayerId == data)
            {
                await reaction.Channel.SendMessageAsync("You may not perform these actions on yourself");
                return;
            }
            DynastyMember target = dan.GetMember(data);
            switch (reaction.Emote.ToString())
            {
                case EUI.greaterthan:
                    if (target.rank < 2)
                        await reaction.Channel.SendMessageAsync("You may not promote this member further");
                    else
                    {
                        target.rank--;
                        await dan.Save();
                        await reaction.Channel.SendMessageAsync($"{target.name} was promoted to {dan.rankNames[target.rank]}");
                        //await DynastyCommands.DynastyUser(player, reaction.Channel, dan, manager, target);
                    }
                    break;
                case EUI.lowerthan:
                    if (target.rank > dan.rankNames.Length - 1)
                        await reaction.Channel.SendMessageAsync("You may not demote this member further");
                    else
                    {
                        target.rank++;
                        await dan.Save();
                        await reaction.Channel.SendMessageAsync($"{target.name} was demoted to {dan.rankNames[target.rank]}");
                    }
                    break;
                case EUI.cancel:
                    
                    break;
            }
        }
    }
}
