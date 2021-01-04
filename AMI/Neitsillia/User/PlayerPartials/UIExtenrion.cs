using AMI.Neitsillia.User.UserInterface;
using Discord;
using System.Threading.Tasks;

namespace AMI.Neitsillia.User.PlayerPartials
{
    partial class Player
    {
        public UI ui;

        /// <summary>
        /// Create a new ui object from an existing message
        /// </summary>
        /// <param name="argMsg">the message</param>
        /// <param name="argType">the enum type determining the actions available</param>
        /// <param name="argdata">the additional data to parse</param>
        /// <returns></returns>
        public async Task NewUI(IUserMessage argMsg, MsgType argType, string argdata = null)
        {
            if (ui != null) await ui.TryDeleteMessage();

            ui = new UI(argMsg, argType, this, argdata);
            SaveFileMongo();
        }

        /// <summary>
        /// Create a new ui from message content
        /// </summary>
        /// <param name="content">Message text</param>
        /// <param name="embed">Message embed</param>
        /// <param name="chan">Channel in which to send the message</param>
        /// <param name="argType">the enum type determining the actions available</param>
        /// <param name="argdata">the additional data to parse</param>
        /// <returns></returns>
        public async Task<IUserMessage> NewUI(string content, Embed embed, IMessageChannel chan, MsgType argType, string argdata = null)
        {
            ui = new UI(await chan.SendMessageAsync(content, embed: embed), argType, this, argdata);
            SaveFileMongo();
            return await ui.GetUiMessage();
        }

        /// <summary>
        /// Edit an existing message ui with the given content or create a new one if ui is null
        /// </summary>
        /// <param name="content"></param>
        /// <param name="embed"></param>
        /// <param name="chan"></param>
        /// <param name="argType"></param>
        /// <param name="argdata"></param>
        /// <returns></returns>
        public async Task<IUserMessage> EditUI(string content, Embed embed,
            IMessageChannel chan,
            MsgType argType, string argdata = null)
        {
            if (ui == null) ui = new UI(await chan.SendMessageAsync(content, embed: embed), argType, this, argdata);

            else await ui.Edit(this, content, embed, argType, argdata, !IsKeepReactions(ui, argType, argdata));

            SaveFileMongo();

            return await ui.GetUiMessage();
        }

        public async Task<IUserMessage> EnUI(bool edit, string content, Embed embed,
            IMessageChannel chan, MsgType argType, string argdata = null)
            => await (edit ? EditUI(content, embed, chan, argType, argdata) : NewUI(content, embed, chan, argType, argdata));

        private bool IsKeepReactions(UI old, MsgType t, string d)
        {
            if (old.type != t) return false;
            switch (old.type)
            {
                case MsgType.Combat:
                case MsgType.NPCInv:
                case MsgType.DailyQuestBoard:
                case MsgType.Inventory:
                case MsgType.Loot:
                    return true; //All trues

                case MsgType.Main:
                    return false; //All falses

                case MsgType.PetUpgrade:
                    return old.data == d; //All when data is equal

                case MsgType.CardGame:
                    return old.data == d || old.data?.Split(';')[0] == d?.Split(';')[0];


                default: return false;
            }
        }

        public void SetUI(IUserMessage msg, MsgType type, string data)
        {
            ui = new UI(msg, type, this, data, false);
            SaveFileMongo();
        }

        public bool IsUI(ulong id) => ui?.msgId == id;

    }
}
