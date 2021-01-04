using AMI.Methods;
using AMI.Neitsillia.Commands;
using AMI.Neitsillia.NeitsilliaCommands;
using AMI.Neitsillia.User;
using AMI.Neitsillia.User.PlayerPartials;
using AMI.Neitsillia.User.UserInterface;
using Discord;
using System;
using System.Threading.Tasks;

namespace AMI.Module
{
    class NeitsilliaError : Exception
    {
        public enum NeitsilliaErrorType
        {
            CharacterIsNotSetUp,
            CharacterAdventuring,
            CharacterDoesNotExist,
            CharacterIsResting,

            ReplyError,
            ReplyUI,
        };

        public static bool operator !(NeitsilliaError e) => e != null; 

        internal string ExtraMessage;
        internal NeitsilliaErrorType ErrorType;

        internal MsgType? uitype;
        internal Player player;

        public NeitsilliaError(string msg, NeitsilliaErrorType type, MsgType? ui = null, Player player = null)
        {
            ExtraMessage = msg;
            ErrorType = type;
            uitype = ui;
            this.player = player;
        }

        internal static NeitsilliaError ReplyError(string msg) => new NeitsilliaError(msg, NeitsilliaErrorType.ReplyError);
        internal static NeitsilliaError CharacterDoesNotExist() => new NeitsilliaError(null, NeitsilliaErrorType.CharacterDoesNotExist);

        internal static NeitsilliaError ReplyUI(string msg, MsgType type, Player player) => new NeitsilliaError(msg, NeitsilliaErrorType.ReplyUI, type, player);
        internal static NeitsilliaError CharacterIsNotSetUp(Player player) => new NeitsilliaError(null, NeitsilliaErrorType.CharacterIsNotSetUp, null, player);
        internal static NeitsilliaError CharacterAdventuring(Player player) => new NeitsilliaError(null, NeitsilliaErrorType.CharacterAdventuring, null, player);
        internal static NeitsilliaError CharacterIsResting(Player player) => new NeitsilliaError(null, NeitsilliaErrorType.CharacterIsResting, null, player);

        internal static NeitsilliaError Is(Exception exception, NeitsilliaErrorType type)
        {
            if (exception is NeitsilliaError error && error.ErrorType == type)
                return error;
            return null;
        }

        internal static bool Is(Exception exception, NeitsilliaErrorType type, out NeitsilliaError error)
        {
            error = null;
            return (exception is NeitsilliaError e && (error = e).ErrorType == type);
        }

        internal static async Task<bool> SpecialExceptions(Exception exception, IMessageChannel chan, BotUser user)
        {
            if (exception is NeitsilliaError error)
                return await SpecialExceptions(error, chan, error.player ?? Player.Load(user, Player.IgnoreException.All));
            return false;
        }

        internal static async Task<bool> SpecialExceptions(Exception exception, IMessageChannel chan, Player player)
        {
            if (exception is NeitsilliaError error)
                return await SpecialExceptions(error, chan, player);
            return false;
        }

        internal static async Task<bool> SpecialExceptions(NeitsilliaError error, IMessageChannel chan, Player player)
        {
            switch (error.ErrorType)
            {
                case NeitsilliaErrorType.ReplyError:
                    await chan.SendMessageAsync(error.ExtraMessage);
                    return true;
                case NeitsilliaErrorType.ReplyUI:
                    {
                        await chan.SendMessageAsync(error.ExtraMessage);
                        switch (error.uitype)
                        {
                            case MsgType.CardGame:
                                {
                                    Type type = Neitsillia.Gambling.Games.GamblingGame.GetGameType(player.ui.data);
                                    Neitsillia.Gambling.Games.IGamblingGame game = Neitsillia.Gambling.Games.GamblingGame.CreateInstance(type, player);
                                    var embed = ((Neitsillia.Gambling.Games.GamblingGame)game).GetEmbed(player).Build();
                                    await player.NewUI(null, embed, await player.DMChannel(), player.ui.type, player.ui.data);
                                }
                                break;
                        }
                        return true;
                    }
                case NeitsilliaErrorType.CharacterIsNotSetUp:
                    if (player != null)
                    {
                        if (player.ui.type == MsgType.AutoNewCharacter)
                            await CharacterCommands.AutoCharacter(player, chan, true);
                        else if (player.ui.type == MsgType.SetSkill
                        || player.ui.type == MsgType.ConfirmSkills)
                        {
                            string[] arrays = player.ui.data.Split(';');
                            await CharacterCommands.SetSkills(player, chan, 0, Utils.JSON<int[]>(arrays[0]), Utils.JSON<bool[]>(arrays[1]));
                        }
                        else if (!await CharacterCommands.Set_Race(chan, player))
                            await CharacterCommands.StarterAbilities(player, chan, 1);
                        return true;
                    }
                    return false;
                case NeitsilliaErrorType.CharacterAdventuring:
                    if (player != null)
                    {
                        await Areas.AdventureStat(player, chan);
                        return true;
                    }
                    return false;
                case NeitsilliaErrorType.CharacterIsResting:
                    if (player != null)
                    {
                        await Areas.RestStat(player, chan);
                        return true;
                    }
                    return false;
                default: return false;
            }
        }
        
        public static string GetType(Exception e)
        {
            return e is NeitsilliaError error ? error.ErrorType.ToString() : e.GetType().ToString();
        }
    }
}
