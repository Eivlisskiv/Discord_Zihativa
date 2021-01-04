using AMI.Methods;
using AMYPrototype;
using AMYPrototype.Commands;
using Discord;
using Discord.WebSocket;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AMI.AMIData
{
    class GifEmote
    {
        private static MongoDatabase Database => Program.data.database;

        internal static string GetAll()
        {
            List<GifEmote> all = Database.LoadRecords<GifEmote>(null);

            string n = null;

            foreach(var e in all)
            {
                n += e.name + ", ";
            }

            return n.TrimEnd(' ',',');
        }

        internal static GifEmote Load(string name)
        {
            return Database.LoadRecord(null, MongoDatabase.FilterEqual<GifEmote, string>("name", name));
        }

        [MongoDB.Bson.Serialization.Attributes.BsonId]
        public string name;

        public List<string> gifs;

        public List<string> messages;
        public List<string> singleMessages;

        private string defaultMessage => "{0} " + name + " {1}";
        private string defaultSingleMessage => "{0} " + name;

        internal bool isNew = false;

        public GifEmote(string title, string initGif, string initMessage = null)
        {
            name = title;
            if (!IsGif(initGif)) throw Module.NeitsilliaError.ReplyError($"`{initGif}` must be a gif");
            gifs = new List<string>() { initGif };

            messages = new List<string>();
            singleMessages = new List<string>();
            if (initMessage != null) messages.Add(initMessage);

            Save();
            isNew = true;
        }

        internal void Save()
        {
            Database.UpdateRecord(null, MongoDatabase.FilterEqual<GifEmote, string>("_id", this.name), this);
        }

        internal async Task<IUserMessage> Send(ISocketMessageChannel chan, params string[] args)
        {
            var r = Program.rng;
            var color = DUtils.RandomColor();
            return await chan.SendMessageAsync(
                embed: DUtils.BuildEmbed(
                    string.Format(getMessage(args), args),
                    color: color
                    ).WithImageUrl(Utils.RandomElement(gifs)).Build()
                );
        }
        
        public string getMessage(string[] args)
        {
            bool single = args[1] == null;
            return single ?
                (singleMessages?.Count > 0 ? Utils.RandomElement(singleMessages) : defaultSingleMessage)
                : (messages?.Count > 0 ? Utils.RandomElement(messages) : defaultMessage);
        }

        private string Add(string value, List<string> list)
        {
            if (list.Contains(value)) return "already in the list";

            list.Add(value);
            Save();

            return "added";
        }

        internal string AddGif(string url) => "URL " + Add(url, gifs);
        internal string AddMessage(string msg) => "Message " + Add(msg, msg.Contains("{1}") ? messages : (singleMessages = singleMessages ?? new List<string>()));

        internal bool IsGif(string s)
        {
            return s.EndsWith(".gif");
        }
    }
}
