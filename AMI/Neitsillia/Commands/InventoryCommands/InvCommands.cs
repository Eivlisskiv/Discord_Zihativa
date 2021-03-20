using AMI.Commands;
using AMI.Methods;
using AMI.Module;
using AMI.Neitsillia.Collections;
using AMI.Neitsillia.Combat;
using AMI.Neitsillia.Crafting;
using AMI.Neitsillia.Encounters;
using AMI.Neitsillia.Items;
using AMI.Neitsillia.User;
using AMI.Neitsillia.User.PlayerPartials;
using AMI.Neitsillia.User.UserInterface;
using AMYPrototype.Commands;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using AMI.Neitsillia.Items.ItemPartials;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AMI.Neitsillia.InventoryCommands
{
    public class Inventory : ModuleBase<CustomSocketCommandContext>
    {
        public static (int, string) ParseInvUIData(string data)
        {
            string filter = "all";
            if (!int.TryParse(data, out int i))
            {
                string[] s = data.Split(';');
                int.TryParse(s[0], out i);
                if (s.Length > 1) filter = s[1];
            }
            return (i, filter);
        }

        internal static async Task UpdateinventoryUI(Player player, IMessageChannel chan, bool skipSave = false)
        {
            if (player.ui?.type != MsgType.Inventory)
            {
                if (!skipSave) player.SaveFileMongo();
                return;
            }

            if (player.inventory.inv.Count == 0) player.SaveFileMongo();

            (int page, string filter) = ParseInvUIData(player.ui.data);

            await GameCommands.DisplayInventory(player, chan, page, filter, true);
        }

        [Command("Equip")]
        [Summary("To equip a gear piece. \n Example: `~equip 1 2 3` Equips items in slot 1, 2 & 3 of the inventory.")]
        public async Task EquipItemCommand([Summary("The slot(s) of item(s) to equip")] params int[] inventorySlots)
        {
            Player player = Context.GetPlayer(Player.IgnoreException.Resting);
            if (player.HealthStatus(out string hpstate) < -1)
                throw NeitsilliaError.ReplyError($"{player.name} may not equip an item while in a {hpstate} state.");

            if (inventorySlots.Length == 0)
            {
                await AutoEquipGear();
                return;
            }

            bool changes = false;

            //sort list
            EmbedBuilder emItem = new EmbedBuilder();
            emItem.WithFooter("Item equipped before this change has been sent back to your inventory.");
            player.UserEmbedColor(emItem);

            List<int> slots = new List<int>(inventorySlots);
            slots.Sort((x, y) => y.CompareTo(x));

            string message = null;
            for (int i = 0; i < slots.Count; i++)
            {
                int slot = slots[i] - 1;
                if (slot < 0 || slot >= player.inventory.Count)
                    message += $"Slot {slot + 1} is invalid" + Environment.NewLine;
                else
                {
                    Item item = player.inventory.GetItem(slot);
                    //emItem = ItemInformation(emItem, item);

                    if (EquipItem(player, item, out Item temp))
                    {
                        player.inventory.Remove(slot, 1);
                        if (temp != null)
                            player.CollectItem(temp, 1);
                        changes = true;
                        //await ReplyAsync(message, false, emItem.Build());
                        message += item.name + " has been equipped" + Environment.NewLine;
                        player.Quest_Trigger(Neitsillia.Items.Quests.Quest.QuestTrigger.QuestLine, "TII");
                        await UpdateinventoryUI(player, Context.Channel);
                    }
                    else message += $"{item.name} is not gear and cannot be equipped." + Environment.NewLine;
                }
            }
            await DUtils.Replydb(Context, message);
            if (changes)
            {
                if (player.ui != null && player.ui.type == MsgType.ConfirmUpgrade)
                    await player.ui.TryDeleteMessage();
                player.SaveFileMongo();
            }
        }

        [Command("AutoEquip")][Alias("aequip")]
        [Summary("Automatically equips gear from the inventory based of given priorities")]
        public async Task AutoEquipGear(
            [Summary("The gear slot to autoequip, or all")]
            string gearslot = "all",
            [Summary("What stat to compare/find the best of")]
            string stat = "tier")
        {
            Player player = Context.Player;
            if (player.HealthStatus(out string hpstate) < -1)
                throw NeitsilliaError.ReplyError($"{player.name} may not equip an item while in a {hpstate} state.");

            stat = stat?.ToLower();
            gearslot = gearslot?.ToLower();

            Dictionary<string, Item.IType> slots = new Dictionary<string, Item.IType>()
            {
                {"weapon", Item.IType.Weapon },
                {"helmet", Item.IType.Helmet },
                {"mask", Item.IType.Mask },
                {"chest", Item.IType.Chest },
                {"trousers", Item.IType.Trousers },
                {"boots", Item.IType.Boots },
            };

            string response = null;
            if (gearslot == "all")
            {
                foreach (var pair in slots)
                {
                    response += AutoEquipSlot(player, pair.Value, GetComparer(stat, gearslot)) + Environment.NewLine;
                }
                player.SaveFileMongo();
            }
            else if (slots.ContainsKey(gearslot))
            {
                response = AutoEquipSlot(player, slots[gearslot], GetComparer(stat, gearslot));
                player.SaveFileMongo();
            }
            else
            {
                string pre = Environment.NewLine + $"{Context.Prefix}AutoEquip ";
                response = "gearslot input was not valid" +
                    pre + string.Join(pre, slots.Keys.ToArray());
            }
            await ReplyAsync(response);
        }

        Func<Item, Item, int> GetComparer(string stat, string slot)
        {
            int basecomparer(Item x, Item y) => x.tier == y.tier ? (x.condition >= y.condition ? 1 : -1) : (x.tier > y.tier ? 1 : -1);
            int damageCompare(Item x, Item y)
            {
                long totalX = ArrayM.Total(x.damage);
                long totalY = ArrayM.Total(y.damage);
                return (totalX == totalY ? basecomparer(x, y) : totalX >= totalY ? 1 : -1);
            }
            int resistanceCompare(Item x, Item y)
            {
                long totalX = ArrayM.Total(x.resistance);
                long totalY = ArrayM.Total(y.resistance);
                return (totalX == totalY ? basecomparer(x, y) : totalX >= totalY ? 1 : -1);
            }
            Dictionary<string, Func<Item, Item, int>> comparers = new Dictionary<string, Func<Item, Item, int>>()
            {
                { "tier", basecomparer },

                {
                    "health", (x, y) =>  x.type == Item.IType.Weapon ? damageCompare(x, y)
                        : (x.healthBuff == y.healthBuff ? basecomparer(x, y) : x.healthBuff >= y.healthBuff ? 1 : -1)
                },

                {
                    "damage", (x, y) =>  x.type == Item.IType.Weapon ? damageCompare(x, y)
                        : (x.healthBuff == y.healthBuff ? basecomparer(x, y) : x.healthBuff >= y.healthBuff ? 1 : -1)
                },

                { "resistance", (x, y) =>  x.type == Item.IType.Weapon ? damageCompare(x, y)  : resistanceCompare(x, y) },

                { "agility", (x, y) => (x.agility == y.agility ? basecomparer(x, y) : x.agility >= y.agility ? 1 : -1) },

                { "critchance", (x, y) => (x.critChance == y.critChance ? basecomparer(x, y) : x.critChance >= y.critChance ? 1 : -1) },
                { "critdamage", (x, y) => (x.critMult == y.critMult ? basecomparer(x, y) : x.critMult >= y.critMult ? 1 : -1) },
            };

            if(comparers.ContainsKey(stat))
                return comparers[stat];

            string pre = Environment.NewLine + $"{Context.Prefix}AutoEquip {slot} ";
            throw NeitsilliaError.ReplyError("stat to compare input was not valid" +
                pre + string.Join(pre, comparers.Keys.ToArray()));
        }
        string AutoEquipSlot(Player player, Item.IType slot, Func<Item, Item, int> comparer)
        {
            //Find the indexes in the inventory for the items of this slot available 
            List<int> indexes = Enumerable.Range(0, player.inventory.Count).Where(i =>
            {
                Item item = player.inventory.GetItem(i);
                return item.type == slot && player.IsRequiredLevel(item.tier);

            }).ToList();

            if (indexes.Count == 0) return $"No {slot} available";
            //sort with comparer
            indexes.Sort((x, y) => -comparer(player.inventory.GetItem(x), player.inventory.GetItem(y)));

            //Get index of top
            int topIndex = indexes[0];
            Item top = player.inventory.GetItem(topIndex);
            //get currently equipped
            Item current = player.equipment.GetGear(slot);

            //if current exists and is better than top found
            if ((current != null && comparer(current, top) > 0) || !EquipItem(player, top, out current))
                return $"Current {slot} was kept";
            else
            {
                player.inventory.Remove(topIndex, 1);
                string result = null;
                if(current != null)
                {//return the past equipped item to the inventory
                    player.inventory.Add(current, 1, -1); //ignore inventory size bc fuck that
                    result = $"returned {current} to inventory and ";
                }
                return result + $"equipped {top}";
            } 
        }

        private bool EquipItem(Player player, Item eItem, out Item temp)
        {
            temp = null;
            bool equipped = false;
            int index = -1;
            if (!player.IsRequiredLevel(eItem.tier))
                throw NeitsilliaError.ReplyError($"{player.name} must be minimum level {Math.Ceiling(eItem.tier / 5.00)} to equip this item.");
            switch (eItem.type)
            {
                case Item.IType.Weapon:
                    {
                        index = 0;
                    } break;
                //case Item.IType.weapon: index = 1; break;
                case Item.IType.Helmet: index = 2; break;
                case Item.IType.Mask: index = 3; break;
                case Item.IType.Chest: index = 4; break;
                case Item.IType.Jewelry:
                    {
                        for (int i = 0; i < player.equipment.jewelry.Length; i++)
                            if (player.equipment.jewelry[i] == null)
                                index = 5 + i;
                        if (index == -1)
                            throw NeitsilliaError.ReplyError("You do not have any free jewelry slot");
                    } break;
                case Item.IType.Trousers: index = 8; break;
                case Item.IType.Boots: index = 9; break;

                default: return false;
            }
            if (index > -1 && index < 10)
            {
                temp = player.equipment.GetGear(index);
                player.equipment.SetGear(index, eItem);
                equipped = true;
            }
            if (temp != null && temp.type == Item.IType.notfound)
            { temp = null; }
            return equipped;
        }

        [Command("Unequip")][Alias("uneq")]
        [Summary("Unequip gear. You must precise the gear slot you want to unequip from plus a number from 1 to 3 for the jewelry slot.")]
        public async Task Unequip(
            [Summary("The name of the slot to unequip [ all, weapon, helmet, mask, chest, jewelry(1- 3), trousers, boots ].")]
            string gearSlot,
            [Summary("For jewelry slot only. 1-3, which jewelry.")]
            int slot = 0)
        {
            slot--;
            switch (gearSlot?.ToLower())
            {
                case "all": await StripEquipment(); return;
                case "weapon": slot = 0; break;
                case "secondary": slot = 1; break;
                case "helmet": slot = 2; break;
                case "mask": slot = 3; break;
                case "chest": slot = 4; break;
                case "jewelry": slot = Verify.MinMax(slot + 5, 7, 0); break;
                case "trousers": slot = 8; break;
                case "boots": slot = 9; break;
                default:
                    await DUtils.Replydb(Context, "Gear Slot unrecognized, example: ``~unequip Weapon`` to unequip primary weapon" + Environment.NewLine
                    + "Weapon: Primary Weapon" + Environment.NewLine
                    //+ "Secondary: Secondary Weapon" + Environment.NewLine
                    + "Helmet: Helmet Gear Piece" + Environment.NewLine
                    + "Mask: Mask Gear Piece" + Environment.NewLine
                    + "Chest: Chest Plate Gear Piece" + Environment.NewLine
                    + "Jewelry #slot: Jewelry [1-3]" + Environment.NewLine
                    + "Trousers: Trousers Gear Piece" + Environment.NewLine
                    + "Boots: Boots Gear Piece" + Environment.NewLine
                    );
                    return;
            }

            Player player = Context.GetPlayer(Player.IgnoreException.Resting);
            if (player.IsDead())
                throw NeitsilliaError.ReplyError($"{player.name} may not unequip gear while dead...");
            Item temp = player.equipment.GetGear(slot);
            if (temp != null)
            {
                if (player.CollectItem(temp, 1))
                {
                    await DUtils.Replydb(Context, $"{temp} Returned to inventory", lifetime: 2);
                    player.equipment.SetGear(slot, null);
                    await UpdateinventoryUI(player, Context.Channel);
                }
                else await DUtils.Replydb(Context, "Inventory Full", lifetime: 1);
            }
            else await DUtils.Replydb(Context, "Gear Slot Empty.", lifetime: 1);

        }

        [Command("Strip")][Summary("Unequip all gear!")]
        public async Task StripEquipment()
        {
            Player player = Context.Player;
            string result = null;
            for(int i = 0; i < Equipment.gearCount; i++)
            {
                Item gear = player.equipment.GetGear(i);
                if(gear != null)
                {
                    if (player.CollectItem(gear, 1))
                    {
                        result += $"{gear} was returned to inventory" + Environment.NewLine;
                        player.equipment.SetGear(i, null);
                    }
                    else result += $"inventory may not contain {gear}" + Environment.NewLine;
                }
            }
            await UpdateinventoryUI(player, Context.Channel);
            await ReplyAsync(result ?? "Nothing to unequip");
        }

        ////    
        [Command("ItemInfo")]
        [Summary("Get the info on an item from its name.")]
        public async Task ItemInfo(params string[] itemName)
        {
            string message = null;
            string name = StringM.UpperAt(ArrayM.ToString(itemName, " "));
            Item item = Item.LoadItem(name);

            if (item != null)
            {
                EmbedBuilder emItem = new EmbedBuilder();
                item.EmdebInfo(emItem);
                message = "Educate yourself with this information, young one.";
                await DUtils.Replydb(Context, message, emItem.Build());
            }
            else
                await DUtils.Replydb(Context, Context.User.Mention + ", I could not find anything with such name.");
        }

        [Command("IDrop")][Alias("drop")]
        [Summary("Drop an item from your inventory into the void. (Deletes the item)")]
        public async Task DropItem(string indexXamount)
        {
            string message = null;
            var ia = Verify.IndexXAmount(indexXamount);
            int index = ia.index - 1;
            int amount = ia.amount;
            Player player = VerifyIndex(index);
            if (player.IsDead())
                message = $"{player.name} may not drop items while dead...";
            if (message == null)
            {
                player.inventory.Remove(index, Verify.MinMax(amount, player.inventory.GetCount(index)));
                if (player.ui != null && player.ui.type == MsgType.ConfirmUpgrade)
                    await player.ui.TryDeleteMessage();
                await DUtils.Replydb(Context, Context.User.Mention + " Item Discarded");
                await UpdateinventoryUI(player, Context.Channel);
            }
            else
                await DUtils.Replydb(Context, message);
        }

        [Command("consume")][Alias("eat", "drink")]
        [Summary("Consume one or more of an item in your inventory.")]
        public async Task IConsume(string indexXamount)
        {
            string message = null;
            var ia = Verify.IndexXAmount(indexXamount);
            int index = ia.index - 1;
            int amount = ia.amount;
            Player player = VerifyIndex(index);
            Item item = player.inventory.GetItem(index);
            if (index > player.inventory.Count || item == null)
                await DUtils.Replydb(Context, "Invalid Item");
            else if (item.type != Item.IType.Healing && item.type != Item.IType.Consumable)
                await DUtils.Replydb(Context, Context.User.Mention + ", item is not consumable.");
            else if (player.inventory.GetCount(index) < amount)
                await DUtils.Replydb(Context, Context.User.Mention + ", you do not posses the required amount of this item.");
            else if (message == null)
            {
                bool inCombat = player.IsEncounter("Combat");
                if (inCombat && player.duel != null && player.duel.abilityName != null
                    && player.duel.abilityName.StartsWith("~"))
                    await ReplyAsync($"{player.name} may not change their turn action from {player.duel.abilityName.Substring(1)}");
                else if (player.HealthStatus(out string hpstate) < -1)
                    await ReplyAsync($"{player.name} may not consume an item while in a {hpstate} state.");
                else
                {
                    EmbedBuilder effect = DUtils.BuildEmbed(player.name, null, null, player.userSettings.Color, Consume(player, item, amount, out int consumed));
                    player.UserEmbedColor(effect);
                    player.inventory.Remove(index, consumed);
                    await DUtils.Replydb(Context, embed: effect.Build());
                    if (inCombat)
                    {
                        if (player.duel == null)
                            player.duel = new DuelData(null)
                            { abilityName = "~Consume" };
                        else player.duel.abilityName = "~Consume";

                        if (player.Encounter.Name == Encounter.Names.PVP)
                            await CombatCommands.PVPTurn(player, "~Consume", Context.Channel);
                        else
                            await CombatCommands.TurnCombat(player, "~Consume", Context.Channel);
                    }
                    await UpdateinventoryUI(player, Context.Channel);
                    player.Quest_Trigger(Items.Quests.Quest.QuestTrigger.QuestLine, "TVIII");
                }
            }
            else
                await DUtils.Replydb(Context, message);
        }

        [Command("heal", true)]
        [Summary("Searches the inventory for healing consumables and consumes them")]
        public async Task AutoHeal()
        {
            Player player = Context.Player;

            if (player.IsEncounter("Combat")) { await ReplyAsync("You must eat healing items manually during combat"); return; }

            long mhp = player.Health();
            if (player.health >= mhp) { await ReplyAsync("Health is full"); return; }

            //Get the first healing item in inventory
            int index = player.inventory.inv.FindIndex(si => si.item.type == Item.IType.Healing);

            EmbedBuilder embed = DUtils.BuildEmbed(player.name, color: player.userSettings.Color);
            while (index > -1 && player.health < mhp)// While has healing item and health not full
            {
                //Consume and write information
                embed.AddField(Consume(player, player.inventory.GetItem(index), player.inventory.GetCount(index), out int ate));
                //Remove what was consumed
                player.inventory.Remove(index, ate);
                //Search for next healing item
                index = player.inventory.inv.FindIndex(si => si.item.type == Item.IType.Healing);
            }

            if (index == -1) embed.AddField("Out of healing items", "You are out of healing items in your inventory!");

            await ReplyAsync(embed: embed.Build());
        }

        EmbedFieldBuilder Consume(Player player, Item item, int amount, out int consumed)
        {
            string itemE = null;
            long healing = 0;
            int stamRegen = 0;

            long mhp = player.Health();
            int msp = player.Stamina();

            consumed = 0;
            for (; consumed < amount; consumed++)
            {
                bool hp = item.healthBuff > 0 && mhp > player.health;
                bool sp = item.staminaBuff > 0 && msp > player.stamina;
                if (hp || sp)
                {
                    if (hp) healing += player.Healing(item.healthBuff, false);
                    if (sp) stamRegen += player.StaminaE(((int)item.healthBuff / 2) + item.staminaBuff);
                }
                else break;
            }

            //Get Item changes info
            if (healing != 0)
                itemE += $"+ {healing} Health : {player.health}/{mhp}{Environment.NewLine}";
            if (stamRegen != 0)
                itemE += $"+ {stamRegen} Stamina: {player.stamina}/{msp}{Environment.NewLine}";
            if (item.perk != null)
            {
                if (consumed == 0)
                    consumed = 1;
                player.Status(item.perk.name, item.perk.maxRank, item.perk.tier);
                itemE += $"+ Status Effect: {item.perk.name} {Environment.NewLine} {item.perk.desc}";
            }
            //end
            player.QuestTrigger(Items.Quests.Quest.QuestTrigger.Consuming, $"{item.originalName};{amount};{item.type}");
            return DUtils.NewField("Consuming " + item.name, $"Consumed {consumed} {item.name}{Environment.NewLine}" + itemE, true);
        }

        [Command("UseItem")][Alias("Use")]
        [Summary("Uses and item from the inventory. Usable items are Schematics, Runes and Repair kits.")]
        public async Task IUse(string indexXamount, 
            [Summary("If the item must be used on another item, enter its inventory slot number")]
            int targetItem = 0)
        {
            Player player = Context.Player;

            bool inCombat = player.IsEncounter("Combat");
            if (inCombat && player.duel != null && player.duel.abilityName != null
                && player.duel.abilityName.StartsWith("~"))
                await ReplyAsync($"{player.name} may not change their turn action from {player.duel.abilityName.Substring(1)}");

            (int index, int amount) = Verify.IndexXAmount(indexXamount);
            index--;
            targetItem--;

            if (index > player.inventory.Count - 1 || index < 0) throw NeitsilliaError.ReplyError("That inventory slot does not exist.");

            Item item = player.inventory.GetItem(index);
            if (item == null) throw NeitsilliaError.ReplyError($"{player.name}, selection invalid.");

            (string message, EmbedBuilder embed) data;
            switch(item.type)
            {
                case Item.IType.Mysterybox:
                    if (inCombat) throw NeitsilliaError.ReplyError($"{player.name}, you may not use this during combat.");
                    data = MysteryBoxLoot(player, index, amount);
                    break;
                case Item.IType.Schematic:
                    if (inCombat) throw NeitsilliaError.ReplyError($"{player.name}, you may not use a schematic during combat.");
                    data = TemporarySchematicUsage(player, index);
                    break;
                case Item.IType.Rune:
                    if (inCombat) throw NeitsilliaError.ReplyError($"{player.name}, you may not use runes during combat.");
                    data = UseRune(player, index, amount, targetItem);
                    break;
                case Item.IType.RepairKit:
                    data = UseRepairKit(player, index, amount, targetItem);
                    break;
                case Item.IType.EssenseVial:
                    if (inCombat) throw NeitsilliaError.ReplyError($"{player.name}, you may not use essence vials during combat.");
                    data = UseEssenceVial(player, index);
                    break;
                
                default:
                    await DUtils.Replydb(Context, 
                        $"{item.name} is a {item.type} which is not \"usable\"." + Environment.NewLine
                        + "Usable item types are **Schematics, Essence Vials, Runes and RepairKits**");
                    return;
            }

            data.embed?.WithColor(player.userSettings.Color);
            await DUtils.Replydb(Context, data.message, data.embed?.Build());

            if (inCombat)
            {
                if (player.duel == null)
                    player.duel = new DuelData(null)
                    { abilityName = "~Consume" };
                else player.duel.abilityName = "~Consume";

                if (player.Encounter.Name == Encounter.Names.PVP)
                    await CombatCommands.PVPTurn(player, "~Consume", Context.Channel);
                else
                    await CombatCommands.TurnCombat(player, "~Consume", Context.Channel);
            }
            else await UpdateinventoryUI(player, Context.Channel);

        }

        (string, EmbedBuilder) MysteryBoxLoot(Player player, int slot, int amount)
        {
            string message = null;
            bool end = false;
            Random rng = new Random();
            Item item = player.inventory.GetItem(slot);

            for (int i = 0; i < amount && !end; i++)
            {
                Item it = Item.RandomItem(rng.Next(item.tier - 3, item.tier + 4));
                if (player.CollectItem(it, 1))
                    message += $"{it.name} Collected" + Environment.NewLine;
                else
                {
                    if (player.Encounter == null)
                        player.NewEncounter(new Encounter(Encounter.Names.Loot, player));
                    player.Encounter.AddLoot(it);
                    message += $"Inventory full, {it.name} added to loot." + Environment.NewLine;
                }
            }
            player.inventory.Remove(slot, amount);
            return (message.Length > 1020 ? message.Substring(0, 1020) : message, null);
        }
        (string, EmbedBuilder) TemporarySchematicUsage(Player player, int schematicIndex)
        {
            Item schemItem = player.inventory.GetItem(schematicIndex);
            Schematic schematic = schemItem.schematic;
            string reply = schematic.CheckPlayerInventoryForMaterial(player, 1);
            if (reply == null)
            {
                player.inventory.Remove(schematicIndex, 1);

                Item result = Item.LoadItem(schematic.name);
                schematic.ConsumeSchematicItems(player, 1);

                if(result.CanBeEquip())
                result.schematic = null;
                result.VerifyItem(true);

                if (!player.CollectItem(result, 1))
                {
                    if (player.Encounter == null)
                        player.NewEncounter(new Encounter(Encounter.Names.Loot, player));
                    player.Encounter.AddLoot(result);
                    return ($"Inventory full, {result.name} added to loot.", result.EmdebInfo(player.UserEmbedColor()));
                }
                return ($"{player.name} Crafted {result.name}.", result.EmdebInfo(player.UserEmbedColor())); 
            }
            return (reply, null);
        }
        (string, EmbedBuilder) UseRune(Player player, int runei, int amount, int geari)
        {
            if (geari < 0 || geari > player.inventory.Count) throw NeitsilliaError.ReplyError("Please enter the inventory slot for the Gear to apply the rune to: " +
            $"`Use {runei + 1}{(amount > 1 ? "x" + amount : null)} #inventorySlot`");

            Item gear = player.inventory.GetItem(geari);

            if (!gear.CanBeEquip()) throw NeitsilliaError.ReplyError("Runes can only be applied to gear.");
            if (gear.tier > (player.level - 1) * 5) throw NeitsilliaError.ReplyError($"You must be higher level to upgrade this gear ({1+gear.tier/5} level).");

            Item rune = player.inventory.GetItem(runei);

            amount = Math.Min(amount, player.inventory.GetCount(runei)); //change amount to use to minimum how much is available

            amount = Math.Min(amount,
                NumbersM.FloorParse<int>(((player.level * 5) - gear.tier)/5.00)
                ); //change the amount to maximum allowed for level

            int tiers = rune.tier * amount;

            int upgrades = gear.baseTier - gear.tier;

            gear.Scale(gear.tier + tiers);

            gear.baseTier += upgrades;

            player.inventory.Remove(runei, amount);

            return (null, DUtils.BuildEmbed($"{gear.name} Upgraded",
                $"Consumed {amount}x {rune.name}" + Environment.NewLine +
                $"{gear} => {gear.tier}"
                ));
        }
        (string, EmbedBuilder) UseRepairKit(Player player, int kiti, int amount, int geari)
        {
            if (geari < 0 || geari > player.inventory.Count) throw NeitsilliaError.ReplyError("Please enter the inventory slot for the Gear to repair with the kit: " +
            $"`Use {kiti + 1}{(amount > 1 ? "x" + amount : null)} #inventorySlot`");

            Item gear = player.inventory.GetItem(geari);

            if (!gear.CanBeEquip()) throw NeitsilliaError.ReplyError("Repair kits can only be applied to gear.");
            if (gear.condition >= gear.durability) throw NeitsilliaError.ReplyError("This gear does not require repairing.");

            Item kit = player.inventory.GetItem(kiti);

            amount = Math.Min(amount, player.inventory.GetCount(kiti)); //change amount to use to minimum how much is available

            amount = Math.Min(amount, NumbersM.CeilParse<int>((gear.durability - gear.condition) / (double)kit.condition)); //change the amount to maximum required for max cnd

            gear.condition = Math.Min(gear.condition + (amount * kit.condition), gear.durability);

            player.inventory.Remove(kiti, amount);

            return (null, DUtils.BuildEmbed($"{gear.name} repaired", 
                $"Consumed {amount}x {kit.name}" + Environment.NewLine + 
                $"{gear} | {gear.condition}/{gear.durability} CND"
                ));
        }
        (string, EmbedBuilder) UseEssenceVial(Player player, int index)
        {
            if (player.specter == null) throw NeitsilliaError.ReplyError("You have not yet awoken your specter.");

            Item vial = player.inventory.GetItem(index);

            if (player.specter.Equip(vial))
            {
                player.inventory.Remove(index, 1);
                player.QuestTrigger(Neitsillia.Items.Quests.Quest.QuestTrigger.QuestLine, "Whispers Of The Wind VI");
                return ($"{vial.name} was applied to your specter", player.specter.essence.InfoPage(new EmbedBuilder(), false));
            }
            else return ("Your specter is not powerful enough to use this essence", null);
        }

        [Command("CompareItem")][Alias("Compare")]
        [Summary("Use this command to compare stats between items")]
        public async Task CompareItems(
            [Summary("The inventory slot number of the item to compare.")]
            int firstSlot,
            [Summary("The item to compare with the first. By default, compares with equipped item")]
            int secondSlot = -1, int jewelrySlotToCompare = 0)
        {
            Player player = Context.Player;
            firstSlot--;
            secondSlot--;

            Item a = null;
            Item b = null;
            bool bIsEquipped = false;
            if (firstSlot < 0 || firstSlot > player.inventory.Count - 1)
                throw NeitsilliaError.ReplyError("First Slot entered is invalid.");
            else
                a = player.inventory.GetItem(firstSlot);

            if (secondSlot < 0 && a.CanBeEquip())
            {
                b = player.equipment.GetGear(a.type, jewelrySlotToCompare);
                bIsEquipped = true;
                if (b == null)
                    throw NeitsilliaError.ReplyError("No equipped gear of this type, please select an item slot from your inventory to compare.");
            }
            else if (secondSlot > -1 && secondSlot < player.inventory.Count)
                b = player.inventory.GetItem(secondSlot);
            else
                throw NeitsilliaError.ReplyError("Second slot is invalid");

            await ReplyAsync(embed: bIsEquipped ?
                player.UserEmbedColor(b.CompareTo(a, 0)).Build()
                : player.UserEmbedColor(a.CompareTo(b, -1)).Build()
                
                );
        }

        #region Tools
        [Command("UseTool")][Alias("tuse", "uset")]
        [Summary("Use the specified tool. Enter tool name: ~UseTool Axe")]
        public async Task UseTool(string toolName)
        {
            Player player = Player.Load(Context.BotUser);
            if (player.Tools == null) player.Tools = new Tools(player._id);

            if (player?.Encounter?.Name == Encounter.Names.Ressource)
            {
                string[] ds = player.Encounter.data.Split(';');
                if (ds[0].Equals(toolName, StringComparison.OrdinalIgnoreCase))
                {
                    await player.NewUI($"Vein exploited, " +
                        Resource.Exploit(player, ds[0].ToLower(), ds[1]), player.Encounter.GetEmbed().Build(), 
                        Context.Channel, MsgType.Loot);
                    return;
                }
            }

            string reply;
            switch (toolName.ToLower())
            {
                case "sickle":
                case "pickaxe":
                case "axe":
                //case "spear":
                    reply = player.Tools.UseTool(toolName.ToLower(), player, player.Area.type);
                    break;
                default:
                    reply = "Tool not recognized, available tools: " + Environment.NewLine
                        + "Sickle" + Environment.NewLine
                        + "Axe" + Environment.NewLine
                        + "Pickaxe" + Environment.NewLine
                        ;
                    break;
            }
            await DUtils.DeleteBothMsg(Context, await ReplyAsync(reply));
        }

        [Command("UpgradeTool")][Alias("tup")]
        [Summary("Upgrade the specified tool: ~UpgradeTool Axe")]
        public async Task UpgradeTool(string toolName)
        {
            string reply = null;
            Player player = Player.Load(Context.BotUser);
            if (player.Tools == null)
                player.Tools = new Tools(player._id);
            switch (toolName.ToLower())
            {
                case "sickle":
                case "pickaxe":
                case "axe":
                    reply = player.Tools.Upgrade(toolName.ToLower(), player);
                    break;
                default:
                    reply = "Tool not recognized, available tools: " + Environment.NewLine
                        + "Sickle" + Environment.NewLine
                        + "Axe" + Environment.NewLine
                        + "Pickaxe" + Environment.NewLine
                        ;
                    break;
            }
            await DUtils.DeleteBothMsg(Context, await ReplyAsync(reply));
        }

        [Command("Tools")][Alias("tool")]
        [Summary("View info on the character's tools")]
        public async Task ToolsInfo()
        {
            Player player = Player.Load(Context.BotUser);
            if (player.Tools == null)
                player.Tools = new Tools(player._id);
            string[] tools = { "Sickle", "Axe", "PickAxe" };
            EmbedBuilder toolsInfo = player.UserEmbedColor(new EmbedBuilder());
            toolsInfo.WithTitle($"{player.name}'s Tools");
            foreach (var s in tools)
                toolsInfo.AddField(s, player.Tools.ToolInfo(s.ToLower()), true);
            toolsInfo.WithFooter("~tuse tooltype |> use the tool" + Environment.NewLine+
                " ~tup tooltype |> upgrade tool");
            await ReplyAsync(embed:toolsInfo.Build());
        }
        #endregion

        [Command("InspectItem")][Alias("Iinfo", "inspect")]
        [Summary("Inspect an item from the inventory.")]
        public async Task IInfo(int slot)
        {
            string message = null;
            slot--;
            Player player = VerifyIndex(slot);
            if (message == null)
            {
                Item item = player.inventory.GetItem(slot);
                EmbedBuilder emItem = new EmbedBuilder();
                player.UserEmbedColor(emItem);
                if (item.type == Item.IType.notfound)
                    Console.WriteLine("Item was not found");
                emItem = item.EmdebInfo(emItem);
                message = "Educate yourself with this information, young one.";
                await DUtils.Replydb(Context, message, emItem.Build());
            }
            else
                await DUtils.Replydb(Context, message);
        }

        #region Loot
        [Command("View Loot")]
        [Alias("vloot")]
        public async Task ViewLoot(int page = 1)
        {
            await ViewLoot(Context.Player, Context.Channel, page - 1);
            await DUtils.DeleteContextMessageAsync(Context);
        }
        internal static async Task ViewLoot(Player player, IMessageChannel chan, int page, bool isEdit = false)
        {
            if (player.Encounter == null || player.Encounter.loot == null || player.Encounter.loot.Count <= 0)
                await chan.SendMessageAsync("No loot found");
            else
            {
                if (page < 0) page = 0;

                EmbedBuilder inLoot = player.Encounter.loot.ToEmbed(ref page, "Loot", -1, player.equipment);
                inLoot.WithDescription($"Use `Loot` command to loot a specific item. Example: `~loot 2x5` loots 5 of the item in slot 2");
                inLoot.WithColor(player.userSettings.Color);

                if (isEdit && player.ui != null)
                    await player.EditUI("Loot Instance", inLoot.Build(),chan, MsgType.Loot, page.ToString());
                else
                    await player.NewUI(await chan.SendMessageAsync("Loot Instance", embed: inLoot.Build()), MsgType.Loot, page.ToString());
            }
        }
        [Command("Loot")]
        public async Task CollectLoot(string arg = "all")
            => await CollectLoot(Context.Player, Context.Channel, arg);
        internal static async Task CollectLoot(Player player, ISocketMessageChannel chan, string arg = "all")
        {
            int index = -1;
            int amount = 1;
            string result = "Failed to retrieve item.";
            
            if (player.Encounter != null && player.Encounter.loot != null && player.Encounter.loot.Count > 0)
            {
                Encounter enc = player.Encounter;

                if (arg.ToLower() == "all")
                {
                    string lootList = "";
                    int i = 0;
                    for (; i < enc.loot.Count; i++)
                    {
                        if (lootList.Length < 500) lootList += (i + 1) + "| " + enc.loot.inv[i] + Environment.NewLine;
                        if (!player.CollectItem(enc.loot.GetItem(i), enc.loot.GetCount(i), true))
                            throw NeitsilliaError.ReplyError("Inventory cannot store all items. Use ``~view loot`` to view the list of loot.");
                    }

                    if (lootList.Length > 500)
                        lootList += $"+ {enc.loot.Count - i} More items";

                    player.EndEncounter();

                    EmbedBuilder looted = new EmbedBuilder();
                    player.UserEmbedColor(looted);
                    looted.AddField("All Collected Loot", lootList);

                    player.Quest_Trigger(Items.Quests.Quest.QuestTrigger.QuestLine, "TVII");

                    await player.NewUI(await chan.SendMessageAsync(embed: looted.Build()),
                        MsgType.Main);

                }
                else
                {
                    var ia = Verify.IndexXAmount(arg);
                    index = ia.index - 1;
                    amount = ia.amount;
                    index = Verify.Max(index, enc.loot.Count - 1);
                    amount = Verify.MinMax(amount, enc.loot.GetCount(index), 1);
                    if (player.CollectItem(enc.loot.GetItem(index), amount))
                    {
                        result = $"{amount}x {enc.loot.GetItem(index)} Collected";
                        enc.loot.Remove(index, amount);
                        
                        player.Quest_Trigger(Items.Quests.Quest.QuestTrigger.QuestLine, "TVII");

                        var reply = await chan.SendMessageAsync(result);

                        if (player.ui?.type == MsgType.Loot)
                        {
                            int.TryParse(player.ui.data, out int page);
                            await ViewLoot(player, chan, page, true);
                        }
                        else player.ui = new UI(reply, MsgType.Loot, player);
                    }
                    else DUtils.DeleteMessage(await chan.SendMessageAsync($"Inventory may not contain {amount}x {enc.loot.GetItem(index)}"));
                    
                }
            }
            else
                DUtils.DeleteMessage(await chan.SendMessageAsync("No Loot Found"));
            
        }
        #endregion
        //
        Player VerifyIndex(int slot)
        {
             Player player = Context.Player;

            if (slot > player.inventory.Count - 1 || slot < 0)
                throw NeitsilliaError.ReplyError("That inventory slot does not exist.");
            else if (player.inventory.inv[slot] == null)
                throw NeitsilliaError.ReplyError("Mortal, stop wasting time. The usage of nothing is meaningless.");

            return player;
        }

        [Command("Status")]
        public async Task StatusInfo()
        {
            Player player = Context.Player;
            if (player.status.Count > 0)
            {
                EmbedBuilder embed = new EmbedBuilder();
                embed.WithTitle($"{player.name}'s Status Effects");
                foreach (Perk p in player.status)
                {
                    embed.Description += $"**{p.name}**[{p.tier}|{p.rank}/{p.maxRank}]{Environment.NewLine}" +
                        p.desc + Environment.NewLine;
                }
                await ReplyAsync(embed: embed.Build());
            }
            else await ReplyAsync($"{player.name} has no active status effects.");
        }

        #region Sorting
        [Command("Sort"), Summary("Use this command to sort your inventory.")]
        public async Task SortCommand(string list = "", string sortby = "", int direction = 1)
        {
            string prefix = Context.Prefix + "Sort ";
            Player player = Context.Player;
            bool save = true;
            switch(list.ToLower())
            {
                case "inv":
                case "inventory":
                    prefix += list;
                    switch(sortby.ToLower())
                    {
                        case "rank":
                            {
                                if(direction >= 0)
                                    player.inventory.inv.Sort((x, y) => x.item.tier.CompareTo(y.item.tier));
                                else
                                    player.inventory.inv.Sort((y, x) => x.item.tier.CompareTo(y.item.tier));
                                await GameCommands.DisplayInventory(player, Context.Channel, 0);
                            }
                            break;
                        case "type":
                            {
                                if (direction >= 0)
                                    player.inventory.inv.Sort((x, y) => x.item.type.CompareTo(y.item.type));
                                else
                                    player.inventory.inv.Sort((y, x) => x.item.type.CompareTo(y.item.type));
                                await GameCommands.DisplayInventory(player, Context.Channel, 0);
                            }
                            break;
                        default:
                            await ReplyAsync("Available sorting:" + Environment.NewLine
                            + prefix + " Rank" + Environment.NewLine
                            + prefix + " Type" + Environment.NewLine
                            );
                            save = false;
                            break;
                    }
                    break;
                default:
                    await ReplyAsync("Available sorting:" + Environment.NewLine
                        + prefix + "Inventory" + Environment.NewLine
                        );
                    save = false;
                    break;
            }
            if (save && player != null)
                player.SaveFileMongo();
        }

        #endregion
    }
}
