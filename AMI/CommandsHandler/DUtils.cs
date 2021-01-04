using AMI.Methods;
using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AMYPrototype.Commands
{
    public class DUtils
    {
        public static async Task Replydb(SocketCommandContext
            Context, string text = null, Embed embed = null, double lifetime = 6)
        {
            DeleteMessage(await Context.Channel.SendMessageAsync(text, embed: embed), lifetime);
            await DeleteContextMessageAsync(Context);
        }
        public static async Task<RestUserMessage> Replydc(SocketCommandContext
           Context, string text = null, Embed embed = null)
        {
            await DeleteContextMessageAsync(Context);
            return await Context.Channel.SendMessageAsync(text, embed: embed);
        }
        public static async Task DeleteBothMsg(SocketCommandContext Context, 
            RestUserMessage botReply)
        {
            await DeleteContextMessageAsync(Context);
            DeleteMessage(botReply);
        }

        internal static Color RandomColor()
        {
            Random r = Program.rng;
            return new Color(r.Next(256), r.Next(256), r.Next(256));
        }

        public static async Task DeleteBothMsg(SocketCommandContext Context,
           IUserMessage botReply)
        {
            await DeleteContextMessageAsync(Context);
            DeleteMessage(botReply);
        }
        public static async Task DeleteContextMessageAsync(
            SocketCommandContext Context)
        {
            try { await Context.Message.DeleteAsync(); }
            catch (Exception) { }
        }
        public static void DeleteMessage(RestUserMessage botReply,
            double minutes = 1)
        {
            new Task(async () =>
            {
                await Task.Delay(Convert.ToInt32(minutes * 60000));
                try
                { await botReply.DeleteAsync(); }
                catch (Exception) { }

            }).Start();
        }

        public static void DeleteMessage(IUserMessage botReply,
            double minutes = 1)
        {
            new Task(async () =>
            {
                await Task.Delay(Convert.ToInt32(minutes * 60000));
                try
                { await botReply.DeleteAsync(); }
                catch (Exception) {  }
            }).Start();
        }

        public static async Task SendToDms(IUser user, string content = null, EmbedBuilder em = null, ISocketMessageChannel backup = null)
        {
            try
            {
                await user.SendMessageAsync(content, false, em?.Build());
            }
            catch(Exception e)
            {
                Log.LogS("Failed to DM " + user.Id);
                Log.LogS(e);
                backup?.SendMessageAsync(content, false, em?.Build());
            }
        }

        public static EmbedBuilder BuildEmbed(string title, string desc = null, string footer = null,
            Color color = default, params EmbedFieldBuilder[] fields)
        {
            EmbedBuilder embed = new EmbedBuilder();
            embed.WithTitle(title);

            if(desc != null && desc.Length > 0)
                embed.WithDescription(desc);

            embed.WithColor(color);

            if (footer != null && footer.Length > 0)
                embed.WithFooter(footer);

            embed.WithFields(fields);

            return embed;
        }

        public static EmbedBuilder BuildEmbed(string title, string desc = null, string footer = null,
    Color color = default,  IEnumerable<EmbedField> fields = null)
        {
            EmbedBuilder embed = new EmbedBuilder();
            embed.WithTitle(title);

            if (desc != null && desc.Length > 0)
                embed.WithDescription(desc);

            embed.WithColor(color);

            if (footer != null && footer.Length > 0)
                embed.WithFooter(footer);

            if(fields != null)
                foreach (var f in fields)
                    embed.AddField(NewField(f.Name, f.Value, f.Inline));

            return embed;
        }

        public static EmbedFieldBuilder NewField(string title, string content, bool inline = false) => new EmbedFieldBuilder()
        {
            Name = title,
            Value = content,
            IsInline = inline,
        };

        internal static EmbedBuilder GetMessageEmbed(IUserMessage msg, bool withFields)
        {
            IEmbed old = Enumerable.First(msg.Embeds);
            return BuildEmbed(old.Title, old.Description,
                old.Footer == null ? null : ((EmbedFooter)old.Footer).Text,
                old.Color ?? new Color(), withFields ? old.Fields : old.Fields.Clear());
        }
    }
}

