using AMI.Neitsillia.Areas;
using AMI.Neitsillia.Areas.AreaPartials;
using AMI.Neitsillia.Collections;
using AMI.Neitsillia.NPCSystems;
using AMYPrototype;
using AMI.Neitsillia.Items.ItemPartials;
using System;

namespace AMI.AMIData.Events
{
    class EventBounty
    {
        public string[] bounties;
        public StackedObject<string, int>[] extraBountyDrops;
        public string[] uncommonBountyDrops;
        public string[] rareBountyDrops;

        public EventBounty(params string[] bounties)
        {
            this.bounties = bounties;
        }

        internal void SpawnBounty(Area area, int floor = 0)
        {
            if (bounties == null || bounties.Length < 1 || area.IsNonHostileArea() || area.GetPopulation(Neitsillia.Areas.AreaExtentions.Population.Type.Bounties).Count > 30
                || Program.Chance(50 + floor)) return;
            Random rng = Program.rng;

            NPC bounty = NPC.GenerateNPC(area.level, bounties[rng.Next(bounties.Length)]);
            if (extraBountyDrops != null && extraBountyDrops.Length > 0)
            {
                StackedObject<string, int> so = extraBountyDrops[rng.Next(extraBountyDrops.Length)];
                AddItem(bounty, so.item, so.count);
            }
            if (uncommonBountyDrops != null && uncommonBountyDrops.Length > 0 && rng.Next(101) <= 45)
                AddItem(bounty, uncommonBountyDrops[rng.Next(uncommonBountyDrops.Length)], 1);
            if (rareBountyDrops != null && rareBountyDrops.Length > 0 && rng.Next(101) <= 15)
                AddItem(bounty, rareBountyDrops[rng.Next(rareBountyDrops.Length)], 1);

            bounty.Evolve(floor / 10, skaviCat: area.name);

            area.GetPopulation(Neitsillia.Areas.AreaExtentions.Population.Type.Bounties).Add(bounty);

            _ = Handlers.UniqueChannels.Instance.SendMessage("Population", $"Event Bounty {bounty.name} Spawned @ {area.name}");
        }

        void AddItem(NPC bounty, string name, int count)
        {
            Item item = Item.LoadItem(name, "Event Items", "Item");
            int amountMultiplier = item.Scale(bounty.level * 5);
            bounty.AddItemToInv(item, count * amountMultiplier, true);
        }
    }
}
