using AMI.Neitsillia.User.UserInterface;
using Discord;
using System.Threading.Tasks;

namespace AMI.Neitsillia.User.PlayerPartials
{
    partial class Player
    {
        public UI ui;

        public async Task NewUI(IUserMessage argMsg, MsgType argType, string argdata = null)
        {
            if (ui != null) await ui.TryDeleteMessage();

            ui = new UI(argMsg, argType, this, argdata);
            SaveFileMongo();
        }

        public async Task<IUserMessage> NewUI(string content, Embed embed, IMessageChannel chan, MsgType argType, string argdata = null)
        {
            ui = new UI(await chan.SendMessageAsync(content, embed: embed), argType, this, argdata);
            SaveFileMongo();
            return await ui.GetUiMessage();
        }

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

        public void SetUI(UI ui, string data)
        {
            this.ui = ui;
            this.ui.data = data;

            SaveFileMongo();
        }

        public bool IsUI(ulong id) => ui?.msgId == id;

    }
}
