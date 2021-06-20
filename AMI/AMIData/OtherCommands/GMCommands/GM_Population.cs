using AMI.Methods;
using AMI.Neitsillia.Areas.AreaPartials;
using AMI.Neitsillia.Items.ItemPartials;
using AMI.Neitsillia.NPCSystems;
using AMI.Neitsillia.User.PlayerPartials;
using AMYPrototype;
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
        [Command("AnimRate")]
        public void AnimRate(int seconds)
        {
            Context.AdminCheck();

            PopulationHandler.SetPerSecond(seconds);
        }

        [Command("ForceWork")]
        public async Task ForceWork(int amount = 1, bool isNpcs = true)
        {
            Context.AdminCheck();

            Player player = Context.GetPlayer(Player.IgnoreException.All);

            Area area = player.Area;

            var popu = area.GetPopulation(isNpcs ?
                Neitsillia.Areas.AreaExtentions.Population.Type.Population
                : Neitsillia.Areas.AreaExtentions.Population.Type.Bounties);

            if(popu == null || popu.Count == 0)
            {
                await ReplyAsync("No population here");
                return;
            }

            for (int i = 0; i < amount && popu.Count > 0; i++)
            {
                NPC n = popu.Random();
                PopulationHandler.Add(area, n);
                await ReplyAsync($"Sent {n} to work");
                await Task.Delay(1000);
            }
        }

        [Command("New NPC")]
        public async Task New_NPC(string areaName, int amount = 1, string profession = "Child", int level = 0)
        {
            if (IsGMLevel(4).Result)
            {
                //areaName = Area.AreaDataExist(areaName);
                if (areaName != null)
                {
                    NPC[] npcs = new NPC[amount];
                    for (int i = 0; i < amount; i++)
                        npcs[i] = NPC.NewNPC(level, profession, null);

                    Area area = Area.LoadFromName(areaName);
                    string populationId = area.GetPopulation(Neitsillia.Areas.AreaExtentions.Population.Type.Population)._id;

                    EmbedBuilder noti = new EmbedBuilder();
                    noti.WithTitle($"{area.name} Population");
                    amount = 0;
                    if (npcs != null && npcs.Length > 0)
                    {
                        foreach (NPC n in npcs)
                        {
                            if (n != null)
                            {
                                if (n.profession == Neitsillia.ReferenceData.Profession.Child)
                                {
                                    if (area.parent == null)
                                        n.origin = area.name;
                                    else
                                        n.origin = area.parent;
                                    n.displayName = n.name + " Of " + n.origin;
                                }
                                else
                                    n.displayName = n.name;
                                PopulationHandler.Add(populationId, n);
                                amount++;
                            }
                        }
                    }

                    if (amount != 0) await ReplyAsync("NPCs created");
                    else await ReplyAsync("No new NPC were created");
                }
                else await DUtils.Replydb(Context, "Area not found.");
            }
        }

        [Command("New Bounty")]
        public async Task New_Bounty(string areaName, string creatureName = null, int floor = 0, int level = 1, string grantDrop = null)
        {
            if (IsGMLevel(4).Result)
            {
                areaName = StringM.UpperAt(areaName);
                //areaName = Area.AreaDataExist(areaName);
                if (areaName != null)
                {
                    Area area = Area.LoadFromName(areaName);
                    floor = Verify.MinMax(floor, area.floors);
                    //
                    NPC mob = null;
                    if (creatureName != null)
                    {
                        mob = NPC.GenerateNPC(Verify.Min(level, 0),
                            StringM.UpperAt(creatureName));
                    }
                    if (mob == null)
                        mob = area.GetAMob(Program.rng, floor);
                    //
                    if (grantDrop != null)
                    {
                        Item grant = Item.LoadItem(grantDrop);
                        if (grant != null)
                            mob.AddItemToInv(grant, 1, true);
                    }

                    //
                    PopulationHandler.Add(area, mob);

                    await DUtils.DeleteContextMessageAsync(Context);
                }
                else await DUtils.Replydb(Context, "Area not found.");
            }
        }

        [Command("Encounter Bounty")]
        public async Task EncounterBounty()
        {
            if (await IsGMLevel(4))
            {
                Player player = Player.Load(Context.BotUser);
                await ReplyAsync(embed: player.Area.ForceBountyEncounter(player).Build());
                player.SaveFileMongo();
            }
        }
    }
}
