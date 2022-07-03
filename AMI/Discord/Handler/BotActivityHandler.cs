using AMI.Neitsillia;
using AMYPrototype;
using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AMI.Handlers
{
    class BotActivityHandler
    {
        public static bool cycle = true;
        public static int delaySeconds = 60;

        private DiscordSocketClient client;
        private bool cycling = false;
        private int currentCycle = 0;

        public BotActivityHandler(DiscordSocketClient client)
        {
            this.client = client;
        }

        public async Task SetActivity(ActivityType activity, string text) => 
            await client.SetGameAsync(text, type: activity);

        public void CycleActivity()
        {
            if (cycling) return;
            cycling = true;

            new Task(async () =>
            {
                while (cycle)
                {
                    try
                    {
                        if (await ApplyStatus(currentCycle))
                            await Task.Delay(delaySeconds * 1000);
                        currentCycle = (currentCycle + 1) % 5;
                    }catch(Exception) { await Task.Delay(10000); }
                }
                cycling = false;

            }).Start();
        }

        public void SetClient(DiscordSocketClient client)
        {
            this.client = client;
            CycleActivity();
        }

        async Task<bool> ApplyStatus(int i)
        {
            switch(i)
            {
                case 1:
                    await SetActivity(ActivityType.Playing, ReferenceData.Version(1));
                    break;
                case 2:
                    await SetActivity(ActivityType.Listening, "`Help` Command");
                    break;
                case 3:
                    await SetActivity(ActivityType.Watching, 
                        Program.data.database.GetRecordsCount("Character")
                        + " Mortals");
                    break;
                case 4:
                    if (AMIData.Events.OngoingEvent.Ongoing != null)
                    {
                        await SetActivity(ActivityType.Streaming,
                            $"Event: {AMIData.Events.OngoingEvent.Ongoing.name}");
                    }
                    else return false;
                break;

                default: return false;
            }
            return true;
        }
    }
}
