using AMI.Methods;
using AMI.Module;
using AMI.Neitsillia.Crafting;
using AMI.Neitsillia.Items.Abilities;
using AMI.Neitsillia.Items.ItemPartials;
using AMI.Neitsillia.Items.Perks.PerkLoad;
using AMI.Neitsillia.Items.Scrolls;
using AMI.Neitsillia.User.PlayerPartials;
using AMI.Neitsillia.User.UserInterface;
using AMYPrototype.Commands;
using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AMI.AMIData.OtherCommands
{
    public partial class GameMaster
    {
        [Command("Grant XP")]
        [Alias("grantx")]
        public async Task GrantXP(IUser user, long xp)
        {
            if (HasFuel(xp, 1).Result)
            {
                Player player = Player.Load(user.Id, Player.IgnoreException.All);
                player.XpGain(xp);
                IUserMessage reply = await ReplyAsync(user.Mention + " gained " + xp + " XP points.");
                player.ui = new UI(reply, new List<string> { EUI.xp, EUI.inv, EUI.stats });
                player.SaveFileMongo();
                await DUtils.DeleteContextMessageAsync(Context);
                GMs.Find(GM.FindWithID(Context.User.Id)).limits[1] -= xp;
                SaveGMFile();
            }
        }
        [Command("Grant Coins")]
        [Alias("grantc")]
        public async Task GrantCoins(IUser user, long amount)
        {
            if (HasFuel(amount, 0).Result)
            {
                Player player = Player.Load(user.Id, Player.IgnoreException.All);
                player.KCoins += amount;
                player.SaveFileMongo();
                IUserMessage reply = await ReplyAsync(user.Mention + " gained " + amount + " Kutsyei coins.");
                player.ui = new UI(reply, new List<string> { EUI.xp, EUI.inv, EUI.stats });
                player.SaveFileMongo();
                await DUtils.DeleteContextMessageAsync(Context);
                GMs.Find(GM.FindWithID(Context.User.Id)).limits[0] -= amount;
                SaveGMFile();
            }
            await DUtils.DeleteContextMessageAsync(Context);
        }
        [Command("Grant Item")]
        [Alias("granti")]
        public async Task GrantItem(IUser user, string itemname, int amount = 1)
        {
            Item item = Item.LoadItem(itemname);
            if (item == null)
                await ReplyAsync("Item Not Found");
            else if (await HasFuel(item.tier, 2))
            {
                Player player = Player.Load(user.Id, Player.IgnoreException.All);
                string result = $"Could not collect {item.name}";
                if (player.CollectItem(item, amount))
                    result = $"Collected {item.name}";
                IUserMessage reply = await ReplyAsync(user.Mention + " " + result);
                player.ui = new UI(reply, new List<string> { EUI.xp, EUI.inv, EUI.stats });
                player.SaveFileMongo();
                await DUtils.DeleteContextMessageAsync(Context);
                GMs.Find(GM.FindWithID(Context.User.Id)).limits[2] -= item.tier;
                SaveGMFile();
            }
            await DUtils.DeleteContextMessageAsync(Context);
        }
        [Command("GrantUsable")]
        public async Task GrantUsable(IUser user, string usable, int level = 1, int amount = 1)
        {
            if (await HasFuel(level * 20, 2))
            {
                Player player = Player.Load(user.Id, Player.IgnoreException.All);
                switch (usable.ToLower())
                {
                    case "repairkit":
                    case "kit":
                        player.inventory.Add(Item.CreateRepairKit(level), amount, -1);
                        break;
                    case "rune":
                    case "runes":
                        player.inventory.Add(Item.CreateRune(level), amount, -1);
                        break;
                    default:
                        throw NeitsilliaError.ReplyError(string.Join(Environment.NewLine, "RepairKit, Kit", "Rune, Runes"));

                }
                player.SaveFileMongo();
                await ReplyAsync($"Gave {player.name} {amount}x {usable}");
            }
        }

        [Command("Grant Scroll")]
        public async Task GrantScroll(IUser user, [Remainder] string name)
        {
            if(await IsGMLevel(4))
            {
                Item scroll = ScrollsManager.Load(name);
                if (scroll == null) return;

                Player player = Player.Load(user.Id, Player.IgnoreException.All);
                player.inventory.Add(scroll, 1, -1);
                await ReplyAsync($"{scroll} given to {player}");
            }
        }

        [Command("Grant Upgraded Gear"), Alias("grantug")]
        public async Task GrantUpgradedGear(IUser user, int tier, params string[] namearg)
        {
            if (await IsGMLevel(4))
            {
                Item item = Item.LoadItem(ArrayM.ToUpString(namearg));
                if (tier > item.tier)
                    item.Scale(tier);

                Player player = Player.Load(user.Id, Player.IgnoreException.All);
                string result = $"Could not collect {item.name}";
                if (player.CollectItem(item, 1))
                    result = $"Collected {item.name}";
                IUserMessage reply = await ReplyAsync(user.Mention + " " + result);
                player.ui = new UI(reply, new List<string> { EUI.xp, EUI.inv, EUI.stats });
                player.SaveFileMongo();
                await DUtils.DeleteContextMessageAsync(Context);
            }
        }
        [Command("Grant Schematic")]
        [Alias("grants")]
        public async Task Grant_Schem(IUser user, params string[] argName)
        {
            if (await IsGMLevel(3))
            {
                string schemName = StringM.UpperAt(ArrayM.ToString(argName));
                Item item = Item.LoadItem(schemName);
                if (item.type != Item.IType.notfound)
                {
                    if (item.schematic.exists)
                    {
                        Player player = Player.Load(user.Id, Player.IgnoreException.All);
                        if (player.schematics == null)
                            player.schematics = new List<Schematic>();
                        if (player.schematics.FindIndex(Schematic.FindWithName(item.schematic.name)) == -1)
                        {
                            player.schematics.Add(item.schematic);
                            player.SaveFileMongo();
                            IUserMessage reply = await ReplyAsync(user.Mention + " received a " + schemName + " schematic");
                            player.ui = new UI(reply, new List<string> { EUI.xp, EUI.inv, EUI.stats, EUI.schem });
                            player.SaveFileMongo();
                            await DUtils.DeleteContextMessageAsync(Context);
                        }
                        else await DUtils.Replydb(Context, "Player already has this schematic");
                    }
                    else await DUtils.Replydb(Context, "Item does not have a schematic");

                }
                else await DUtils.Replydb(Context, "Item not found");
            }
        }
        [Command("Grant Temporary Schematic")]
        [Alias("grantts")]
        public async Task GrantTempSchem(IUser user, params string[] argName)
        {
            if (await IsGMLevel(3))
            {
                Item item = Item.NewTemporarySchematic(ArrayM.ToString(argName));
                if (item == null)
                    await ReplyAsync("Item Not Found");
                else if (HasFuel(item.tier, 2).Result)
                {
                    Player player = Player.Load(user.Id);
                    string result = $"Could not collect {item.name}";
                    if (player.CollectItem(item, 1))
                        result = $"Collected {item.name}";
                    IUserMessage reply = await ReplyAsync(user.Mention + " " + result);
                    player.ui = new UI(reply, new List<string> { EUI.xp, EUI.inv, EUI.stats });
                    player.SaveFileMongo();
                    await DUtils.DeleteContextMessageAsync(Context);
                    GMs.Find(GM.FindWithID(Context.User.Id)).limits[2] -= item.tier;
                    SaveGMFile();
                }
                await DUtils.DeleteContextMessageAsync(Context);
            }
        }
        [Command("Grant Ability")]
        [Alias("granta")]
        public async Task Grant_Ability(IUser user, string argName)
        {
            if (await IsGMLevel(3))
            {
                string abName = StringM.UpperAt(argName);
                Ability a = Ability.Load(abName);
                Player p = Player.Load(user.Id, Player.IgnoreException.All);
                if (!p.HasAbility(a.name, out _))
                {
                    p.abilities.Add(a);
                    p.SaveFileMongo();
                    await ReplyAsync($"{p.name} learned {a.name}");
                }
                else await ReplyAsync($"{p.name} already knows {a.name}");
            }
        }

        [Command("Grant Ability XP")]
        [Alias("grantax")]
        public async Task Grant_Ability_XP(IUser user, string argName, long xp = 100)
        {
            if (await IsGMLevel(3))
            {
                string abName = StringM.UpperAt(argName);
                Player p = Player.Load(user.Id, Player.IgnoreException.All);
                if (p.HasAbility(abName, out int i))
                {
                    p.abilities[i].GainXP(xp, 1);
                    p.SaveFileMongo();
                    await DUtils.DeleteContextMessageAsync(Context);
                    await ReplyAsync(embed: p.abilities[i].InfoPage(p.UserEmbedColor(), true).Build());
                }
                else await DUtils.Replydb(Context, $"{p.name} already does not know {argName}", lifetime: 0.5);
            }
        }
        [Command("Grant Perk")]
        [Alias("grantp")]
        public async Task Grant_Perk(IUser user, params string[] argName)
        {
            if (await IsGMLevel(3))
            {
                string perkName = StringM.UpperAt(ArrayM.ToString(argName));
                Player p = Player.Load(user.Id, Player.IgnoreException.All);
                int i = p.HasPerk(perkName);
                if (i == -1)
                {
                    p.perks.Add(PerkLoad.Load(perkName));
                    p.SaveFileMongo();
                    await DUtils.DeleteContextMessageAsync(Context);
                }
                else await DUtils.Replydb(Context, $"{p.name} already has the perk {perkName}", lifetime: 0.5);
            }
        }

        [Command("Give Skill Point")]
        public async Task Give_SKill_Point(IUser user, int amount = 1)
        {
            if (await IsGMLevel(4))
            {
                Player player = Player.Load(user.Id, Player.IgnoreException.All);
                player.skillPoints += amount;
                player.SaveFileMongo();
            }
        }

        [Command("Give Spec Point")]
        [Alias("grantspecp")]
        public async Task Give_Spec_Point(IUser user, int amount = 1)
        {
            if (await IsGMLevel(4))
            {
                Player player = Player.Load(user.Id, Player.IgnoreException.All);
                if (player.Specialization != null)
                {
                    player.Specialization.specPoints += amount;
                    player.SaveFileMongo();
                }
            }
        }

        [Command("GrantEgg")]
        [Alias("grante")]
        public async Task Grant_Egg(IUser user, int tier = 0)
        {
            if (await IsGMLevel(4))
            {
                Player player = Player.Load(user.Id, Player.IgnoreException.All);
                if (player.EggPocket == null)
                    await ReplyAsync("Player has no egg pocket");
                else
                    await player.EggPocket.EquippEgg(Neitsillia.NPCSystems.Companions.EggTypes
                        .GetEgg(tier), player, Context.Channel);
            }
        }
        [Command("HatchEgg")]
        public async Task Grant_Pet(IUser user)
        {
            if (await IsGMLevel(4))
            {
                Player player = Player.Load(user.Id, Player.IgnoreException.All);
                if (player.EggPocket?.egg == null)
                    await ReplyAsync("Player has no egg");
                else
                    player.EggPocket.Hatch(player);
            }
        }

        [Command("ApplyPerk")]
        public async Task ApplyPerk(int index, string perkName)
        {
            if (await IsGMLevel(4))
            {
                Item item = Context.Player.inventory.GetItem(index - 1);
                if (item != null && item.CanBeEquip())
                {
                    item.perk = PerkLoad.Load(perkName);
                    Context.Player.SaveFileMongo();
                    await ReplyAsync("Perks replaced");
                }
                else await ReplyAsync("item is not gear");
            }
        }
    }
}
