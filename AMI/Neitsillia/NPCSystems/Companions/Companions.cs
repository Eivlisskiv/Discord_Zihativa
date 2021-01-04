using AMI.Module;
using AMI.Neitsillia.NPCSystems.Companions.Pets;
using AMI.Neitsillia.User.PlayerPartials;
using AMI.Neitsillia.User.UserInterface;
using AMYPrototype.Commands;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;

namespace AMI.Neitsillia.NPCSystems.Companions
{
    public class CompanionCommands : ModuleBase<AMI.Commands.CustomSocketCommandContext>
    {
        [Command("EggPocket"), Alias("epocket")]
        public async Task ViewEggPocket()
        {
            Player player = Context.Player;
            if (player.EggPocket == null)
                await ReplyAsync("You have no egg pocket");
            else await PocketUi(player, Context.Channel);
        }
        internal static async Task PocketUi(Player player, ISocketMessageChannel chan)
        {
            if (player.EggPocket == null) throw NeitsilliaError.ReplyError(player.name + " does not have an egg pocket.");
            EmbedBuilder e = DUtils.BuildEmbed(player.name + "'s Egg Pocket",
                player.EggPocket.GetInfo(player._id));
            await player.NewUI(await chan.SendMessageAsync(embed: e.Build()), MsgType.EggPocket);
        }

        [Command("Pet")][Alias("Pets")]
        public async Task PetCommand(string action = "view", int slot = 0, params string[] arguments)
        {
            Player player = Context.Player;
            slot--;
            switch (action?.ToLower())
            {
                case "view":
                    if (slot < 0 || slot >= player.PetList.Count)
                        await player.PetList.BuildUI(player, Context.Channel);
                    else await player.PetList[slot].GetInfo(player, Context.Channel, slot, false);
                    break;
                case "name":
                case "rename":
                    if (slot < 0 || slot >= player.PetList.Count) throw NeitsilliaError.ReplyError("Please enter a valid slot for the pet you wish to rename: `~pet rename 1 \"Mister Paw\"");
                    if (arguments.Length < 1) throw NeitsilliaError.ReplyError($"Please enter a name for the pet you wish to rename: `~pet rename {slot} \"Mister Paw\"");
                    if (player.PetList[slot].status != Pet.PetStatus.Idle) throw NeitsilliaError.ReplyError($"Pet must be [Idle] to do that");

                    player.PetList[slot].pet.displayName = string.Join(" ", arguments);
                    await player.PetList[slot].GetInfo(player, Context.Channel, slot, false);
                break;

                case "upgrade":
                    if (slot < 0 || slot >= player.PetList.Count) await ReplyAsync("Please enter a valid slot for the pet you wish to upgrade: `~pet upgrade 1`");
                    else if (player.PetList[slot].status != Pet.PetStatus.Idle) await ReplyAsync($"Pet must be [Idle] to do that");
                    else await player.PetList[slot].UpgradeOptionsUI(player, Context.Channel, slot, false);
                break;

                case "evolve":
                    if (slot < 0 || slot >= player.PetList.Count)
                        await ReplyAsync("Please enter a valid slot for the pet you wish to evolve: `~pet upgrade 1`");
                    else if (player.PetList[slot].status != Pet.PetStatus.Idle) await ReplyAsync($"Pet must be [Idle] to do that");
                    else await player.PetList[slot].ViewEvolves(player, Context.Channel, slot, false);
                break;

                default:
                    await ReplyAsync("Invalid action. Please try: " 
                        + Environment.NewLine +  $"{Context.Prefix}pet view"
                        + Environment.NewLine +  $"{Context.Prefix}pet rename"
                        + Environment.NewLine +  $"{Context.Prefix}pet upgrade"
                        + Environment.NewLine +  $"{Context.Prefix}pet evolve"
                        );
                    break;
            }
        }

        internal static async Task ViewPets(Player player, ISocketMessageChannel channel)
        {
            PetList pl = player.PetList;
            await pl.BuildUI(player, channel);
        }
    }
}
