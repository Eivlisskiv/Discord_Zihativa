using AMI.Methods;
using AMI.Neitsillia.Collections;
using AMI.Neitsillia.Items;
using AMI.Neitsillia.User.PlayerPartials;
using AMI.Neitsillia.User.UserInterface;
using AMYPrototype;
using AMYPrototype.Commands;
using Discord;
using AMI.Neitsillia.Items.ItemPartials;
using System;
using System.Threading.Tasks;

namespace AMI.AMIData.Events
{
    class EventShop
    {
        public static EventShop HolidayPreset() => new EventShop("Holiday Coin",
            ("Repair Kit;1", 10),
            ("Rune;1", 50),
            ("~Random", 20),
            ("~Random;5", 80)
            );


        public string currency;

        public StackedObject<string, int>[] stock;
        /* Format for shop item
         * 
         * ---Database Items---
         * Database Item: The item's name 
         * Random Item: ~Random;{type} no type for any type except jewelry
         * 
         * ---Tiered Items---
         * Runes, Repair Kits, etc: [Rune|RepairKit];{level}
         * 
         * ---GearSet Items---
         * GearSet;{Set Name};
         * 
         */

        public EventShop(string currency, params (string, int)[] items)
        {
            this.currency = currency;

            if(items.Length > 0)
            {
                stock = new StackedObject<string, int>[items.Length];
                for (int i = 0; i < stock.Length; i++) stock[i] = new StackedObject<string, int>(items[i]);
            }
        }

        EmbedBuilder BaseEmbed(Player player) => DUtils.BuildEmbed($"{currency} Event Shop", $"{player.name}'s {currency}: {player.Currency.Get(currency)}",
                "Use the \"event\" command for more event related stuff", player.userSettings.Color);

        public async Task ViewShop(Player player, IMessageChannel chan, int item, bool edit)
         => await player.EnUI(edit, null, BaseEmbed(player).AddField(item >= 0 ? GetStock(item, GetMaximum(item, player.Currency.Get(currency))) : GetStock() ).Build(), chan, MsgType.EventShop, item >= 0 ? item.ToString() : null);

        public Item ParseItem(int i, int level)
        {
            string[] data = stock[i].item.Split(';');
            switch (data[0])
            {
                case "Rune": return Item.CreateRune(int.Parse(data[1]));
                case "Repair Kit": return Item.CreateRepairKit(int.Parse(data[1]));

                case "GearSet":
                    string name = GearSets.Drop(data[1]);
                    return Item.LoadItem(name);

                case "~Random": return Item.RandomItem(level * 5, data.Length == 2 ? int.Parse(data[1]) : Program.rng.Next(6, 12));

                default: 
                    return Item.LoadItem(Utils.RandomElement(data));
            }
        }

        public string GetItemName(int i)
        {
            string[] data = stock[i].item.Split(';');
            switch (data[0])
            {
                case "Rune":
                case "Repair Kit":
                    return $"Tier {int.Parse(data[1])} {data[0]}";

                case "GearSet": return $"{data[1]} Set Piece";
                case "~Random": return "Random Scaled " 
                        + (data.Length == 2 ? ((Item.IType)int.Parse(data[1])).ToString() : "Gear");

                default: return stock[i].item;
            }
        }

        internal bool Stackable(int i)
        {
            string[] data = stock[i].item.Split(';');
            switch (data[0])
            {
                case "Rune":
                case "Repair Kit": return true;

                case "GearSet":
                case "~Random": return false;

                default: return false;
            }
        }

        EmbedFieldBuilder GetStock()
        {
            string content = null;

            if (stock != null)
            {
                for (int i = 0; i < stock.Length; i++)
                    content += $"{EUI.GetNum(i + 1)} {GetItemName(i)} | {stock[i].count}x {currency}" + Environment.NewLine;
            }

            return DUtils.NewField("Inventory", content ?? "No shop inventory!");
        }

        int GetMaximum(int i, int c) => NumbersM.FloorParse<int>(((double)c) / stock[i].count);

        EmbedFieldBuilder GetStock(int i, int maxAmount = 0)
            => DUtils.NewField(GetItemName(i), 
                $"{EUI.GetLetter(8)} Buy 1 for {stock[i].count} {Environment.NewLine}"
                + $"{EUI.GetLetter(21)} Buy 5 for {stock[i].count * 5} {Environment.NewLine}"
                + $"{EUI.GetLetter(23)} Buy 10 for {stock[i].count * 10} {Environment.NewLine}"
                + $"{EUI.GetLetter(12)} Buy Maximum: {maxAmount} for {stock[i].count * maxAmount} {Environment.NewLine}"
                );
    }
}
