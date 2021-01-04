using AMI.Handlers;
using AMI.Methods;
using AMI.Neitsillia.Areas;
using AMI.Neitsillia.Areas.AreaExtentions;
using AMI.Neitsillia.Areas.AreaPartials;
using AMI.Neitsillia.Collections;
using AMI.Neitsillia.Crafting;
using AMYPrototype;
using Neitsillia.Items.Item;
using Neitsillia.Methods;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AMI.Neitsillia.NPCSystems
{
    partial class NPC
    {
        private static string GetTime => $"[{DateTime.UtcNow.TimeOfDay.ToString("hh\\:mm")}]";

        public async Task<bool> Act(Area area, int multiplier)
        {
            if (health < (Health() * 0.65) || (health < Health()  && Program.Chance(30)))
                Rest();
            else
            {
                int actionrng = Program.rng.Next(101);

                if (actionrng <= 20 && area.IsExplorable)
                {
                    (bool explored, bool remove) = await Explore(area);
                    if (!explored) actionrng = -1;
                    else if (remove) return false;
                }
                else if (actionrng <= 40)
                {
                    if (!Travel(area)) actionrng = -1;
                    else return false;
                }
                else if (actionrng <= 60 && !TrainSkill())
                    actionrng = -1;
                else if (actionrng <= 80 && !Research())
                    actionrng = -1;
                else if (actionrng <= 100 && !AttemptMate(area) )
                    actionrng = -1;
                else actionrng = -1;

                if (actionrng == -1)
                    Work(area, multiplier);

                if ((profession != ReferenceData.Profession.Creature && inventory.Count >= 50)
                    || (profession == ReferenceData.Profession.Creature && inventory.Count >= 15))
                    TrimInventory();

            }
            if (profession != ReferenceData.Profession.Creature)
                AttemptCraftItem();

            return true;
        }
        void Rest()
        {
            _ = UniqueChannels.Instance.SendMessage("Population", $" **{displayName}** rested");
            if (health + (Health() * 0.1) < Health()) Healing(Convert.ToInt32(Health() * 0.1));
            else Healing(1);
        }
        async Task<(bool explored, bool remove)> Explore(Area area)
        {
            if (area.eLootRate == 0 && area.eMobRate == 0)
                return (false, false);
            else
            {
                Random rng = new Random();
                int x = rng.Next(101);
                if (x <= area.eLootRate)
                {
                    int t = ArrayM.IndexWithRates(area.loot.Length, rng);
                    AddItemToInv(SpawnItem(area.loot[t][ArrayM.IndexWithRates(area.loot[t].Count, rng)]));
                    KCoins += rng.Next(1 + Rank());

                    _ = UniqueChannels.Instance.SendMessage("Population", $"{GetTime} **{displayName}** found loot in {area.name}");

                    return (true, false);
                }
                else if (false && x <= area.eMobRate + (area.eLootRate))
                {
                    //To fix
                    x = rng.Next(11);
                    var bounties = area.GetPopulation(Population.Type.Bounties);
                    NPC mob = null;
                    if (x <= 2 && bounties != null && bounties.Count > 0)
                        mob = bounties.Random();
                    else
                    {
                        int rtier = ArrayM.IndexWithRates(area.mobs.Length, rng);
                        mob = GenerateNPC(area.level, area.mobs[rtier][ArrayM.IndexWithRates(area.mobs[rtier].Count, rng)]);
                    }
                    if (mob.displayName == displayName)
                        return (false, false);
                }
                else if (health < Health())
                {
                    health++;
                    _ = UniqueChannels.Instance.SendMessage("Population", $"{GetTime} **{displayName}** found nothing while exploring {area.name}");
                    return (true, false);
                }

                return (false, false);
            }
        }
        void Work(Area area, int multiplier)
        {
            if (area.type == AreaType.Tavern) GetsAProfession(ReferenceData.Profession.Tapster);

            double coinsMult = 1;
            int minCoins = 1;
            int maxCoins = Verify.Min(Rank(), minCoins + 1);
            //
            double toGetGear = level;
            int itemAmount = 1;
            int minItemTier = 0;
            int maxItemTier = level / 2;
            //
            int minXP = Verify.Min(Rank() * 5, 5);
            int maxXP = Verify.Min(Rank() * 10, 10);
            double xpMult = 1;
            Random rng = Program.rng;
            //
            int[] eqps = { 6, 7, 8, 9, 10, 11 };
            int[] cons = { 0, 1, 2 };
            switch (profession)
            {
                case ReferenceData.Profession.Peasant:
                    {
                        toGetGear = Verify.Max(level, 10);
                    }break;
                case ReferenceData.Profession.Creature:
                    {
                        maxCoins = Verify.MinMax(level, level, minCoins + 1);
                        maxXP = Verify.MinMax(level * 10, level * 10, 10);
                    }
                    break;
                case ReferenceData.Profession.Child:
                    {
                        xpMult = Math.Max(Rank() / 4.0, 10);
                        toGetGear = 0;
                    }
                    break;
                case ReferenceData.Profession.Tapster:
                    {
                        AddItemToInv(Item.LoadItem("Vhochait"), 5);
                        AddItemToInv(Item.NewTemporarySchematic(Item.RandomItem(level, rng.Next(1,3))));
                        minCoins += 10;
                        toGetGear = Verify.Max(level, 5);
                    }
                    break;
                case ReferenceData.Profession.Gladiator:
                    {
                        if (toGetGear < 10)
                            toGetGear = 10;
                        toGetGear *= 1.20;
                        coinsMult += 0.5;
                    }
                    break;
                case ReferenceData.Profession.Merchant:
                    {
                    coinsMult = Verify.Min(Rank() * 0.5, 1);
                        toGetGear = Verify.Max(level, 25);
                        AddItemToInv(Item.NewTemporarySchematic(Item.RandomItem(level)));
                    }
                    break;
                case ReferenceData.Profession.Adventurer:
                    {
                    xpMult = 3;
                        toGetGear = Verify.Max(level, 20);
                    }
                    break;
                case ReferenceData.Profession.Blacksmith:
                    {
                        Research();
                        AddItemToInv(Item.NewTemporarySchematic(Item.RandomItem(level, rng.Next(6,12))));
                        AddItemToInv(Item.CreateRepairKit(1));
                    }
                break;
                case ReferenceData.Profession.Alchemist:
                    {
                        Research();
                        AddItemToInv(Item.NewTemporarySchematic(Item.RandomItem(level, rng.Next(1, 3))));
                    }
                    break;
            }
            ///
            if (toGetGear > 70) toGetGear = 70;
            if (xpMult < 1) xpMult = 1;

            maxXP = Math.Max(maxXP, minXP + 1);
            maxCoins = Math.Max(maxXP, minCoins + 1);
            maxItemTier = Math.Max(maxItemTier, minItemTier + 1);
            ///
            long kutsGained = Convert.ToInt64(rng.Next(minCoins, maxCoins) * coinsMult);
            KCoins += kutsGained;
            for (int i = 0; i < itemAmount; i++)
            {
                if (rng.Next(0, 100) < toGetGear)
                    AddItemToInv(Item.RandomItem(rng.Next(minItemTier, maxItemTier), 
                        eqps[rng.Next(eqps.Length)]), 1, false);
                else
                    AddItemToInv(Item.RandomItem(rng.Next(minItemTier, maxItemTier),
                        cons[rng.Next(cons.Length)]), 1 , false);
            }
            _ = UniqueChannels.Instance.SendMessage("Population", $"{GetTime} **{displayName}** lvl{level} " +
                $"gained {XPGain(Convert.ToInt64(rng.Next(minXP, maxXP) * (xpMult + area.level + multiplier)), 0)}xp and {kutsGained} Kuts from work");
            if (profession == ReferenceData.Profession.Child && level > 13)
                GetsAProfession((ReferenceData.Profession)rng.Next(2,8));
        }
        bool Travel(Area area)
        {
            //return false; //disabled for now;
            if (profession == ReferenceData.Profession.Creature)
                return false;
            if(area.junctions == null || area.junctions.Count < 1)
                return false;
            else
            {
                Random r = Program.rng;

                Area toTravel = Area.LoadArea(area.junctions[r.Next(area.junctions.Count)].filePath);

                int rank = Rank() / 5;

                int x = r.Next(60) +  rank;

                switch(profession)
                {
                    case ReferenceData.Profession.Peasant:
                        {
                            if (!area.IsNonHostileArea() && toTravel.IsNonHostileArea())
                                return HasTraveled(area, toTravel);
                            else return false;
                        }

                    case ReferenceData.Profession.Child:
                        {
                            if (area.IsNonHostileArea() && rank < 25)
                                x -= (25 - rank);
                            x -= (10 - area.GetPopulation(Population.Type.Population).Count);

                            if (area.IsNonHostileArea())
                                x -= 20;
                            if (level < 18)
                                x -= 18 - level;
                        }
                        break;

                    case ReferenceData.Profession.Creature:
                        {
                            if (toTravel.IsNonHostileArea())
                                return false;
                        }
                        break;

                    default:
                        {
                            if (area.IsNonHostileArea() && rank < 25)
                                x -= (25 - rank);
                            x -= (10 - area.GetPopulation(Population.Type.Population).Count);

                            if (!toTravel.IsNonHostileArea() && area.IsNonHostileArea())
                            {
                                if (toTravel.level > level + 10)
                                    x -= toTravel.level - level;
                                else if (toTravel.level < level)
                                    return false;
                            }

                        }break;
                }


                if (Program.Chance(x))
                    return HasTraveled(area, toTravel);
            }
            return false;
        }
        bool HasTraveled(Area old, Area anew)
        {
            anew.GetPopulation(Population.Type.Population).Add(this);
            _ = UniqueChannels.Instance.SendMessage("Population", $"{GetTime} **{displayName}** has traveled from {old} to {anew}");
            return true;
        }
        bool AttemptMate(Area area)
        {
            if (level >= 18)
            {
                if (profession != ReferenceData.Profession.Creature && area.GetPopulation(Population.Type.Population).Count < 30)
                {
                    NPC child = NewNPC(1, "Child", race);
                    var parentNames = name.Split(' ');
                    child.name = RandomName.ARandomName() + " " + parentNames[parentNames.Length - 1];
                    if (area.parent == null)
                    {
                        child.origin = area.AreaId;
                        child.displayName = child.name + " Of " + area.name;
                    }
                    else
                    {
                        child.origin = area.GeneratePath(false) + area.parent;
                        child.displayName = child.name + " Of " + area.parent;
                    }
                    PopulationHandler.Add(area, child);
                    _ = UniqueChannels.Instance.SendMessage("Population", $"{GetTime} **{displayName}** has given birth to {child.name} in {area.name}");
                    return true;
                }
                else if (profession == ReferenceData.Profession.Creature && area.GetPopulation(Population.Type.Bounties).Count < 50)
                {
                    List<NPC> list = Database.LoadRecords("Creature", AMIData.MongoDatabase.FilterEqual<NPC, string>("race", race));
                    list.Sort((x, y) => x.baseLevel.CompareTo(y.baseLevel));
                    int index = ArrayM.IndexWithRates(list.Count, Program.rng);
                    NPC child = GenerateNPC(area.level, list[index]);
                    child.displayName = $"{RandomName.ARandomName()} {name}";
                    child.Evolve(skaviCat: area.name);

                    PopulationHandler.Add(area, child);
                    _ = UniqueChannels.Instance.SendMessage("Population", $"{GetTime} **{displayName}** has given birth to {child.displayName} in {area.name}");
                    return true;
                }
            }
            return false;
        }
        bool Research()
        {
            if (profession == ReferenceData.Profession.Creature)
                return false;
            int x = Program.rng.Next(101);
            if (profession == ReferenceData.Profession.Blacksmith || profession == ReferenceData.Profession.Alchemist)
                x += 40;
            if (x <= 40)
            {
                if (schematics == null)
                    schematics = new List<Schematic>();
                    Item i = Item.RandomItem(Rank());
                if (i.schematic != null && i.schematic.exists && schematics.FindIndex(Schematic.FindWithName(i.name)) == -1)
                {
                    bool canResearch = false;
                    if (i.CanBeEquip() && (profession == ReferenceData.Profession.Child || profession == ReferenceData.Profession.Blacksmith))
                    {
                        GetsAProfession(ReferenceData.Profession.Blacksmith);
                        canResearch = true;
                    }
                    else if ((i.type == Item.IType.Healing || i.type == Item.IType.Consumable)
                    && (profession == ReferenceData.Profession.Child || profession == ReferenceData.Profession.Alchemist))
                    {
                        GetsAProfession(ReferenceData.Profession.Alchemist);
                        canResearch = true;
                    }
                    if (canResearch)
                    {
                        schematics.Add(i.schematic);
                        _ = UniqueChannels.Instance.SendMessage("Population", $"{GetTime} **{displayName}** Researched {i.name} successfully");
                        return true;
                    }
                }
            }
            return false;
        }
        bool AttemptCraftItem()
        {
            if(schematics != null && schematics.Count > 0)
            {
                Schematic s = schematics[Program.rng.Next(schematics.Count)];
                if (s.name == null || s.name.Trim() == "")
                    schematics.RemoveAt(schematics.FindIndex(Schematic.FindWithName(s.name)));
                else
                {
                    string result = null;
                    if(s.Craft(this, ref result))
                        _ = UniqueChannels.Instance.SendMessage("Population", $"{GetTime} **{displayName}** Attempted to craft {s.name} and {result}");
                }
            }
            return false;
        }

        internal void TrimInventory(int length = 20)
        {
            int index = 0;
            if (inventory.Count < 30) return;

            string notif = "";
            while (length > 0 && index < inventory.Count)
            {
                StackedItems si = inventory[index];
                if(si.item.CanBeEquip())
                {
                    if (si.item.tier >= level * 5 || UpgradeGear(si.item) || si.item.tier > level * 3.5)
                        index++;
                    else
                    {
                        notif += ScrapItem(index) + Environment.NewLine;
                        if(notif.Length > 500)
                        {
                            _ = UniqueChannels.Instance.SendMessage("Population", $"{GetTime} **{displayName}** ... {Environment.NewLine} {notif}");
                            notif = "";
                        }
                        length--;
                    }
                }
                else
                    index++;
            }

            if(notif.Length > 0) _ = UniqueChannels.Instance.SendMessage("Population", $"{GetTime} **{displayName}** ... {Environment.NewLine} {notif}");
        }

        internal string ScrapItem(int index)
        {
            StackedItems st = inventory[index];

            KCoins += Verify.Min(st.item.GetValue() * st.count, 1);
            XPGain(Verify.Min(st.item.GetValue() * st.count, st.count));
            inventory.Remove(index, st.count);

            return $" Got Rid of {st} gaining {(st.item.GetValue() / 2) * st.count} XP and Coins";
        }

        bool TrainSkill()
        {
            if(abilities.Count > 1)
            {
                int i = Program.rng.Next(1, abilities.Count);
                if (abilities[i].type == Items.Ability.AType.Enchantment)
                    abilities[i] = Items.Ability.Load(abilities[i].name);
                if (abilities[i].GainXP((Rank() + level) * 100, 1))
                {
                    _ = UniqueChannels.Instance.SendMessage("Population", $"**{displayName}** Trained {abilities[i].name} " +
                        $"to level {abilities[i].level}");
                    return true;
                }
            }
            return false;
        }
    }
}
