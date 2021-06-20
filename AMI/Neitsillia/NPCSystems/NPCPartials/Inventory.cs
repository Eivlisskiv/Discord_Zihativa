using AMI.Methods.Graphs;
using AMI.Neitsillia.Collections;
using AMI.Neitsillia.Items.ItemPartials;
using AMYPrototype;
using System;
using static AMI.Neitsillia.ReferenceData;

namespace AMI.Neitsillia.NPCSystems
{
    public partial class NPC
    {
        internal bool HasGearSlot(Item.IType t)
            => t switch
            {
                Item.IType.Weapon => hasweapon,
                Item.IType.Helmet => hashelmet,
                Item.IType.Mask => hasmask,
                Item.IType.Chest => haschestp,
                Item.IType.Jewelry => true,
                Item.IType.Trousers => hastrousers,
                Item.IType.Boots => hasboots,
                _ => false,
            };

        internal bool HasGearSlot(int t)
            => t switch
            {
                0 => hasweapon,
                1 => false,
                2 => hashelmet,
                3 => hasmask,
                4 => haschestp,
                5 => true,
                6 => true,
                7 => true,
                8 => hastrousers,
                9 => hasboots,
                _ => false,
            };

        internal Item SpawnItem(string name)
        {
            Item i = Item.LoadItem(name);
            if (!i.CanBeEquip())
                return i;

            try { i.Scale(level * 5); }
            catch (Exception e)
            {
                _ = Handlers.UniqueChannels.Instance.SendToLog(
                $"Failed to Scale mob drop: {i.originalName} => T {i.tier} > {level * 5} {Environment.NewLine} {e.Message}");
            }

            return i;
        }

        public void GearNPC()
        {
            if (level < 1)
                level = 1;
            //Get koins
            int average = Convert.ToInt32(Exponential.KoinsGeneratedWithMob(level));
            int maxK = Convert.ToInt32(average * 5);
            int minK = Convert.ToInt32(average * 2);
            KCoins = Program.rng.Next(minK, maxK);
            //Inventory
            int itemCount = Program.rng.Next(3, 8);
            string[] icollection = MobDrops(itemCount);
            //add inventory
            if (icollection != null)
                foreach (string i in icollection)
                {
                    if (i != null)
                        AddItemToInv(SpawnItem(i), 1, false);
                }
        }
        public void AddItemToInv(Item item, int amount = 1, bool ignoreInterest = true)
        {
            if (item == null) return;

            int result = IsInterested(item);
            if (inventory == null) inventory = new Inventory();

            if (
                (result == 2 || (ignoreInterest && result != 1))
                                ||
                (result == 1 && !EquipItem(item))
               )
            {
                UpgradeGear(item);
                inventory.Add(item, amount, -1);
            }

        }
        public void RemoveInvItem(int i, int amount = 1) => inventory.Remove(i, amount);
        /// <summary>
        /// Return -1 to ignore the item, 1 to equip and 2 to store
        /// </summary>
        /// <param name="item"> the item of interest</param>
        /// <param name="equip"> if the npc will even consider equipping it</param>
        /// <returns></returns>
        public int IsInterested(Item item)
        {
            if (item.CanBeEquip())
            {
                Item against = equipment.GetGear(item.type);
                if (against == null) return 1;

                double iCond = item.condition / item.durability;

                /*if (profession == Profession.Merchant && !equip && 
                   (iCond >= 0.30 || (iCond *100 + (item.tier - against.tier) > 5)))
                        return 2;//*/
                return against.tier < item.tier && iCond >= 0.5 ? 1 : //Equip
                        iCond >= 0.35 || (iCond + (item.tier - against.tier) > 0.20) ? 2 //Store
                        : -1; //ditch
            }
            else
            {
                if (Rank() < 20)
                    return 2;
                else if (item.type == Item.IType.Consumable)
                {
                    if (profession == Profession.Peasant)
                        return 2;
                    else if (health < Health())
                        return 2;
                }
                else if ((Rank() / 8) < item.tier)
                    return 2;
            }
            return -1;
        }

        bool UpgradeGear(Item item)
        {
            const int upgradeCost = 250;

            if (!item.CanBeEquip() || KCoins < upgradeCost) return false;

            if (item.tier < level * 5)
            {
                int tiers = item.tier - (level * 5);
                long cost = tiers * upgradeCost;
                if (KCoins >= cost)
                {
                    item.Scale(tiers + item.tier);
                    KCoins -= cost;

                    _ = Handlers.UniqueChannels.Instance.SendMessage("Population",
                        $"{displayName} Upgraded {item.originalName} to {item.name} reaching rank {item.tier}");

                    return true;
                }
            }
            return false;
        }
    }
}
