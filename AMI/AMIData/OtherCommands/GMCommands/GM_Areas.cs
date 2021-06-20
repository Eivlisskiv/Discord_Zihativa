using AMI.Methods;
using AMI.Neitsillia.Areas;
using AMI.Neitsillia.Areas.AreaPartials;
using AMI.Neitsillia.User.PlayerPartials;
using AMI.Neitsillia.User.UserInterface;
using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AMI.AMIData.OtherCommands
{
    public partial class GameMaster
    {
        [Command("Move To Area")]
        [Alias("mtarea")]
        public async Task MoveToArea(IUser user, [Remainder] string name)
        {
            if (await IsGMLevel(4))
            {
                Player player = Player.Load(user.Id);
                Area area = Area.LoadFromName(StringM.UpperAt(name));
                player.EndEncounter();
                await player.SetArea(area);
                player.SaveFileMongo();
                EmbedBuilder areaInfo = player.UserEmbedColor(player.Area.AreaInfo(player.AreaInfo.floor));
                await player.NewUI(await Context.Channel.SendMessageAsync("You've entered " + player.Area.name, embed: areaInfo.Build())
                , MsgType.Main);
            }
        }

        [Command("Generate Dungeon")]
        [Alias("GenDungeon")]
        public async Task GenerateDungeon(IUser user, [Remainder] string name)
        {
            if (await IsGMLevel(3))
            {
                Player player = Player.Load(user.Id);
                Area dungeon;
                if (name.Length > 0)
                    dungeon = Dungeons.ManualDungeon(StringM.UpperAt(name), player.AreaInfo.floor, player.Area);
                else dungeon = Dungeons.Generate(player.AreaInfo.floor, player.Area);
                if (dungeon != null)
                {
                    await player.SetArea(dungeon);
                    await player.NewUI(await ReplyAsync(embed: dungeon.AreaInfo(player.AreaInfo.floor).Build()), MsgType.Main);
                }
                else await ReplyAsync("Dungeon not Found");
            }
        }

        [Command("AutoInvade")]
        public async Task AutoInvade(IUser user = null)
        {
            if (await IsGMLevel(4))
            {
                if (user == null)
                    user = Context.User;
                Player player = Player.Load(user.Id);
                if (player.Area.type == AreaType.Stronghold)
                {
                    player.Area.sandbox.Captured(user.Id);
                    await player.Area.UploadToDatabase();
                    await ReplyAsync("Completed");
                }
                else await ReplyAsync("Area is not a Stronghold");
            }
        }

        [Command("AddChildArea")]
        public async Task AddChildArea_CMD(string childType, string childName, string arguments = null)
        {
            if (await IsGMLevel(4))
                await ReplyAsync(AddChildArea(childType, childName, arguments));
        }

        string AddChildArea(string childType, string childName, string arguments)
        {
            Area parent = Player.Load(Context.BotUser, Player.IgnoreException.All).Area;
            Area child;
            switch (childType.ToLower())
            {
                case "tavern": child = ChildrenArea.Tavern(parent, childName, true); break;
                case "arena": child = ChildrenArea.Arena(parent, childName, arguments, true); break;
                case "petshop": child = ChildrenArea.PetShop(parent, childName, true); break;
                case "shrine": child = ChildrenArea.Shrine(parent, childName, true); break;
                default:
                    return "Child Area Type Invalid" + Environment.NewLine
                        + "Tavern" + Environment.NewLine
                        + "Arena" + Environment.NewLine
                        + "PetShop" + Environment.NewLine
                        + "Shrine" + Environment.NewLine
                        ;

            }
            return $"Added {child} {child.type} in {parent}";
        }

        [Command("Add Junction")]
        public async Task AddJunction(string fromId, string toId, int floor = 0, int returnFloor = 0)
        {
            if (await IsGMLevel(4))
            {
                Area from = Area.LoadFromName(fromId);
                Area to = Area.LoadFromName(toId);

                from.junctions = from.junctions ?? new List<NeitsilliaEngine.Junction>();
                int index = from.junctions.FindIndex(NeitsilliaEngine.Junction.FindName(to.name));
                if (index == -1)
                    from.junctions.Add(new NeitsilliaEngine.Junction(to, floor, returnFloor));
                else
                {
                    from.junctions[index].floorRequirement = floor;
                    from.junctions[index].returnfloor = returnFloor;
                }
                await from.UploadToDatabase();
                await ReplyAsync("Area Junction Added");
            }
        }

        [Command("SpawnNest")]
        public async Task InitSpawnNest()
        {
            if (!await IsGMLevel(4)) return;
            await Neitsillia.Areas.Nests.Nest.SpawnNest();
        }

        [Command("VerifyNests")]
        public async Task VerifyExistingNests()
        {
            if (!await IsGMLevel(4)) return;

            await Neitsillia.Areas.Nests.Nest.VerifyNests();
        }
    }
}
