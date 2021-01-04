using System;
using System.Threading.Tasks;
using System.IO;
using AMI.Methods;
using AMI.Module;
using System.Collections.Generic;
using AMI.Neitsillia.NeitsilliaCommands;
using AMI.AMIData;
using Discord.Rest;
using AMI.AMIData.Events;
using Discord.WebSocket;
using Discord;
using AMYPrototype.Commands;
using System.Linq;
using AMI.AMIData.Servers;
using AMI.Neitsillia.User.UserInterface;

namespace AMYPrototype
{
    public class ProgramData
    {
        internal List<GM> gms;

        internal AvailablePerks AvailablePerks;
        internal Lottery lottery;
        internal PlayerActivity activity;
        private bool InitiatedEvents = false;

        internal MongoDatabase database;

        //Suggestions
        internal ISocketMessageChannel sendSuggestions;
        internal ISocketMessageChannel sendbugreports;
        internal ISocketMessageChannel BugReportChannel
        {
            get
            {
                return sendbugreports ?? (sendbugreports = (ISocketMessageChannel)Program.clientCopy.GetGuild(201877884313403392)
                .GetChannel((ulong)(Program.isDev ? 741896956296691802 : 741897200317104149)));
            }
        }

        public ProgramData()
        {
            database = new MongoDatabase("ZihativaDatabase" + (Program.isDev ? "_Dev" : "_Live"));
        }

        #region Suggestion Settings
        SocketGuildChannel LoadSuggestionSettings()
        {
            var guild = Program.clientCopy.GetGuild(201877884313403392);
            var gset = GuildSettings.Load(guild);
            return guild.GetChannel(gset.suggestionChannel.id);
        }

        internal async Task<(string, Embed)> SendSuggestion(string content, IUser author)
        {
            sendSuggestions = sendSuggestions ?? (ISocketMessageChannel)LoadSuggestionSettings();
            if (sendSuggestions == null)
                throw NeitsilliaError.ReplyError("Bot suggestions are currently deactivated.");

            EmbedBuilder embed = DUtils.BuildEmbed(null,
                content, author.Id.ToString(), new Color(Program.rng.Next(256),
                Program.rng.Next(256), Program.rng.Next(256)),
                DUtils.NewField("Status", "Pending"));

            embed.WithAuthor(author);

            Embed e = embed.Build();

            var reply = await sendSuggestions.SendMessageAsync(embed: e);

            await reply.ModifyAsync(x =>
            {
                x.Content = $"Use `suggest` in my DMs or in the support server to make a suggestion to the support server."
                + Environment.NewLine + $"Id: `{reply.Id}`";
            });

            await reply.AddReactionsAsync(new[] { EUI.ToEmote(EUI.ok),
                EUI.ToEmote(EUI.cancel) });

            return (reply.GetJumpUrl(), e);
        }

        internal async Task<string> UpdateSuggestion(GuildSettings gset, ulong id, string status)
        {
            var suggestionChannel = gset.GetChannel(GuildSettings.AssignedChannels.Suggestion);
            if(suggestionChannel == null)
                throw NeitsilliaError.ReplyError("Suggestions are currently deactivated.");
            IUserMessage msg = (IUserMessage)await suggestionChannel.GetMessageAsync(id);
            if(msg == null)
                throw NeitsilliaError.ReplyError("Message not found.");
            else if(msg.Author.Id != Program.clientCopy.CurrentUser.Id)
                throw NeitsilliaError.ReplyError("Message not sent by this bot.");

            IEmbed[] embeds = Enumerable.ToArray(msg.Embeds);
            if(embeds.Length < 1)
            {
                await msg.DeleteAsync();
                throw NeitsilliaError.ReplyError("Message had no embeds and was deleted.");
            }
            EmbedAuthor a = (EmbedAuthor)embeds[0].Author;
            string footer = ((EmbedFooter)embeds[0].Footer).Text;
            EmbedBuilder embed = DUtils.BuildEmbed(null,
                    embeds[0].Description, footer, embeds[0].Color ?? new Color(),
                    DUtils.NewField("Reply", status));

            embed.WithAuthor(a.Name, a.IconUrl, a.Url);

            await msg.ModifyAsync(x =>
            {
                x.Embed = embed.Build();
            });

            if (ulong.TryParse(footer, out ulong userId))
                Program.clientCopy.GetUser(userId)?.SendMessageAsync("Your suggestion has a new response.", embed:embed.Build());

            return msg.GetJumpUrl();
        }

        //Bug Report

        internal async Task<(string, Embed)> SendBugReport(string content, IUser author)
        {
            if (BugReportChannel == null)
                throw NeitsilliaError.ReplyError("Bot bug report are currently deactivated.");

            EmbedBuilder embed = DUtils.BuildEmbed(null,
                content, author.Id.ToString(), new Color(Program.rng.Next(256),
                Program.rng.Next(256), Program.rng.Next(256)),
                DUtils.NewField("Status", "Pending"));

            embed.WithAuthor(author);

            Embed e = embed.Build();

            var reply = await BugReportChannel.SendMessageAsync(embed: e);

            await reply.ModifyAsync(x =>
            {
                x.Content = $"Use `BugReport` to report the a bug to the support server."
                + Environment.NewLine + $"Id: `{reply.Id}`";
            });

            await reply.AddReactionsAsync(new[] { EUI.ToEmote(EUI.ok), EUI.ToEmote(EUI.cancel) });

            return (reply.GetJumpUrl(), e);
        }

