﻿using AMI.AMIData.OtherCommands;
using AMI.Methods;
using AMI.Neitsillia.User.PlayerPartials;
using AMYPrototype;
using Discord;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AMI.Neitsillia.NeitsilliaCommands
{
    class Lottery
    {
        internal static string savePath = @".\Data\Lottery.txt";
        internal long TotalValue => (entries.Count * ticketValue) + baseValue;
        public int daysLength;
        public DateTime dueDate;
        public long baseValue = 100;
        public long ticketValue = 10;
        public List<string> entries;

        [JsonConstructor]
        public Lottery(bool json = true)
        { }
        public Lottery(long ticket, int daysLengthArg)
        {
            ticketValue = ticket;
            baseValue = ticketValue * 10;
            entries = new List<string>();
            daysLength = daysLengthArg;
            dueDate = DateTime.UtcNow.AddDays(daysLength);
            _ = Notify();
            Save();
        }
        public Lottery(long ticket, double minutesLength)
        {
            ticketValue = ticket;
            baseValue = ticketValue * 10;
            entries = new List<string>();
            daysLength = 0;
            dueDate = DateTime.UtcNow.AddMinutes(minutesLength);
            _ = Notify();
            Save();
        }
        internal EmbedBuilder Info()
        {
            EmbedBuilder info = new EmbedBuilder();
            info.WithTitle("Lottery");
            var time = (dueDate - DateTime.UtcNow);
            info.WithDescription(
                $"Lottery base value: {baseValue} {Environment.NewLine}" +
                $"Lottery current Total Value: {TotalValue} {Environment.NewLine}" +
                $"Ticket cost: {ticketValue} {Environment.NewLine}" +
                $"Ends in {Math.Floor(time.TotalDays)}Days" +
                    $" {time:h'h 'm'm 's's'}"
                );
            info.WithColor(225, 0, 0);
            return info;
        }
        internal async Task<Discord.Rest.RestUserMessage[]> Notify(string message = "**Lottery!**")
        {
            EmbedBuilder info = Info();
            info.Description += Environment.NewLine + 
                "Enter `~Lottery` to view current worth and buy a ticket.";
            return (await GameMaster.SendToSubscribed(
                message, info.Build())).ToArray();
        }
        internal string AddEntry(Player player)
        {
            string entryKey = player._id;
            if (entries.Contains(entryKey))
                return $"{player.name} is already registered for this lottery.";
            else if (player.KCoins < ticketValue)
                return $"{player.name} may not afford the {ticketValue} coins ticket.";
            player.KCoins -= ticketValue;
            player.SaveFileMongo();
            entries.Add(entryKey);
            Save();
            return $"Congratulations! {player.name} has bought a lottery ticket!";
        }
        internal void Save()
            => FileReading.SaveJSON(this, savePath);
        internal async Task End()
        {
            try
            {
                string winner = GetWinner(out Player player);
                
                player.KCoins += TotalValue;
                player.SaveFileMongo();
                string[] d = winner.Split('\\');
                await GameMaster.ReportActivityToServers(
                $"<@{d[0]}> 's {d[1]} Won the {TotalValue}~~K~~ Lottery!", null);
                await Handlers.DiscordBotHandler.Client.GetUser(ulong.Parse(d[0])).SendMessageAsync(
                    $"Your character {d[1]} has won the {TotalValue}~~K~~ Lottery!");

            }
            catch (Exception e) { Log.LogS(e); Log.LogS("Lottery End Error"); }
        }

        internal string GetWinner(out Player player)
        {
            int i = Program.rng.Next(entries.Count);
            for(int j = 0; j < Program.rng.Next(2, 6); j++)
                i = Program.rng.Next(entries.Count);
            string winner = entries[i];
            player = Player.Load(winner, Player.IgnoreException.All);

            while (player == null)
            {
                entries.RemoveAt(i);
                i = Program.rng.Next(entries.Count);
                winner = entries[i];
                player = Player.Load(winner, Player.IgnoreException.All);
            }

            return winner;
        }
    }
}
