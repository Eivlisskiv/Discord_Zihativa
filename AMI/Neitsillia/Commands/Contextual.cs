using AMI.Commands;
using AMI.Neitsillia.Areas.Sandbox;
using AMI.Neitsillia.Gambling.Games;
using AMI.Neitsillia.User.PlayerPartials;
using AMI.Neitsillia.User.UserInterface;
using Discord.Commands;
using System.Threading.Tasks;

namespace AMI.Neitsillia.Commands
{
    public class Contextual : ModuleBase<CustomCommandContext>
    {
        [Command("amount")]
        public async Task SetContextAmount(int amount)
        {
            Player player = Context.Player;

            switch (player.ui?.type)
            {
                case MsgType.TileProduce:
                    await TileProduce(player, amount);
                    return;
                case MsgType.GameBet:
                    await CardBet(player, amount);
                    return;
            }

            await ReplyAsync($"There is no amount to set");
        }

        private async Task TileProduce(Player player, int amount)
        {
            UI ui = player.ui;
            (string source, Sandbox sb, int tileIndex, int productIndex, _) = await ui.GetTileProcudeData(player);

            await SandboxActions.ProduceAmount(player, sb,
                    source, tileIndex, productIndex, amount, Context.Channel);
        }

        private async Task CardBet(Player player, int amount)
        {
            string[] d = player.ui.data.Split(';');
            if (d.Length < 2) return;

            await GamblingGame.SelectInitialBet(player, Context.Channel, d[0], amount);
        }
    }
}