        internal async Task<string> UpdateBugReport(ulong id, string status)
        {
            if (BugReportChannel == null)  throw NeitsilliaError.ReplyError("Bug reports are currently deactivated.");

            IUserMessage msg = (IUserMessage)await BugReportChannel.GetMessageAsync(id);

            if (msg == null) throw NeitsilliaError.ReplyError("Message not found.");
            else if (msg.Author.Id != Program.clientCopy.CurrentUser.Id) throw NeitsilliaError.ReplyError("Message not sent by this bot.");

            IEmbed[] embeds = Enumerable.ToArray(msg.Embeds);
            if (embeds.Length < 1)
            {
                await msg.DeleteAsync();
                throw NeitsilliaError.ReplyError("Message had no embeds and was deleted.");
            }
            EmbedAuthor a = (EmbedAuthor)embeds[0].Author;
            string footer = ((EmbedFooter)embeds[0].Footer).Text;
            EmbedBuilder embed = DUtils.BuildEmbed(null,
                    embeds[0].Description, footer, embeds[0].Color ?? new Color(),
                    DUtils.NewField("Reply", status));

            embed.WithAuthor(a.Name, a.IconUrl, a.Url);

            await msg.ModifyAsync(x => { x.Embed = embed.Build(); });

            if (ulong.TryParse(footer, out ulong userId))
                Program.clientCopy.GetUser(userId)?.SendMessageAsync("Your bug report has a new response.", embed: embed.Build());

            return msg.GetJumpUrl();
        }
        #endregion

        public async Task LoadStuff()
        {
            UI.InitialiseOptionLoaderDict();
            AvailablePerks = AvailablePerks ?? FileReading.LoadJSON<AvailablePerks>("Data/Perks/Perks.txt");
            LoadLottery();
            LoadActivity();
            CheckEvents();

            await AMI.Neitsillia.Areas.Nests.Nest.LoadNests();
            AMI.Handlers.TaskHandler.Initiate();
            AMI.Neitsillia.NPCSystems.PopulationHandler.Start();
        }

        //Lottery
        void LoadLottery()
        {
            if (lottery != null) return;

            if (File.Exists(Lottery.savePath)) lottery = FileReading.LoadJSON<Lottery>(Lottery.savePath);
            else lottery = new Lottery(1, 1.00);

            Task lotteryDueTime = new Task(async () => await LotteryThread() );

            lotteryDueTime.Start();
        }
        async Task LotteryThread()
        {
            TimeSpan time = (lottery.dueDate - DateTime.UtcNow);
            int sleepTime = (int)time.TotalMilliseconds;
            int extraSleepTime = Verify.Min((int)(time.TotalMilliseconds - sleepTime), 0);
            RestUserMessage[] msgs = null;
            if (sleepTime > 0)
            {
                Log.LogS($"Next Lottery in {Math.Floor(time.TotalDays)}Days" +
                    $" {time.ToString("h'h 'm'm 's's'")}");
                await Task.Delay(sleepTime);
                await Task.Delay(extraSleepTime);
            }
            while (lottery.entries.Count < 3)
            {
                lottery.daysLength++;
                lottery.dueDate = lottery.dueDate.AddDays(lottery.daysLength);
                lottery.baseValue += lottery.daysLength * 10;
                lottery.Save();
                if (msgs != null)
                {
                    foreach (var m in msgs)
                    {
                        await m.DeleteAsync();
                    }
                }
                msgs = await lottery.Notify("Lottery time extended, more participants required!");
                time = (lottery.dueDate - DateTime.UtcNow);
                sleepTime = (int)time.TotalMilliseconds;
                extraSleepTime = Verify.Min((int)(time.TotalMilliseconds - sleepTime), 0);
                if (sleepTime > 0)
                {
                    Log.LogS($"Lottery extended to {Math.Floor(time.TotalDays)}Days {time.ToString("h'h 'm'm 's's'")}");
                    await Task.Delay(sleepTime);
                }
                await Task.Delay(5);
            }
            await lottery.End();
            lottery = new Lottery(Program.rng.Next(1, 51) * 10, Program.rng.Next(3, 14));
        }
        //Event
        void CheckEvents()
        {
            if (InitiatedEvents) return;

            EventInfo.VerifyHardCoded();

            OngoingEvent.LoadOngoing();

            _ = OngoingEvent.StartWait();

            InitiatedEvents = true;
        }
        //Player Activity
        internal void LoadActivity()
        {
            if (activity != null) return;

            activity = PlayerActivity.LoadCurrent();
            if (activity == null)
                activity = new PlayerActivity(true);
            RefreshActivityThread();
        }

        void RefreshActivityThread()
        {
            DateTime toEnd = activity.start.AddDays(7);
            long time = NumbersM.NParse<long>((toEnd - DateTime.UtcNow).TotalMilliseconds);
            ActivityThread((int)time, (int)(time - int.MaxValue));
        }

        void ActivityThread(int sleepTime, int extraSleeptime)
        {
            new Task(async () =>
            {
                if (sleepTime > 0)
                {
                    await Task.Delay(sleepTime);
                    if (extraSleeptime > 0)
                        await Task.Delay(extraSleeptime);
                }

                try
                {
                    activity.Archive();
                    activity = new PlayerActivity(true);
                    RefreshActivityThread();
                }
                catch(Exception e)
                {
                    Log.LogS("Failed to complete Activity Thread");
                    Log.LogS(e);
                }
            }).Start();
        }
    }
}
