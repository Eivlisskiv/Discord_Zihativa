using AMI.Handlers;
using AMI.Module;
using AMI.Neitsillia.Collections;
using AMI.Neitsillia.User.PlayerPartials;
using AMI.Neitsillia.User.UserInterface;
using AMYPrototype.Commands;
using Discord;
using AMI.Neitsillia.Items.ItemPartials;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AMI.Neitsillia.Social.Mail
{
    public class Mail
    {
        static AMIData.MongoDatabase Database =>
            AMYPrototype.Program.data.database;

        public static async Task<Mail[]> Load(ulong id)
        {
            List<Mail> mails = await Database.LoadRecordsAsync<Mail>(null, $"{{ receiver: {id} }}");
            return mails.ToArray();
        }

        public static async Task<Mail> Load(string id)
        {
            Guid guid = Guid.Parse(id);
            return await Database.LoadRecordAsync(null, AMIData.MongoDatabase.FilterEqual<Mail, Guid>("_id", guid));
        }

        public static async Task ReferenceReward(Player player, ulong referer)
        {
            Mail mail = new Mail(referer, "A player you've referred has leveled up!",
                $"<@{player.userid}> 's character {player.name} has reached level {player.level}. " + Environment.NewLine +
                $"As thanks for helping the community grow with active players, please accept these rewards.", 
                player.level * 100, new StackedItems(Item.RandomGear(player.level * 5, true), 1));
            await mail.Save();
        }

        public static async Task ReferenceSetReward(ulong self)
        {
            Mail mail = new Mail(self, "Reference set!",
                "You've set your reference." + Environment.NewLine +
                "This will grant rewards to the person who refered you" +
                " to this bot every time one of your characters level up." + Environment.NewLine +
                "Invite your friends to play and ask them to use the same command for the same rewards!",
                5000);
            await mail.Save();
        }

        public static async Task NestReward(ulong id, string name, string nest, int level, int score, int position)
        {
            Mail mail = new Mail(id, $"A reward for your services.",
                $"Hunter, the {nest} was cleared thanks to your contribution. Here are some additional rewards.",
               level * score * (position < 10 ? 10 - position : 0))
            { forceCharacter = name };

            if(position < 5)
                mail.AddContent(new StackedItems(Item.RandomGear(level * 5, true), 1));

            await mail.Save();
        }

        public Guid _id;
        public DateTime date;

        public ulong receiver;

        public string subject;
        public string body;

        public string forceCharacter;

        public int kuts;
        public Inventory content;

        public Mail(ulong receiver, string subject, string body, int kuts = 0, params StackedItems[] items)
        {
            _id = Guid.NewGuid();
            date = DateTime.UtcNow;

            this.receiver = receiver;

            this.subject = subject;
            this.body = body;

            this.kuts = kuts;

            if (items != null && items.Length > 0)
                AddContent(items);
        }

        public void AddContent(params StackedItems[] items)
        {
            content ??= new Inventory();
            content.Add(-1, items);
        }

        public EmbedBuilder ToEmbed(bool controls = false, Color? color = null)
        { 
            var embed = DUtils.BuildEmbed(subject, body, null, color ?? default);

            string rewards = GetRewards();
            if (rewards != null) embed.AddField("Rewards", rewards);

            if (controls)
                embed.AddField("Controls", $"{EUI.ok} Collect Content and delete message"
                    + Environment.NewLine);

            return embed;
        }

        public string GetRewards()
        {
            string rewards = kuts > 0 ? $"{kuts} Kutsyei Coins" : null;
            if (content != null && content.Count > 0)
            {
                if (rewards != null) rewards += Environment.NewLine;
                for (int i = 0; i < content.inv.Count; i++)
                {
                    StackedItems item = content.inv[i];
                    rewards += item.ToString() + Environment.NewLine;
                }
            }
            return rewards;
        }

        public async Task Save()
        {
            await Database.SaveRecordAsync(null, this);
            Notify();
        }

        private void Notify()
        {
            Task.Run(async () =>
            {
                var user = DiscordBotHandler.Client.GetUser(receiver);
                if (user != null)
                {
                    var dms = await user.GetOrCreateDMChannelAsync();
                    await dms.SendMessageAsync("I've been looking for you. Got something I'm supposed to deliver - your hands only." +
                        Environment.NewLine + " Use the `mail` command to view your mail.", embed: ToEmbed().Build());
                }
            });
        }

        public async Task Collect(Player player, Discord.WebSocket.ISocketMessageChannel channel)
        {
            if (player != null)
            {
                player.KCoins += kuts;
                if (content != null && content.Count > 0 && !player.inventory.Add(content, -1))
                {
                    await channel.SendMessageAsync("Inventory may not contain all rewards");
                    return;
                }

                player.SaveFileMongo();
                await channel.SendMessageAsync("Mail collected and deleted successfully.");
            }

            await Database.DeleteRecord<Mail, Guid>(null, _id);
        }
    }
}
