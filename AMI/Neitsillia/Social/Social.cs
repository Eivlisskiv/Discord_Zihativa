using AMI.Methods;
using AMI.Module;
using AMI.Neitsillia.Combat;
using AMI.Neitsillia.NeitsilliaCommands.Social;
using AMI.Neitsillia.Social.Mail;
using AMI.Neitsillia.User;
using AMI.Neitsillia.User.PlayerPartials;
using AMI.Neitsillia.User.UserInterface;
using AMYPrototype.Commands;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using AMI.Neitsillia.Items.ItemPartials;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AMI.Neitsillia.NeitsilliaCommands
{
    public class SocialCommands : ModuleBase<AMI.Commands.CustomCommandContext>
    {
        [Command("mail")] [Alias("inbox")]
        public async Task Viewinbox(int page = 0)
            => await ViewInbox(Context.BotUser, page - 1, Context.Channel, false);

        public static async Task ViewInbox(BotUser user, int page, IMessageChannel chan, bool edit)
        {
            Mail[] mails = await Mail.Load(user._id);
            if (mails.Length == 0) 
            { 
                await chan.SendMessageAsync("Your inbox is empty");
                return;
            }

            EmbedBuilder embed = DUtils.BuildEmbed("Inbox", $"**Rewards will be given to the currently loaded character: {user.loaded}**");
            page = page < 0 ? 0 : Math.Min(page, (mails.Length - 1) / 5);

            int n = 1;
            int i = page * 5; 
            int l = Math.Min(i + 5, mails.Length);
            string ids = $"{page};";
            for(; i < l; i++, n++)
            {
                Mail mail = mails[i];
                string rewards = mail.GetRewards();
                embed.AddField($"{EUI.GetNum(n)} {mail.subject}",
                    mail.body + (rewards != null ? Environment.NewLine + Environment.NewLine + rewards : null));
                ids += $"{mail._id}";
                if (i + 1 < l) ids += ",";
            }

            if (!edit) user.NewUI(await chan.SendMessageAsync(embed: embed.Build()),
                MsgType.Inbox, ids);
            else await user.EditUI(null, embed.Build(), MsgType.Inbox, ids);
        }

        //Party Commands

        [Command("Party Info")]
        [Alias("PartyI", "pinfo")]
        public async Task Party_Info()
        {
            Player p = Player.Load(Context.BotUser, Player.IgnoreException.Resting);
            if (p.Party == null)
                await DUtils.Replydb(Context, $"{p.name} is not in a party");
            else await DUtils.Replydb(Context, embed: p.Party.EmbedInfo(), lifetime: 3);
        }

        [Command("Party Invite")]
        [Alias("pinv")]
        public async Task Party_Invite(IUser user)
        {
            Player p = Context.Player;
            Player up = Player.Load(user.Id, Player.IgnoreException.All);
            if (p.Party == null)
                await DUtils.Replydb(Context, $"{p.name}, you must be in a party to invite another adventurer.");
            else if (up.Party != null)
                await DUtils.Replydb(Context, $"{up.name} is already in a party.");
            else if (p.Party.MemberCount >= p.Party.maxPartySize)
                await DUtils.Replydb(Context, $"{p.Party.partyName} is full");
            else if (p.IsEncounter("Combat") || p.IsEncounter(Encounters.Encounter.Names.Puzzle))
                await DUtils.Replydb(Context, $"Players can't join you during Combat or Puzzles");
            else
            {
                await up.NewUI(await DUtils.Replydc(Context, $"{user.Mention}, you have been invited to join " +
                      $"({Context.User.Username}) {p.name}'s  Party: {p.Party.partyName}"), MsgType.PartyInvite,
                      $"{p.userid}\\{p.name}");
                p.QuestTrigger(Items.Quests.Quest.QuestTrigger.QuestLine, "partyinvite");
            }
        }

        [Command("Party Kick")]
        [Summary("Use this command to kick other players from the party. Use `Follower Kick` to kick NPCs from the party.")]
        public async Task Party_Kick(IUser user)
        {
            if (Context.Player.Party == null) await ReplyAsync("You are not in a party.");
            if (Context.Player.IsSolo) await ReplyAsync("Use `Follower kick` to kick followers");
            else if (!Context.Player.IsLeader) await ReplyAsync("You are not leader.");
            else if (Context.Player.IsEncounter("Loot")) await ReplyAsync("You may not kick players during the loot phase.");
            else if (Context.Player.ui?.type == MsgType.CardGame)
                await ReplyAsync("You may not kick players during this mini game.");
            else
            {
                PartyMember member = Context.Player.Party.members.Find(m => m.id == user.Id);
                if (member == null) await ReplyAsync("This user has no character in this party.");
                else
                {
                    Player p = member.LoadPlayer();
                    await Context.Player.Party.Remove(p);
                    p.SaveFileMongo();
                    await ReplyAsync($"{p.name} was kicked from the party.");
                }

            }
        }

        [Command("Create Party")]
        [Alias("cparty", "party create")]
        public async Task Create_Party(params string[] name)
        {
            string partyName = ArrayM.ToString(name);
            Player player = Context.Player;
            if (player.Party != null)
            {
                await DUtils.Replydb(Context, $"You are already in a party: {player.Party.partyName}", lifetime: 1);
                player.QuestTrigger(Items.Quests.Quest.QuestTrigger.QuestLine, "party");
            }
            else if (player.IsEncounter("combat"))
                await DUtils.Replydb(Context, $"You can't start a party while in combat.", lifetime: 1);
            else if (name.Length < 1 || partyName.Length < 3 || partyName.Length > 30)
                await DUtils.Replydb(Context, $"Party name must be between 5 and 30 characters long.", lifetime: 1);
            else if (!Regex.Match(partyName, @"^([a-zA-Z]|'|-|’|\s)+$").Success)
                await DUtils.Replydb(Context, $"Party name must only contain A to Z, (-), ('), (’) and spaces.", lifetime: 1);
            else if (AMYPrototype.Program.data.database.IdExists<Party, string>("Party", partyName.ToLower()))
                await DUtils.Replydb(Context, $"Party name already taken", lifetime: 1);
            else
            {
                new Party(partyName, player);

                player.QuestTrigger(Items.Quests.Quest.QuestTrigger.QuestLine, "party");

                if (player.Encounter != null) await player.EncounterKey.ChangeId(player.Party.EncounterKey);

                await player.Party.SaveData();
                player.SaveFileMongo();

                await DUtils.Replydb(Context, $"Party Created {player.Party.partyName}");
            }  
        }

        [Command("Leave Party")]
        [Alias("lparty", "party leave")]
        public async Task Leave_Party()
        {
            Player p = Player.Load(Context.User.Id, Player.IgnoreException.All);
            p.LoadCheck(false, Player.IgnoreException.Resting, Player.IgnoreException.MiniGames);

            if (p.Party == null)
                await DUtils.Replydb(Context, $"{p.name} is not in a party");
            else
            {
                string name = p.Party.partyName;
                await p.Party.Remove(p);
                string cost = null;
                if (p.IsEncounter("Combat"))
                    cost = Environment.NewLine + "Party left during combat: " + 
                        CombatEndHandler.DefeatCost(p, p.Encounter.mobs[AMYPrototype.Program.rng.Next(p.Encounter.mobs.Length)]);
                p.EncounterKey?.Null();
                p.PartyKey = null;
                p.SaveFileMongo();
                await DUtils.Replydb(Context, $"{p.name} has left {name}{cost}");
                if (p.IsResting) await Commands.Areas.EndRest(p, Context.Channel);
            }
        }


        [Command("Followers")][Alias("follower", "foll")]
        [Summary("Action related to the NPCs in your party.")]
        public async Task FollowerAction(
            [Summary("inspect, give, interact or kick")]
            string action = "inspect", 
            [Summary("The slot of the npc you want to apply the action to. `pinfo` command for party member list.")]
            int followerSlot = 0,
            [Summary("Only needed for `give` action: {slot}x{amount} of your inventory item(s) to give")]
            string argument = null)
        {
            await FollowerAction(Player.Load(Context.BotUser), action, followerSlot - 1, 
                argument, Context.Channel);
        }
        internal static async Task FollowerAction(Player player, string action, 
            int slot, string argument, ISocketMessageChannel chan)
        {
            if (player.Party != null && player.Party.NPCMembers.Count > 0)
            {
                string message = null;
                EmbedBuilder embed = null;
                List<NPCSystems.NPC> followers = player.Party.NPCMembers;
                if (player.IsEncounter("Npc"))
                    player.Party.UpdateFollower(player.Encounter.npc);
                
                MsgType? uiType = null;
                string uidata = null;

                switch (action.ToLower())
                {
                    case "inspect":
                    case "i":
                        {
                            embed = new EmbedBuilder();
                            if (slot > -1 && slot < followers.Count)
                                embed.WithDescription(followers[slot].NPCInfo(true, true, getEq: true));
                            else
                            {
                                int i = 1;
                                foreach (NPCSystems.NPC npc in followers)
                                {
                                    embed.AddField($"[{i}] {npc.name}",
                                          npc.NPCInfo(true));
                                    i++;
                                }
                            }
                        }
                        break;
                    case "give":
                        {
                            if (slot > -1 && slot < followers.Count)
                            {
                                (int index, int amount) = Verify.IndexXAmount(argument);
                                index--;
                                if (index <= -1 && index >= player.inventory.Count)
                                    message = "Invalid inventory slot";
                                else
                                {
                                    amount = Math.Min(amount, player.inventory.GetCount(index));
                                    followers[slot].AddItemToInv(player.inventory.GetItem(index), amount);

                                    message = $"Gave {followers[slot].name} {amount}x {player.inventory.GetItem(index)}";
                                    player.inventory.Remove(index, amount);
                                    player.SaveFileMongo();
                                    await player.Party.SaveData();

                                    embed = new EmbedBuilder();
                                    embed.WithDescription(followers[slot].NPCInfo(true, true, getEq: true));
                                }
                            }
                            else message = "Follower slot is invalid.";
                        }
                        break;
                    case "interact":
                        {
                            if (slot < 0 && slot >= followers.Count)
                                message = "Follower slot is invalid.";
                            if (player.IsEncounter("Combat"))
                                message = "You may not interact with a follower during combat";
                            else
                            {
                                if (player.Encounter?.npc?.name != followers[slot].name)
                                {
                                    player.NewEncounter(new Encounters.Encounter(true)
                                    {
                                        Name = Encounters.Encounter.Names.NPC,
                                        npc = followers[slot],
                                    }, true);
                                    player.SaveFileMongo();
                                }
                                uiType = MsgType.NPC;
                                embed = player.Encounter.npc.NPCInfo(player.UserEmbedColor(), true, false, false, false);
                            }
                        } break;
                    case "kick":
                        {

                            if (player.Party.GetLeaderID() != player.userid)
                                message = "You are not party leader.";
                            if (slot < 0 && slot >= followers.Count)
                                    message = "Follower slot is invalid.";
                            else
                            {
                                message = $"{player.Party.NPCMembers[slot].displayName} was removed from the party";
                                player.Party.Remove(slot, player.Area);
                                await player.Party.SaveData();
                            }

                        }break;
                    default:
                        message = "Available actions: " + Environment.NewLine
                            + "``~followers inspect``" + Environment.NewLine
                            + "``~followers give``" + Environment.NewLine
                            + "``~followers interact #slot``" + Environment.NewLine
                            + "``~followers kick #slot``" + Environment.NewLine
                            ;
                        break;
                }
                var msg = await chan.SendMessageAsync(message, embed: embed?.Build());
                if (uiType != null) await player.NewUI(msg, uiType.Value, uidata);
            }
            else DUtils.DeleteMessage(await chan.SendMessageAsync("You currently have no follower."));
        }

        //Player Trading Commands
        /// <summary>
        /// 
        /// </summary>
        /// <param name="index">format: "itemIndex"x"itemAmount"</param>
        /// <param name="cost"></param>
        /// <param name="utarget"></param>
        /// <returns></returns>
        /// //
        [Command("ModifyPartySize")]
        public async Task ModfiyPartySize(int size)
        {
            if (Context.User.Id == 201875246091993088)
            {
                Player player = Player.Load(Context.BotUser);
                if(player.Party == null)
                    await ReplyAsync("You are not in a party.");
                if(player.Party.MemberCount > size)
                    await ReplyAsync("Size may not be lower than current member count.");
                else
                {
                    player.Party.maxPartySize = size;
                    await player.Party.SaveData();
                    await ReplyAsync("Maximum member size changed to " + size);
                }
            }
            else await ReplyAsync("You are not permitted to modify this value.");
        }

        [Command("Offer")]
        [Summary("Offer an item from your inventory to another user for a cost in Kutsyei Coins with an optional note.")]
        public async Task Offer(string indexXamount, long cost, string user, params string[] notes)
        {
            Player player = Context.Player;
            string prefix = Context.Prefix;
            //string[] splitArg = arg.Split('x', 'X', '*');
            int index = -1;
            int amount = 0;
            try
            {
                var ia = Verify.IndexXAmount(indexXamount);
                index = ia.index - 1;
                amount = ia.amount;
            }
            catch (Exception)
            {
                await ReplyAsync("The offer was unsuccessful, make sure you are using the correct format: " + Environment.NewLine +
                    "{ItemIndex}x{AmountOffered} cost @user" + Environment.NewLine +
                    $"Example: ``{prefix}offer 13*6 60 @Friend`` will offer 6 of the index 13 item for 60 each to Friend");
            }

            if (!ulong.TryParse(Regex.Replace(user, "[<|@|!|>]", ""), out ulong utId))
                throw NeitsilliaError.ReplyError("User is not valid. Please @user or enter user id.");

            IUser utarget = Context.Client.GetUser(utId) ??
                throw NeitsilliaError.ReplyError("User not found.");

            if (utarget.Id == player.userid)
                await ReplyAsync("You may not offer yourself an item.");
            else if(index > -1 && index < player.inventory.Count)
            {
                ItemOffer[] offers = await ItemOffer.SentOffers(player.userid);
                if (offers.Length >= 15)
                    await ReplyAsync("You may not create more offers, cancel older offers to continue." +
                        $" Use ``{prefix}Sent Offers`` to view all sent offers, inspect one using the reaction and cancel the offer");
                else
                {
                    amount = Verify.Max(amount, player.inventory.GetCount(index));
                    EmbedBuilder offer = new EmbedBuilder()
                    { Title = $"Confirm Offer to {utarget.Username}" };
                    offer.WithFooter($"Offer will be sent to the character {utarget.Username} will have loaded after the confirmation");
                    Item item = player.inventory.GetItem(index);
                    offer.WithDescription($"Offering {amount} {item} (R:{item.tier}) for {cost}Kuts." + Environment.NewLine +
                        $"Total: {cost}Kuts");
                    //
                    await player.NewUI(await ReplyAsync($"{Context.User.Mention}", 
                        embed: offer.Build()), MsgType.ConfirmOffer,
                        JsonConvert.SerializeObject(new string[] {index.ToString(), amount.ToString(),
                    cost.ToString(), utarget.Id.ToString(), ArrayM.ToString(notes)}));
                }
            }
            else await ReplyAsync("Selection invalid");
        }

        [Command("Offers")]
        public async Task ViewOffers()
        {
            string prefix = Context.Prefix;
            await ReplyAsync("Offer Commands:"
               + Environment.NewLine + prefix + "Offer {slot}x{amount} {total cost} {@user} {note to user} | Send a new offer to a user."
               + Environment.NewLine + prefix + "Received Offers | View a list of received offers to accept or decline them."
               + Environment.NewLine + prefix + "Sent Offers | View the list of sent offers to retract them."
            );
        }

        [Command("Received Offers")]
        public async Task ReceivedOffers(int page = 1)
        {
            Player player = Context.Player;
            await ItemOffer.GetOffers(player, page - 1, ItemOffer.OfferQuery.Receiver, Context.Channel);
        }
        [Command("Sent Offers")]
        public async Task SentOffers(int page = 1)
        {
            Player player = Context.Player;
            await ItemOffer.GetOffers(player, page - 1, ItemOffer.OfferQuery.Sender, Context.Channel);
        }
    }
}
