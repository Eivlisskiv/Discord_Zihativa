using AMI.Neitsillia.Collections;
using Discord;
using System;
using System.Collections.Generic;

namespace AMI.AMIData.Events
{
    class EventInfo
    {
        public static void VerifyHardCoded()
        {
            var list = new List<EventInfo>()
            {
                 HuntEvent(), 
            };

            foreach (EventInfo e in list)
                AMYPrototype.Program.data.database.UpdateRecord("EventInfos", MongoDatabase.FilterEqual<EventInfo, string>("_id", e.name), e);
        }

        public static EventInfo HuntEvent() =>
            new EventInfo("Bounty Hunter Festival")
            {
                description = "Hunt bounties for trophies and exchange those trophies in the `event shop`",
                shop = new EventShop("Bounty Trophy",
                    ("Repair Kit;1", 5),
                    ("Rune;1", 30),
                    ("GearSet;Hunter Trap", 30),
                    ("~Random", 10),
                    ("~Random;5", 30)
                ),

                eventBounty = new EventBounty("Goq", "Vhoizuku", "Cevharhu"),
            };

        public static EventInfo HalloweenEvent() =>
            new EventInfo("Terror Nights", new DateTime(DateTime.UtcNow.Year, 10, 27), 7)
            {
                description = "Beware the monstrosities of the terror nights.",
                shop = EventShop.HolidayPreset(),
                //new EventShop("Bounty Trophy",
                //    ("Repair Kit;1", 5),
                //    ("Rune;1", 30),
                //    ("~Random", 10),
                //    ("~Random;5", 30)
                //),

                eventBounty = new EventBounty("Goq", "Tsuu Bear", "Vhoizuku"),
            };

        public static EventInfo ThanksgivingEvent() =>
            new EventInfo("Thanksgiving Envoys", new DateTime(DateTime.UtcNow.Year, 11, 23), 7)
            {
                description = "Many creatures have been stealing the Thanksgiving supplies!! Hunt down these bounties and stock up on food.",
                eventBounty = new EventBounty("Raging Turkey")
                {
                    extraBountyDrops = new StackedObject<string, int>[]
                    {
                        new StackedObject<string, int>("Raw Meat", 10),
                        new StackedObject<string, int>("Vhochait", 5),
                    }
                },

                shop = EventShop.HolidayPreset()
            };

        public static EventInfo ChristmasEvent()
            => new EventInfo("Christmas", new DateTime(DateTime.UtcNow.Year, 12, 22), 6)
            {
                description = "Is Christmas y'all.",
                eventBounty = new EventBounty("Mischief Elf")
                {
                    extraBountyDrops = new StackedObject<string, int>[]
                    {
                        new StackedObject<string, int>("Candy Cane", 1),
                        new StackedObject<string, int>("Gift Wrap", 1),
                        new StackedObject<string, int>("Red Ribbon", 1),
                    },
                    rareBountyDrops = new string[]
                    {
                        "Candy Cane Club",
                        "Snowman Ornament",
                        "Empty Gift Box"
                    }
                },
                shop = EventShop.HolidayPreset()
            };

        [MongoDB.Bson.Serialization.Attributes.BsonId]
        public string name;

        public string description;

        public DateTime StartDate;
        public int daysDuration;

        public EventBounty eventBounty;
        public EventShop shop;

        public EventInfo(string name)
        {
            this.name = name;
        }

        public EventInfo(string name, DateTime start, int days)
        {
            this.name = name;
            StartDate = start;
            daysDuration = Math.Max(5, days);
        }

        internal EmbedBuilder EmbedInfo(DateTime start, DateTime endTime)
        {
            EmbedBuilder em = new EmbedBuilder();
            em.WithTitle("Event " + name);
            em.WithDescription(description);

            em.AddField("Dates", "Started: " + start.ToLongDateString()
                + Environment.NewLine + "Ends: " + endTime.ToLongDateString());
            //
            string effects = null;

            if (eventBounty != null) effects += "Event Bounties" + Environment.NewLine;
            if (shop != null) effects += $"{Neitsillia.User.UserInterface.EUI.trade}{shop.currency} Shop" + Environment.NewLine;

            em.AddField("Effects:", effects ?? "None", true);
            return em;
        }

        internal void Start()
        {

        }

        internal void End()
        {
            
        }
    }
}
