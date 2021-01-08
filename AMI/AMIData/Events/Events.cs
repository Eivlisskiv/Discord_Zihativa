using AMI.AMIData.OtherCommands;
using AMI.Module;
using AMI.Neitsillia.Areas.AreaPartials;
using AMI.Neitsillia.User.PlayerPartials;
using AMYPrototype;
using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AMI.AMIData.Events
{
    class OngoingEvent
    {
        static MongoDatabase Database => Program.data.database;

        private static CancellationTokenSource awaitEvent;

        public static OngoingEvent Ongoing { get; private set; }

        public static void SetOngoing(OngoingEvent value, bool save = true)
        {
            Ongoing = value;

            Ongoing.eventinfo = Ongoing.eventinfo ?? Database.LoadRecord("EventInfos", MongoDatabase.FilterEqual<EventInfo, string>("_id", Ongoing.name));
            if (Ongoing.eventinfo == null)
            {
                _ = Handlers.UniqueChannels.Instance.SendToLog($"{Ongoing.name} event canceled due to event info data not found");
                Ongoing = null;
            }

            if (Ongoing != null)
            {
                if (save) Database.UpdateRecord("Events", MongoDatabase.FilterEqual<OngoingEvent, string>("_id", "ongoing"), Ongoing);

                _ = GameMasterCommands.SendToSubscribed($"Event {Ongoing.name} has started", Ongoing.EmbedInfo());
            }
        }

        private static List<EventInfo> scheduledEvents;

        public static void LoadOngoing()
        {
            if (Ongoing != null) return;

            Ongoing = Database.LoadRecord("Events", MongoDatabase.FilterEqual<OngoingEvent, string>("_id", "ongoing"));

            if (Ongoing == null) return;

            Ongoing.eventinfo = Ongoing.eventinfo ?? Database.LoadRecord("EventInfos", MongoDatabase.FilterEqual<EventInfo, string>("_id", Ongoing.name));
            if (Ongoing.eventinfo == null)
            {
                _ = Handlers.UniqueChannels.Instance.SendToLog($"{Ongoing.name} event canceled due to event info data not found");
                Ongoing = null;
            }
        }

        public static async Task StartUnscheduledEvent(string name, int days)
        {
            SetOngoing(new OngoingEvent(name, days));
            await StartWait();
        }

        internal static string ExtendOngoing(int days)
        {
            if (Ongoing == null) return "No current event";

            var next = CheckScheduledEvent(false);
            double daysleft = (next.StartDate - Ongoing.endTime.AddDays(days)).TotalDays;

            if (daysleft < 6) return $"{next.name} is starting in less than 3 days, leaving no time for another event";
            days = Math.Min((int)daysleft - 5, days);

            Ongoing.endTime = Ongoing.endTime.AddDays(days);
            Database.UpdateRecord("Events", MongoDatabase.FilterEqual<OngoingEvent, string>("_id", Ongoing._id), Ongoing);

            _ = GameMasterCommands.SendToSubscribed($"Event {Ongoing.name} duration was extended by {days} days", Ongoing.EmbedInfo());

            return $"Event extended by {days} days";
        }

        public static async Task StartWait()
        {
            awaitEvent?.Cancel();

            await Task.Delay(500);

            awaitEvent = new CancellationTokenSource();

            _ = Task.Run(async () =>
            {
                try
                {
                    DateTime wait = GetWaitDate();

                    TimeSpan span = (wait - DateTime.UtcNow);
                    while (span.TotalMilliseconds > 0)
                    {
                        await Task.Delay(span.TotalMilliseconds > int.MaxValue ? int.MaxValue : (int)span.TotalMilliseconds);
                        span = (wait - DateTime.UtcNow);
                    }

                    await EndOngoing();

                    _ = StartWait();
                }catch(Exception e)
                {
                    Methods.Log.LogS(e);
                    _ = Handlers.UniqueChannels.Instance.SendToLog(e);
                }

            }, awaitEvent.Token);
        }

        private static void GetScheduledEvents()
        {
            scheduledEvents = Database.LoadRecords<EventInfo>("EventInfos")
                .Where(e => e.StartDate != default).ToList(); //Scheduled events are the ones with start dates

            scheduledEvents.Sort((x, y) => x.StartDate.CompareTo(y.StartDate));
        }

        private static EventInfo CheckScheduledEvent(bool considerOngoing = true)
        {
            if (scheduledEvents == null) GetScheduledEvents();
            if (considerOngoing && Ongoing != null) return Ongoing.eventinfo;

            DateTime now = DateTime.UtcNow;                                           //Set the year to be the current year
            List<EventInfo> nextScheduled = scheduledEvents.Where(e => (e.StartDate = e.StartDate.AddYears(now.Year - e.StartDate.Year)) > now).ToList();

            //If there is no next this year, so get for next year, else return next this year
            return nextScheduled.Count == 0 ? scheduledEvents[0] : nextScheduled[0];
        }

        private static DateTime GetWaitDate()
        {
            if (Ongoing != null) return Ongoing.endTime;
            EventInfo nextEvent = CheckScheduledEvent();
            DateTime wait = nextEvent.StartDate.AddYears(DateTime.UtcNow.Year - nextEvent.StartDate.Year);

            //if the event start passed
            if (wait < DateTime.UtcNow)
            {
                //If event is currently ongoing
                if (wait.AddDays(nextEvent.daysDuration) > DateTime.UtcNow)
                {
                    SetOngoing(new OngoingEvent(nextEvent));
                    wait = Ongoing.endTime;
                }
                //If next is past, then next is next year
                else wait.AddYears(1);
            }

            return wait;
        }

        static async Task EndOngoing()
        {
            if (Ongoing == null) return;
            
            //End the wait time
            awaitEvent?.Cancel();


            Ongoing.eventinfo.End();

            await Database.DeleteRecord<OngoingEvent>("Events", Ongoing._id);

            DateTime now = DateTime.UtcNow;
            Ongoing._id = $"Expired_{Ongoing.name}_{now.Year}/{now.Month}/{now.Day}";

            Database.SaveRecord("Events", Ongoing);

            _ = GameMasterCommands.SendToSubscribed("Event Ended", Ongoing.EmbedInfo());
            Ongoing = null;

        }

        //Instance
        public string _id = "ongoing";
        public string name;

        public DateTime starttime;
        public DateTime endTime;

        internal EventInfo eventinfo;

        private OngoingEvent(EventInfo eventinfo)
        {
            if (Ongoing?.name != eventinfo.name)
                _ = Handlers.UniqueChannels.Instance.SendToLog($"Event {eventinfo.name} could not start because Event {Ongoing.name} is already ongoing");

            name = eventinfo.name;
            starttime = DateTime.UtcNow;
            endTime = (eventinfo.StartDate).AddDays(eventinfo.daysDuration);
            this.eventinfo = eventinfo;
        }

        /// <summary>
        /// Used for manually starting events from a command
        /// </summary>
        /// <param name="name">The _id of the event</param>
        /// <param name="days">For how long the event should run</param>
        private OngoingEvent(string name, int days)
        {
            if (Ongoing != null) throw NeitsilliaError.ReplyError($"{Ongoing.name} is already ongoing");

            var next = CheckScheduledEvent();
            double daysleft = (next.StartDate - DateTime.UtcNow).TotalDays;

            if (daysleft < 6) throw NeitsilliaError.ReplyError($"{next.name} is starting in less than 3 days, leaving no time for another event");
            days = Math.Min((int)daysleft - 5, days);

            //Days length checks are good

            eventinfo = Program.data.database.LoadRecord("EventInfos", MongoDatabase.FilterEqual<EventInfo, string>("_id", name));

            if (eventinfo == null) throw NeitsilliaError.ReplyError($"{name} Event was not found");
            else if (eventinfo.StartDate != default) throw NeitsilliaError.ReplyError($"Cannot start scheduled {name} Event manually");

            //Event checks out as an existing manual event

            this.name = name;
            starttime = DateTime.UtcNow;
            endTime = DateTime.UtcNow.AddDays(days);
        }

        public void EventBounty(Area area, int floor)
            => eventinfo.eventBounty?.SpawnBounty(area, floor);

        public Embed EmbedInfo() => eventinfo.EmbedInfo(starttime, endTime).Build();

        public async Task OpenShop(Player player, IMessageChannel chan, int i = -1, bool edit = false) =>
            await (eventinfo.shop?.ViewShop(player, chan, i, edit) ?? chan.SendMessageAsync("This event has no shop!", embed: EmbedInfo()));

        public string BountyReward => (eventinfo.shop != null && eventinfo.eventBounty != null) ? eventinfo.shop.currency : null;
    }
}
