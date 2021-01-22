using AMI.Commands;
using AMI.Methods;
using AMI.Module;
using AMI.Neitsillia.Collections;
using AMI.Neitsillia.Crafting;
using AMI.Neitsillia.Encounters;
using AMI.Neitsillia.Items;
using AMI.Neitsillia.Items.Perks.PerkLoad;
using AMI.Neitsillia.User.PlayerPartials;
using AMI.Neitsillia.User.UserInterface;
using AMYPrototype;
using AMYPrototype.Commands;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Neitsillia.Items.Item;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AMI.Neitsillia.Commands.InventoryCommands
{
    public class Crafting : ModuleBase<CustomSocketCommandContext>
    {
        [Command("Craft")]
        [Summary("Craft an item from your `Schematics` list. \n Example: ~Craft Wooden Spear")]
        public async Task ICraft(
            [Summary("Full name of the item you wish to craft")]
            params string[] itemToCraft)
        {
            if (itemToCraft.Length < 1)
                throw NeitsilliaError.ReplyError("No item name was given. ex: ``~craft Wooden Spear``  use ``~schematics`` to view craftable items.");

            if (itemToCraft.Length == 1 && int.TryParse(itemToCraft[0], out int err))
                throw NeitsilliaError.ReplyError($"If you are trying to craft an item using a schematic in your inventory, use ``~use {err}``.");

            int countIndex = int.TryParse(itemToCraft[0], out int amount) ? 0 : int.TryParse(itemToCraft[itemToCraft.Length - 1], out amount) ? itemToCraft.Length - 1 : -1;
            string itemname = StringM.UpperAt(ArrayM.ToKString(itemToCraft, " ", countIndex));
            Player player = Context.Player;
            if (itemname == null)
                await ReplyAsync($"{player.name} must enter the name of the schematic.");
            else if (player.HealthStatus(out string hpstate) < -1)
                await ReplyAsync($"{player.name} may not craft an item while in a {hpstate} state.");
            else if (player.IsEncounter("Combat"))
                await DUtils.Replydb(Context, "You may not craft during combat.");
            else
            {
                int sIndex = player.KnowsSchematic(itemname);
                if (sIndex == -1)
                    await DUtils.Replydb(Context, $"{player.name} does not know schematic for {itemname}");

                else
                {
                    Schematic schem = player.schematics[sIndex];
                    string details = null;
                    EmbedBuilder crafting = new EmbedBuilder();
                    player.UserEmbedColor(crafting);
                    crafting.WithTitle("Crafting " + schem.name);
                    Item ret = schem.Craft(player, ref details);
                    if (ret != null)
                    {
                        if (!player.IsRequiredLevel(ret.baseTier))
                            await DUtils.Replydb(Context, $"{player.name} is too low level to craft {itemname}. Requires level {Math.Ceiling(ret.baseTier / 5.00)}");
                        else
                        {
                            player.XpGain(ret.tier * 10 * player.level);

                            ret.EmdebInfo(crafting);
                            await DUtils.Replydb(Context, Context.User.Mention + " " + details,
                                crafting.Build());
                            player.EggPocketTrigger(Neitsillia.NPCSystems.Companions.Egg.EggChallenge.Crafting);
                            player.QuestTrigger(Neitsillia.Items.Quests.Quest.QuestTrigger.Crafting, ret.originalName);

                            await Neitsillia.InventoryCommands.Inventory.UpdateinventoryUI(player, Context.Channel);
                        }
                    }
                    else
                    {
                        crafting.AddField("Item could not be crafted:", details);
                        await DUtils.Replydb(Context, Context.User.Mention, crafting.Build());
                    }
                }
            }
        }

        [Command("Scrap")]
        [Alias("scrap")]
        [Summary("Scrap gear or consumables to get some of the materials used to craft the item and a small chance to learn its schematic."
            + "\n Example: `~scrap 1x5` will scrap 5 of the items in slot 1 of your inventory.")]
        public async Task IScrap(string indexXamount)
        {
            var ia = Verify.IndexXAmount(indexXamount);
            int index = ia.index - 1;
            Player player = Context.GetPlayer(Player.IgnoreException.Resting);
            if (player.Encounter != null && player.Encounter.IsCombatEncounter())
                await DUtils.Replydb(Context, Context.User.Mention + ", you may not scrap items during combat.");
            else if (index >= player.inventory.Count || index < 0)
                await DUtils.Replydb(Context, "Invalid Item");
            else if (player.inventory.GetItem(index) is Item item &&
                (item.isUnique || item.schematic == null || !item.schematic.exists || item.type == Item.IType.Schematic))
                await DUtils.Replydb(Context, "Item can not be dismantled");
            else
            {
                int count = Verify.MinMax(ia.amount, player.inventory.GetCount(index), 1);
                await DismantlingLoot(player, player.inventory.GetItem(index), count);
                player.inventory.Remove(index, count);
                if (player.ui != null && player.ui.type == MsgType.ConfirmUpgrade)
                    await player.ui.TryDeleteMessage();
                await Neitsillia.InventoryCommands.Inventory.UpdateinventoryUI(player, Context.Channel);
            }
        }
        async Task DismantlingLoot(Player player, Item i, int count)
        {
            Random r = Program.rng;
            Inventory l = new Inventory();

            player.XpGain(i.tier * player.level);

            AddScrapLoot(l, i, count, player, r);

            bool canCollect = true;
            for (int a = 0; a < l.Count && canCollect; a++)
                canCollect = player.inventory.CanContain(l.inv[a], player.InventorySize());

            await MoreDismantlingMethod(player, l, canCollect);
            bool schemGained = await CheckSchemGain(player, i, r, Verify.Min(count / 75.001, 1));

            player.QuestTrigger(Items.Quests.Quest.QuestTrigger.Scrapping, $"{i.name};{count};{schemGained}");
        }
        void AddScrapLoot(Inventory l, Item i, int count, Player player, Random r)
        {
            double cnd = 50;
            if (i.CanBeEquip())
                cnd = ((i.condition * 100.00) / i.durability);
            Schematic s = i.schematic;
            int playerInventorySize = player.InventorySize();
            for (int a = 0; s.underMat != null && a < s.underAmount; a++)
                if (cnd >= r.Next(101))
                    l.Add(Item.LoadItem(s.underMat), count, playerInventorySize);
            for (int a = 0; s.mainMat != null && a < s.mainAmount; a++)
                if (cnd >= r.Next(101))
                    l.Add(Item.LoadItem(s.mainMat), count, playerInventorySize);
            for (int a = 0; s.tyingMat != null && a < s.tyingAmount; a++)
                if (cnd >= r.Next(101))
                    l.Add(Item.LoadItem(s.tyingMat), count, playerInventorySize);
            for (int a = 0; s.secondaryMat != null && a < s.secondaryAmount; a++)
                if (cnd >= r.Next(101))
                    l.Add(Item.LoadItem(s.secondaryMat), count, playerInventorySize);
            for (int a = 0; s.specialMat != null && a < s.specialAmount; a++)
                if (cnd >= r.Next(101))
                    l.Add(Item.LoadItem(s.specialMat), count, playerInventorySize);
            for (int a = 0; s.skaviMat != null && a < s.skaviAmount; a++)
                if (cnd >= r.Next(101))
                    l.Add(Item.LoadItem(s.skaviMat), count, playerInventorySize);
        }
        async Task<bool> CheckSchemGain(Player player, Item i, Random r, double extraSchemChance = 0)
        {
            double cnd = 50;
            if (i.CanBeEquip())
                cnd = ((i.condition * 100.00) / i.durability);
            double schemChance = r.Next(101) - (player.stats.GetINT() * Stats.SchemDropRatePerInt) - extraSchemChance -
                (player.schematics.Count < 3 ? 10 : 0);
            if (player.schematics == null)
                player.schematics = new List<Schematic>();
            if ((cnd / 20) >= schemChance)
            {
                if (player.schematics.Find(Schematic.FindWithName(i.originalName)) == null)
                {
                    player.schematics.Add(i.schematic);
                    await DUtils.Replydb(Context, $" {player.name} has discovered how to craft {i.name}");
                }
                else
                {
                    Item sc = Item.NewTemporarySchematic(i);
                    if (player.CollectItem(sc, 1, true))
                        await DUtils.Replydb(Context, $" {player.name} collected {sc.name}");
                    else
                    {
                        if (player.Encounter == null)
                            player.NewEncounter(new Encounter(Encounter.Names.Loot, player));
                        player.Encounter.AddLoot(sc);
                        await DUtils.Replydb(Context, $" {player.name} inventory full, {sc.name} added to loot instance.");
                    }
                }
                return true;
            }
            return false;
        }
        async Task MoreDismantlingMethod(Player player, Inventory list, bool canCollect)
        {
            if (canCollect)
            {
                EmbedBuilder loot = new EmbedBuilder();
                player.UserEmbedColor(loot);
                string results = null;
                foreach (StackedItems i in list)
                    if (player.CollectItem(i))
                        results += i + Environment.NewLine;
                if (results == null)
                    results = "None";
                loot.AddField("Materials Collected", results);
                loot.WithTitle($"Dismantling");
                await DUtils.Replydb(Context, "", embed: loot.Build());
            }
            else
            {
                if (player.Encounter == null)
                    player.NewEncounter(new Encounter(Encounter.Names.Loot, player));
                foreach (StackedItems i in list)
                    player.Encounter.AddLoot(i);
                await DUtils.Replydb(Context, $"You successfully dismantle the item but your inventory may not contain " +
                    $"everything that was retrieved and was instead added to your loot");
            }

        }

        [Command("BulkScrap"), Alias("bscrap")]
        [Summary("Scrap gear or consumables to get some of the materials used to craft the item and a small chance to learn its schematic."
            + "\n Example: `~scrap 1x5 2x3 ...` will scrap 5 of the items in slot 1 of your inventory and so on.")]
        public async Task Bulk_Scrap(params string[] slots)
        {
            Player player = Context.GetPlayer(Player.IgnoreException.Resting);
            if (player.Encounter != null && player.Encounter.IsCombatEncounter())
                await DUtils.Replydb(Context, Context.User.Mention + ", you may not scrap items during combat.");
            else if (slots.Length < 1)
                await ReplyAsync($"Missing item indexes, format example: ``~bulcscrap 1 5 7 6`` will scrap inventory item 1, 5, 6 and 7.");
            else if (slots.Length > 15)
                await ReplyAsync($"You may not bulk scrap more than 15 items at once.");
            else
            {
                List<(int index, int amount)> i = Verify.IndexXAmount(slots);

                Random r = Program.rng;
                if (player.Encounter == null)
                    player.NewEncounter(new Encounter(Encounter.Names.Loot, player));
                long xp = 0;
                for (int c = 0; c < i.Count; c++)
                {
                    int index = i[c].index;

                    if (index < player.inventory.Count && index > -1 && i[c].amount > 0 &&
                        (c == 0 || index != i[c - 1].index))
                    {
                        Item item = player.inventory.GetItem(index);
                        int amount = Math.Min(i[c].amount, player.inventory.GetCount(index));

                        if (item?.schematic != null)
                        {
                            xp += item.tier * player.level;
                            AddScrapLoot(player.Encounter.loot, item, amount, player, r);
                            bool schemGained = await CheckSchemGain(player, item, r);
                            player.inventory.Remove(index, amount);

                            player.QuestTrigger(Items.Quests.Quest.QuestTrigger.Scrapping,
                                $"{item.name};{amount};{schemGained}", false);
                        }
                    }
                }
                player.XpGain(xp);
                await Neitsillia.InventoryCommands.Inventory.ViewLoot(player, Context.Channel, 0);
            }
        }

        [Command("upgrade")]
        [Alias("iup")]
        [Summary("Used to upgrade gear using materials. \n" +
            " Example: `~upgrade 1 2*5` will upgrade gear in slot 1 of your inventory using 5 or the material in slot 2.")]
        public async Task IUpgrade(
            [Summary("Inventory slot number of the item to upgrade")]
            int slotOfItemToUpgrade,
            [Summary("Inventory slot number and amount of the material to upgrade with")]
            string indexXamount)
        {
            var (index, amount) = Verify.IndexXAmount(indexXamount);
            int indexOfMaterialToUse = index - 1;
            int amountOfMaterialsToUse = amount;
            Player player = Context.Player;
            Item irec = player.inventory.GetItem(slotOfItemToUpgrade - 1);
            Item igiv = player.inventory.GetItem(indexOfMaterialToUse);
            if (player.Encounter != null && player.Encounter.IsCombatEncounter())
                await DUtils.Replydb(Context, Context.User.Mention + ", you may not upgrade items during combat.");
            else if (irec == null || igiv == null)
                DUtils.DeleteMessage(await ReplyAsync("Invalid selection"), 0.5);
            else if (igiv.type != Item.IType.Material)
                DUtils.DeleteMessage(await ReplyAsync("You may only upgrade using materials"), 0.5);
            else if (irec.CanBeEquip()) //Is Gear
            {
                int i = player.KnowsSchematic(irec.originalName, irec.name);
                if (i == -1)
                    await DUtils.Replydb(Context,
                        $"{player.name}, You may not modify something you do not understand ({irec.originalName})", lifetime: 0.2);
                else
                {
                    amountOfMaterialsToUse = Verify.Max(amountOfMaterialsToUse, player.inventory.GetCount(indexOfMaterialToUse));
                    List<float> stats = igiv.GetStatList(irec.type == Item.IType.Weapon);
                    int[] mods = IMethods.GetUpgrades(stats.ToArray(), (irec.type == Item.IType.Weapon));
                    int newTier = IMethods.GetNewTier(irec, stats, mods, amountOfMaterialsToUse);

                    if (!player.IsRequiredLevel(newTier))
                        await DUtils.Replydb(Context,
                        $"{player.name}, you may not upgrade this item to a rank higher than {player.level * 5} at your current level.", lifetime: 0.2);
                    else if (newTier > irec.baseTier + 15)
                        await DUtils.Replydb(Context,
                        $"{player.name}, you may not upgrade this item so drastically.", lifetime: 0.2);
                    else if (irec.tier >= irec.baseTier + 15)
                        await DUtils.Replydb(Context,
                        $"{player.name}, this item may no longer be upgraded.", lifetime: 0.2);
                    else
                    {
                        List<string> descs = new List<string> {"Health","Durability","Agility",
                        "Critical Chance","Critical Damage", "Stamina"};
                        foreach (string d in ReferenceData.DmgType)
                        {
                            if (irec.type == Item.IType.Weapon)
                                descs.Add(d + " Damage");
                            else
                                descs.Add(d + " Resistance");
                        }//
                        EmbedBuilder info = player.UserEmbedColor(new EmbedBuilder());
                        info.WithTitle($"Upgrade => {irec.name} With {amountOfMaterialsToUse}x {igiv.name}");
                        //
                        long price = UpgradeCost(irec.tier, igiv.tier, amountOfMaterialsToUse);
                        info.WithDescription($"Extra Cost: {price}~~K~~oins");
                        info.AddField(irec.name, irec.StatsInfo(), true);
                        string modificationInfo = $"+ {newTier - irec.tier} Rank{Environment.NewLine}" +
                            $"+ {stats[mods[1]] * amountOfMaterialsToUse} {descs[mods[1]]}" + Environment.NewLine;
                        //
                        if (mods[0] > -1)
                            modificationInfo += $"{stats[mods[0]] * amountOfMaterialsToUse} {descs[mods[0]]}";
                        info.AddField(igiv.name, modificationInfo, true);
                        await DUtils.DeleteContextMessageAsync(Context);
                        player.ui = new UI(await ReplyAsync(embed: info.Build()),
                            MsgType.ConfirmUpgrade, player)
                        {
                            data = JsonConvert.SerializeObject(
                            new int[] { slotOfItemToUpgrade - 1, indexOfMaterialToUse, mods[0], mods[1], amountOfMaterialsToUse })
                        };
                        player.SaveFileMongo();
                    }
                }
            }
            else DUtils.DeleteMessage(await ReplyAsync("You may only upgrade gear"), 0.5);
        }

        internal static async Task ProceedItemUpgrade(Player player, ISocketMessageChannel channel, string data)
        {
            int[] i = JsonConvert.DeserializeObject<int[]>(data);
            int amount = 1;
            if (i.Length >= 5)
                amount = i[4];
            Item irec = player.inventory.GetItem(i[0]);
            Item igiv = player.inventory.GetItem(i[1]);
            long price = UpgradeCost(irec.tier, igiv.tier, amount);
            if (irec == null || igiv == null)
                DUtils.DeleteMessage(await channel.SendMessageAsync("Invalid selection"), 0.5);
            else if (player.KCoins < price)
                DUtils.DeleteMessage(await channel.SendMessageAsync("Insufficient funds"), 0.5);
            else
            {
                List<string> descs = new List<string> {"Health","Durability","Agility",
                    "Critical Chance","Critical Damage", "Stamina"}; descs.AddRange(ReferenceData.DmgType);
                irec.Upgrade(amount, igiv, i[2], i[3]);
                PerkLoad.CheckPerks(player, Perk.Trigger.Upgrading, irec, amount, igiv.tier);
                player.inventory.Remove(i[1], amount);
                player.KCoins -= price;
                player.XpGain(price * player.level);

                player.EggPocketTrigger(Neitsillia.NPCSystems.Companions.Egg.EggChallenge.Crafting);
                player.QuestTrigger(Neitsillia.Items.Quests.Quest.QuestTrigger.GearUpgrading);

                DUtils.DeleteMessage(await channel.SendMessageAsync(embed:
                    irec.EmdebInfo(player.UserEmbedColor(new EmbedBuilder())).Build()), 0.3);
            }
        }
        internal static long UpgradeCost(int receiverTier, int giverTier, int giveramoutn) => (receiverTier + (giverTier * giveramoutn)) * 8;

        [Command("IRename")]
        [Alias("irem")]
        [Summary("Rename an item. Must have been upgraded.")]
        public async Task IRename(
            [Summary("Inventory slot number of the item to rename")]
            int slot,
            [Summary("New name")]
            params string[] newItemName)
        {
            slot--;
            Player p = Context.Player;
            Item item = p.inventory.GetItem(slot);
            string prefix = Context.Prefix;
            if (item == null)
                DUtils.DeleteMessage(await ReplyAsync("Invalid selection"), 0.5);
            else if (item.tier - item.baseTier <= 10)
                DUtils.DeleteMessage(await ReplyAsync("Gear may only be renamed once they reach more than 10 ranks above their base rank."), 0.5);
            else if (newItemName.Length < 1)
                DUtils.DeleteMessage(await ReplyAsync($"You must enter a new name for your item: `{prefix}IRename #slot newItemName"), 0.5);
            else
            {
                string rename = StringM.UpperFormat(ArrayM.ToString(newItemName));
                if (rename == null)
                    DUtils.DeleteMessage(await ReplyAsync($"You must enter a new item name: ``{prefix}iRename 13 The Slayer``"), 0.5);
                else if (rename.Length > 30)
                    DUtils.DeleteMessage(await ReplyAsync("Name must be less than 36 characters"), 0.5);
                else if (!Regex.Match(rename, @"^([a-zA-Z]|'|-|’|\s)+$").Success)
                    DUtils.DeleteMessage(await ReplyAsync("Name must only contain A to Z, (-), ('), (’) and spaces"), 1);
                else
                {
                    item.Rename(rename);
                    p.SaveFileMongo();
                    DUtils.DeleteMessage(await ReplyAsync(embed: item.EmdebInfo(p.UserEmbedColor(new EmbedBuilder())).Build()));
                }
            }
        }

        [Command("IRepair")]
        [Alias("Repair", "Irep")]
        [Summary("Attempts to repair an item using materials. Requires the item's schematic in the character's `Schematics` list")]
        public async Task IRepair(
            [Summary("Inventory slot number of the item to repair")]
            int slot)
        {
            slot--;
            Player player = Context.Player;
            if (player.Encounter != null && player.Encounter.IsCombatEncounter())
                await DUtils.Replydb(Context, Context.User.Mention + ", you may not repair items during combat.");
            else if (slot > player.inventory.Count - 1)
                await DUtils.Replydb(Context, "Invalid index");
            else
            {
                Item item = player.inventory.GetItem(slot);
                if (!item.CanBeEquip())
                    await DUtils.Replydb(Context, "Item can not be repaired");
                else if (player.KnowsSchematic(item.originalName) == -1)
                    await DUtils.Replydb(Context, $"You may not repair an item you do not understand. [Requires {item.originalName} Schematic]");
                else if (item.condition == item.durability)
                    await DUtils.Replydb(Context, $"Item is already in its best condition.");
                else
                {
                    int materialMultiplier = NumbersM.FloorParse<int>((item.tier - item.baseTier) / 5.00) + 1;
                    string missingMaterials = item.schematic.CheckPlayerInventoryForMaterial(player, materialMultiplier);
                    if (missingMaterials != null)
                        await DUtils.Replydb(Context, missingMaterials);
                    else
                    {
                        item.condition = item.durability;
                        item.schematic.ConsumeSchematicItems(player, materialMultiplier);
                        await Neitsillia.InventoryCommands.Inventory.UpdateinventoryUI(player, Context.Channel);
                        await DUtils.Replydb(Context, $"{player.name} repaired their {item.name}.");
                    }
                }
            }
        }
    }
}
