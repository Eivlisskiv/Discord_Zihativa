using AMI.Module;
using AMI.Neitsillia.Areas.Arenas;
using AMI.Neitsillia.Areas.InteractiveAreas;
using AMI.Neitsillia.Items.Quests;
using AMI.Neitsillia.NeitsilliaCommands;
using AMYPrototype.Commands;
using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AMI.Neitsillia.User.UserInterface
{
    partial class UI
    {
        static void InitO_Area()
        {
            OptionsLoad.Add(MsgType.ArenaService, ui =>
                ui.options = new List<string>()
                { EUI.sideQuest, EUI.bounties }
            );

            OptionsLoad.Add(MsgType.ArenaFights, ui =>
            {
                if (int.TryParse(ui.data, out int l))
                {
                    ui.options = new List<string>()
                        { EUI.ok, EUI.uturn };
                }
                else if (int.TryParse(ui.data.TrimStart('#'), out l))
                {
                    ui.options = new List<string>();
                    for (int i = 1; i <= l; i++)
                        ui.options.Add(EUI.GetNum(i));
                }
                
            });
        }

        public async Task PetShop(SocketReaction reaction, IUserMessage msg)
        {
            switch (reaction.Emote.ToString())
            {
                case EUI.mainQuest:
                    {
                        await Quest.AvailableQuestList(player, reaction.Channel, data);

                    } break;
            }
        }

        public async Task Tavern(SocketReaction reaction, IUserMessage msg)
        {
            if (EUI.Dice(reaction.Emote.ToString()) == 1)
            {
                await GamblingCommands.TavernGames(player, reaction.Channel);
                return;
            }
            switch (reaction.Emote.ToString())
            {
                case EUI.sideQuest:
                    {
                        var qt = User.DailyQuestBoard.Load(player._id);
                        await qt.ShowBoard(player, reaction.Channel);
                    }
                    break;
                case EUI.bounties:
                    {
                        Areas.AreaPartials.Area tavern = player.Area;
                        if (tavern.parent != null)
                            tavern = Areas.AreaPartials.Area.LoadArea(tavern.GeneratePath(false) + tavern.parent);
                        await TavernInteractive.GenerateBountyFile(player, tavern, -1, reaction.Channel);
                    }
                    break;
            }
        }

        public async Task Adventure(SocketReaction reaction, IUserMessage msg)
        {
            string e = reaction.Emote.ToString();
            int i = EUI.GetNum(e);
            if(i > -1)
            {
                if (data[0] == 'D') //Select difficulty
                {
                    await Adventures.Adventure.StartAdventure(player, Channel, 
                        Adventures.Adventure.AdventureType.FreeRoam,
                        ((Adventures.Adventure.Intensity[])Enum.GetValues(typeof(Adventures.Adventure.Intensity)))[i - 1], 
                        null);
                }
                else //Select a quest
                {

                }
                return;
            }
            switch(e)
            {
                case EUI.ok:
                    await Adventures.Adventure.SelectIntensity(player, reaction.Channel);
                    break;
                case EUI.cancel:
                    if (player.IsInAdventure) await player.Adventure.End(player, reaction.Channel);
                    else await GameCommands.StatsDisplay(player, reaction.Channel);
                    break;
            }
        }

        public async Task ArenaService(SocketReaction reaction, IUserMessage msg)
        {
            switch (reaction.Emote.ToString())
            {
                case EUI.sideQuest:
                    var data = await ArenaGlobalData.Load(player.AreaInfo.path);
                    await data.DiscordUI(player, reaction.Channel);
                    break;

                case EUI.bounties:
                    await Arena.SelectMode(player, 0, reaction.Channel, true);
                    break;
            }
        }

        public async Task ArenaFights(SocketReaction reaction, IUserMessage msg)
        {
            string e = reaction.Emote.ToString();
            var data = await ArenaGlobalData.Load(player.AreaInfo.path);
            //is not a number, is paying
            switch (e)
            {
                case EUI.ok:
                    await data.StartFight(int.Parse(this.data), player, reaction.Channel);
                    break;
                case EUI.uturn:
                    await data.DiscordUI(player, reaction.Channel);
                    break;
                default:
                    int i = EUI.GetNum(e) - 1;
                    if (i < 0) return;
                    await data.DiscordUI(i, player, reaction.Channel);
                    break;
            }
        }

        public async Task ArenaGameMode(SocketReaction reaction, IUserMessage msg)
        {
            string[] args1 = data.Split(';');
            int page = int.Parse(args1[0]);
            switch (reaction.Emote.ToString())
            {
                case EUI.prev:
                    await Arena.SelectMode(player, page - 1, reaction.Channel, true);
                    break;
                case EUI.next:
                    await Arena.SelectMode(player, page + 1, reaction.Channel, true);
                    break;
                case EUI.cancel:
                    await msg.DeleteAsync();
                    break;
                case EUI.ok:
                    {
                        int mods = Enum.GetValues(typeof(ArenaModifier.ArenaModifiers)).Length;
                        if (mods > 0)
                            await Arena.SelectModifiers(player,
                                    page.ToString(),
                                    0,
                                    string.Join(",", new int[mods]),
                                reaction.Channel, true);
                        else
                        {
                            await Arena.Generate(player.Area, player, page, null);
                            await EditMessage(null, DUtils.BuildEmbed($"Entered {player.Area.name}", 
                                "Complete your challenge to gain rewards." + Environment.NewLine +
                                "**Warning:** Running away, dying or leaving the area will end the challenge" + Environment.NewLine +
                                "Use the `Explore` command to continue", null, player.userSettings.Color()).Build(), reaction.Channel);
                        }
                    }
                    break;
            }
        }
        public async Task ArenaModifiers(SocketReaction reaction, IUserMessage msg)
        {
            //data format: mode;page;isActive,IsActive...
            string[] args = data.Split(';');
            int page = int.Parse(args[1]);
            switch (reaction.Emote.ToString())
            {
                case EUI.uturn:
                    await Arena.SelectMode(player, 0, reaction.Channel, true);
                    break;
                case EUI.prev:
                    await Arena.SelectModifiers(player, args[0], page - 1, args[2], reaction.Channel, true);
                    break;
                case EUI.next:
                    await Arena.SelectModifiers(player, args[0], page + 1, args[2], reaction.Channel, true);
                    break;

                case EUI.ok:
                    {
                        await RemoveReaction(reaction.Emote, msg);
                        await msg.AddReactionAsync(new Emoji(EUI.cancel));
                        options.Add(EUI.cancel);

                        string[] boolArray = args[2].Split(',');

                        boolArray[page] = "1";

                        data = $"{args[0]};{args[1]};{string.Join(",", boolArray)}";
                        player.SaveFileMongo();
                    }
                    break;
                case EUI.cancel:
                    {
                        await RemoveReaction(reaction.Emote, msg);
                        await msg.AddReactionAsync(new Emoji(EUI.ok));
                        options.Add(EUI.ok);

                        string[] boolArray = args[2].Split(',');

                        boolArray[page] = "0";

                        data = $"{args[0]};{args[1]};{string.Join(",", boolArray)}";
                        player.SaveFileMongo();
                    }
                    break;

                case EUI.enterFloor:
                    {
                        string[] boolArray = args[2].Split(',');
                        await Arena.Generate(player.Area, player, page, boolArray);
                        await msg.Channel.SendMessageAsync(embed: player.Area.AreaInfo(player.AreaInfo.floor).Build());
                    }
                    break;
            }
        }
    }
}
