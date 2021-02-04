using AMI.Methods;
using AMI.Neitsillia.Collections;
using System;

namespace AMI.Neitsillia.Items.Quests
{
    /// <summary>
    /// Function for loading a quest
    /// </summary>
    /// <param name="i">Use this variable to scale the quest</param>
    /// <param name="s">Use this variable to set a custom objective</param>
    /// <returns></returns>
    delegate Quest QuestFunc(int i = 1, string s = null);

    /*

    delegate (int i, string s)
    {
        return new Quest(" ", 1)
        {
            description = "",
            trigger = Quest.QuestTrigger,

            koinsReward = 0,
            xpReward = 0,
            itemReward = ItemReward("", 0),    
        };
    },

    //*/
    static class QuestLoad
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="s"></param>
        /// <param name="i"></param>
        /// <returns></returns>
        internal static Collections.StackedObject<string, int> ItemReward(string s = null, int i = 0)
        {
            return (s == null || i < 1 ? null :
                new Collections.StackedObject<string, int>(s, i));
        }
        internal static int[] SetNext(int i1 = -1, int i2 = -1, int i3 = -1)
        {
            return i1 < 0 || i2 < 0 || i3 < 0 ? null : new[]{ i1, i2, i3 };
        }
        internal static int[][] SetNexts(params int[][] nexts) => nexts;


        internal static Quest Incomplete(string title = "Coming Soon")
        {
            return new Quest(title, 1)
            {
                description = "Quest coming soon!",
                trigger = Quest.QuestTrigger.Incomplete,
            };
        }

        const string FloorLongDescription = "Reach higher floors by exploring and finding pathways. Higher floors increase the level of enemies.";

        internal static readonly QuestFunc[][][] QuestList = new QuestFunc[][][]
        {
            //0##   Story Quests
            new QuestFunc[][] //Main Quests
            {
                //00#   Hand Holding
                new QuestFunc[]//Hand Holding aka Tutorial
                {
                    //000
                    delegate(int i, string s)
                    {
                        return new Quest("Hand Holding I", 1)
                        {
                            description = "Collect your daily reward.",
                            longDesciption = "Use the ``daily`` command to collect your daily reward every 24 hours.",
                            trigger = Quest.QuestTrigger.CollectingDaily,

                            koinsReward = 0,
                            xpReward = 10,

                            nextId = new[] {
                                SetNext(0,0,1),
                                SetNext(2,0,0),
                            },
                        };
                        
                    },
                    //001
                    delegate(int i, string s)
                    {
                        return new Quest("Hand Holding II", 1)
                        {
                            description = "New players are suggested to complete the Tutorial Quest first.",
                            longDesciption = "Enter the area \"Atsauka\" by using `enter atsauka` command or `tp` command to view accessible locations",
                            trigger = Quest.QuestTrigger.Enter,

                            koinsReward = 500,
                            xpReward = 100,
                            nextId = new[] {
                                SetNext(0,1,0),
                                SetNext(0,0,2),
                            },
                        };
                    },
                    //002
                    delegate(int i, string s)
                    {
                        return new Quest("Hand Holding III", 1)
                        {
                            description = "Learn a schematics.",
                            longDesciption =  "You will have a small chance to" +
                            " learn a schematic by scraping items using ``scrap invSlot#``.",
                            trigger = Quest.QuestTrigger.Scrapping,

                            objective = "null;-1;True",

                            koinsReward = 500,
                            xpReward = 0,
                            nextId = new[] {
                                SetNext(0,0,3),
                            },
                        };
                    },
                    //003
                    delegate(int i, string s)
                    {
                        return new Quest("Hand Holding IV", 1)
                        {
                            description = "Craft an item from known schematics",
                            longDesciption = "Craft a schematic you learned using ``craft itemname``." +
                            " You can view known schematics using ``schems``",

                            trigger = Quest.QuestTrigger.Crafting,

                            koinsReward = 500,
                            xpReward = 0,
                            nextId = new[] {
                                SetNext(0,0,4),
                            },
                        };
                    },
                    //004
                    delegate(int i, string s)
                    {
                        return new Quest("Hand Holding V", 1)
                        {
                            description = "Upgrade gear.",
                            longDesciption = "Use ``IUpgrade invSlot#`` or ``iup invSlot#``." +
                            " Upgrading gear consumes the given material and applies it's highest positive and lowest negative stats to the item.",

                            trigger = Quest.QuestTrigger.GearUpgrading,

                            koinsReward = 500,
                            xpReward = 1000,
                            nextId = new[] {
                                SetNext(0,0,5),
                            },
                        };
                    },
                    //005
                    delegate(int i, string s)
                    {
                        return new Quest("Hand Holding VI", 1)
                        {
                            description = "Create a party.",
                            longDesciption = "Use ``create party partyname``, party name must be unique and is deleted when the party is disbanded.",

                            trigger = Quest.QuestTrigger.QuestLine,
                            objective = "party",

                            koinsReward = 100,
                            xpReward = 1000,
                            nextId = new[] {
                                SetNext(0,0,6),
                            },
                        };
                    },
                    //006
                    delegate(int i, string s)
                    {
                        return new Quest("Hand Holding VII", 1)
                        {
                            description = "Invite another player to your party.",
                            longDesciption = "Use `party invite @user` to invite another player. Invited player does not need to join for the objective to complete.",

                            trigger = Quest.QuestTrigger.QuestLine,
                            objective = "partyinvite",

                            koinsReward = 100,
                            xpReward = 1000,
                            nextId = new[] {
                                SetNext(0,0,7),
                            },
                        };
                    },
                    //007
                    delegate(int i, string s)
                    {
                        return new Quest("Hand Holding VIII", 1)
                        {
                            description = "Recruit an NPC to your party.",
                            longDesciption = "Use `Recruit` while in an interaction with an NPC to recruit them. " +
                            "Doing so will cost Kutsyei Coins and the NPC may refuse if the level difference is too great.",

                            trigger = Quest.QuestTrigger.RecruitNPC,

                            koinsReward = 5000,
                            xpReward = 10000,
                            nextId = new[] {
                                SetNext(0,0,8),
                            },
                        };
                    },
                    //008
                    delegate(int i, string s)
                    {
                        return new Quest("Hand Holding IX", 10)
                        {
                            description = "Consume Vhochait 10 times.",
                            longDesciption = "Vhochait is often mistaken for some random material when it is actually a healing concoction! " +
                            "Watch out for other healing items by inspecting them: `iteminfo itemnamehere`",

                            trigger = Quest.QuestTrigger.Consuming,

                            objective = "Vhochait;-1;null",

                            koinsReward = 500,
                            xpReward = 10000,
                            nextId = new[] {
                                SetNext(0,0,9),
                            },
                        };
                    },
                    //009
                    delegate(int i, string s)
                    {
                        return new Quest("Hand Holding X", 1)
                        {
                            description = "Enter a dungeon.",
                            longDesciption = " Watch out! Dungeons are very" +
                            " difficult. You can only leave a dungeon by dying, defeating" +
                            "the boss or running away from the boss.",

                            trigger = Quest.QuestTrigger.Enter,

                            objective = "Dungeon",

                            koinsReward = 500,
                            xpReward = 1000,
                            nextId = new[] {
                                SetNext(0,0,10),
                            },
                        };
                    },
                    //00;10
                    delegate(int i, string s)
                    {
                        return new Quest("Hand Holding X", 1)
                        {
                            description = "Kill a dungeon boss.",
                            longDesciption = "Bosses grant better rewards but are also much stronger than average enemies.",

                            trigger = Quest.QuestTrigger.ClearDungeon,

                            koinsReward = 2000,
                            xpReward = 100000,
                            nextId = new[] {
                                SetNext(0,0,11),
                            },
                        };
                    },
                    //00;11
                    delegate(int i, string s)
                    {
                        return new Quest("Hand Holding XI", 1)
                        {
                            description = "Accept a quest from your daily or weekly quests.",
                            longDesciption = "In any Tavern, use the `Service` command and select the \"Quest Board\" option to accept your daily and weekly quests",

                            trigger = Quest.QuestTrigger.QuestLine,
                            objective = "XI",

                            koinsReward = 5000,
                            xpReward = 10000,
                        };
                    },
                    //00?
                    delegate(int i, string s)
                    {
                        return new Quest("Hand Holding ?", 1)
                        {
                            description = "Canceled quest",

                            trigger = Quest.QuestTrigger.Kill,

                            koinsReward = 5000,
                            xpReward = 10000,
                        };
                    },
                },
                //01# Adventure Awaits
                new QuestFunc[]//Adventure Awaits (Entering new areas)
                {
                    //010
                    delegate(int i, string s)
                    {
                        return new Quest("Adventure Awaits I", 1)
                        {
                            description = "Enter Muzoisu.",
                            longDesciption = "Use `tp` to view accessible locations",
                            trigger = Quest.QuestTrigger.Enter,

                            koinsReward = 100,
                            objective = "Neitsillia\\Casdam Ilse\\Central Casdam\\Muzoisu\\Muzoisu",

                            nextId = new[] {
                                SetNext(1,0,0),
                                SetNext(2,1,0),
                            },
                        };
                    },
                    //011
                    delegate(int i, string s)
                    {
                        return new Quest("Adventure Awaits II", 1)
                        {
                            description = "Enter Amethyst Gardens.",
                            longDesciption = "Use `tp` to view accessible locations",
                            trigger = Quest.QuestTrigger.Enter,

                            koinsReward = 200,
                            objective = "Neitsillia\\Casdam Ilse\\Central Casdam\\Amethyst Gardens\\Amethyst Gardens",

                            nextId = new[] {
                                SetNext(1,1,0),
                            },
                        };
                    },

                    //012
                    delegate(int i, string s)
                    {
                        return new Quest("Adventure Awaits III", 1)
                        {
                            description = "Enter the town of Keanute",
                            longDesciption = "Use `tp` to view accessible locations",
                            trigger = Quest.QuestTrigger.Enter,

                            koinsReward = 500,
                            objective = "Neitsillia\\Casdam Ilse\\Central Casdam\\Keanute\\Keanute",

                            nextId = new[] {
                                SetNext(0,2,0),
                                SetNext(0,1,3),
                            },
                        };
                    },
                    //013
                    delegate(int i, string s)
                    {
                        return new Quest("Adventure Awaits IV", 1)
                        {
                            description = "Enter Peresa Forest",
                            longDesciption = "Use `tp` to view accessible locations",
                            trigger = Quest.QuestTrigger.Enter,

                            koinsReward = 500,
                            objective = "Neitsillia\\Casdam Ilse\\Central Casdam\\Peresa Forest\\Peresa Forest",

                            nextId = new[] {
                                SetNext(1,2,0),
                            },
                        };
                    },
                },
                //02# Whispers Of The Wind
                new QuestFunc[]
                {
                    //020
                    delegate(int i, string s)
                    {
                        return new Quest("Whispers Of The Wind I", 1)
                        {
                            description = "Follow the Trail.",
                            longDesciption = "You've been out the old mine's cave and the fresh air is no longer new to you. " +
                            "But the cool breezes are not all you sense. You feel a faint incomprehensible whisper coming from the high cliffs of the west." +
                            " Following the odd sounds might grant you what you seek.",
                            trigger = Quest.QuestTrigger.Enter,

                            objective = "Neitsillia\\Casdam Ilse\\Central Casdam\\Serene Cliffside\\Serene Cliffside",

                            koinsReward = 100,
                            xpReward = 10000,

                            nextId = new[] { SetNext(0,2,1) },
                        };
                    },

                    //021
                    delegate(int i, string s)
                    {
                        return new Quest("Whispers Of The Wind II", 1)
                        {
                            description = "Follow the Trail.",
                            longDesciption = "The odd sounds are coming from the northern peak of the cliffs. " +
                            "Reach the end of this greedy road to answer the call.",
                            trigger = Quest.QuestTrigger.EnterFloor,

                            objective = "Neitsillia\\Casdam Ilse\\Central Casdam\\Serene Cliffside\\Serene Cliffside;50",

                            koinsReward = 100,
                            xpReward = 10000,

                            nextId = new[] { SetNext(0,2,2) },
                        };
                    },

                    //022
                    delegate(int i, string s)
                    {
                        return new Quest("Whispers Of The Wind III", 1)
                        {
                            description = "Follow the Trail.",
                            longDesciption = "Before you, a path leads to a ruined shrine. Enter it alone.",
                            trigger = Quest.QuestTrigger.QuestLine,

                            objective = "Whispers Of The Wind III",

                            koinsReward = 100,
                            xpReward = 10000,

                            nextId = new[] { SetNext(0,2,3) },
                        };
                    },

                    //023
                    delegate(int i, string s)
                    {
                        return new Quest("Whispers Of The Wind IV", 1)
                        {
                            description = "Solve the puzzle.",
                            longDesciption = "You've reached the shrine, the wind has calmed and a puzzle was left for you. Solve it on your own",
                            trigger = Quest.QuestTrigger.Puzzle,

                            objective = "Disk;2;Untamed Specter",

                            koinsReward = 100,
                            xpReward = 10000,

                            nextId = new[] { SetNext(0,2,4) },
                        };
                    },

                    //024
                    delegate(int i, string s)
                    {
                        return new Quest("Whispers Of The Wind V", 1)
                        {
                            description = "Defeat the specter.",
                            longDesciption = "The puzzle unleashed a blast of energy, awakening an untamed specter. " +
                            "Prove your worth to the creature by defeating it.",
                            trigger = Quest.QuestTrigger.Kill,

                            objective = $"Untamed Specter;Specter;-1",

                            koinsReward = 0,
                            xpReward = 10,

                            nextId = new[] { SetNext(0,2,5) },
                        };
                    },
                    
                    //025
                    delegate(int i, string s)
                    {
                        return new Quest("Whispers Of The Wind VI", 1)
                        {
                            description = "Use an essence vial.",
                            longDesciption = "The specter judged you worthy and binded with you. " + Environment.NewLine +
                            "A binded specter's element can be used by the one binded to it to cast an ability." + Environment.NewLine +
                            "`Use` an essence vial to change your specter's element. (Vials are consumed on usage)",
                            trigger = Quest.QuestTrigger.QuestLine,

                            objective = "Whispers Of The Wind VI",

                            koinsReward = 0,
                            xpReward = 10,
                        };
                    }

                },
                //03#   Pets Story Line
                new QuestFunc[]
                {
                    //030
                    delegate(int i, string s)
                    {
                        return new Quest("The Wild Art I", 1)
                        {
                            description = "Acquire an Egg Pocket",
                            longDesciption = "Deliver [50x Leather], [5x Polished Metal] and [25x Cloth] to any Beast Master's store using the `deliver` command to acquire an Egg Pocket.",
                            trigger = Quest.QuestTrigger.Deliver,

                            koinsReward = 100,
                            objective = "BeastMasterShop;Leather*50,Polished Metal*5,Cloth*25",

                            nextId = new[]
                            {
                                SetNext(0,3,1),
                            },
                        };
                    },
                    //031
                    delegate(int i, string s)
                    {
                        return new Quest("The Wild Art II", 1)
                        {
                            description = "Acquire an Egg.",
                            longDesciption = "Complete battles in **nests** to have a chance to get an egg. Your chances increase with each battle won. " +
                            "(One drop per nest for each character and you cannot have more than one egg at a time)",
                            trigger = Quest.QuestTrigger.FillEggPocket,

                            koinsReward = 100,
                            objective = null,

                            nextId = new[]
                            {
                                SetNext(0,3,2),
                            },
                        };
                    },

                    //032
                    delegate(int i, string s)
                    {
                        return new Quest("The Wild Art III", 1)
                        {
                            description = "Quest line delayed.",
                            longDesciption = "The Wild Arts updates have been delayed. These updates will return later.",
                            trigger = Quest.QuestTrigger.Incomplete,

                            koinsReward = 100,
                            objective = "~~",

                            nextId = new[]
                            {
                                SetNext(0,3,2),
                            },
                        };
                    },
                },
                //04#   Tutorial 2.0
                new QuestFunc[]
                {
                    //040
                    delegate(int i, string s)
                    {
                        return new Quest("Tutorial I", 1)
                        {
                            description = "Collect your first daily rewards.",
                            longDesciption = "Use `daily` to collect your daily every 24 hours.",
                            trigger = Quest.QuestTrigger.CollectingDaily,

                            koinsReward = 100,
                            itemReward = new StackedObject<string, int>("Odez's Gift", 1),

                            nextId = new[]
                            {
                                SetNext(0,4,1),
                            },
                        };
                    },
                    //041
                    delegate(int i, string s)
                    {
                        return new Quest("Tutorial II", 1)
                        {
                            description = "Open inventory and equip gear.",
                            longDesciption = "Open your inventory with the `Inv` command. Equip gear with the `Equip` command followed by the inventory slots of the items." +
                            " Example: `~Equip 3 4` Equips items in the slot 3 and 4 of the inventory.",
                            trigger = Quest.QuestTrigger.QuestLine,

                            objective = "TII",

                            koinsReward = 200,

                            nextId = new[]
                            {
                                SetNext(0,4,2),
                            },
                        };
                    },
                    //042
                    delegate(int i, string s)
                    {
                        return new Quest("Tutorial III", 1)
                        {
                            description = "Explore the area.",
                            longDesciption = "Use the `explore` command to explore your current area. Results vary depending on the area you are in.",
                            trigger = Quest.QuestTrigger.QuestLine,

                            objective = "TIII",

                            koinsReward = 200,

                            nextId = new[]
                            {
                                SetNext(0,4,3),
                            },
                        };
                    },
                    //043
                    delegate(int i, string s)
                    {
                        return new Quest("Tutorial IV", 1)
                        {
                            description = "Cast an ability other than brawl.",
                            longDesciption = "The command `Cast` will cast 'Brawl' by default, to use another of your abilities, type the ability name. EX: `~Cast strike` " +
                            " Use the `Ability` command to view your character's abilities.",
                            trigger = Quest.QuestTrigger.QuestLine,

                            objective = "TIV",

                            koinsReward = 100,

                            nextId = new[]
                            {
                                SetNext(0,4,4),
                            },
                        };
                    },
                    //044
                    delegate(int i, string s)
                    {
                        return new Quest("Tutorial V", 1)
                        {
                            description = "Target a cast manually.",
                            longDesciption = "By default, defensive abilities are casted on self and offensive abilities on the first live enemy. " + Environment.NewLine +
                            "You can manually pick your target by typing the target position after the ability name. " + Environment.NewLine+
                            "Positions are `p0` `p1` `p2` `p3` for allies or `m0` `m1` `m2` `m3` for enemies, as written on the right of each combatant's name. " + Environment.NewLine +
                            "Example: `~Cast brawl m0` ",
                            trigger = Quest.QuestTrigger.QuestLine,

                            objective = "TV",

                            koinsReward = 100,

                            nextId = new[]
                            {
                                SetNext(0,4,5),
                            },
                        };
                    },
                    //045
                    delegate(int i, string s)
                    {
                        return new Quest("Tutorial VI", 1)
                        {
                            description = "Win a battle.",
                            longDesciption = "Kill quests only gain progress if the battle was won.",
                            trigger = Quest.QuestTrigger.Kill,

                            koinsReward = 200,

                            nextId = new[]
                            {
                                SetNext(0,4,6),
                            },
                        };
                    },
                    //046
                    delegate(int i, string s)
                    {
                        return new Quest("Tutorial VII", 1)
                        {
                            description = "Loot the spoils.",
                            longDesciption = "Loot is most commonly found when defeating enemies. Loot everything using the `Loot` command. " + Environment.NewLine +
                            "You can also loot specific items by indicating the slot and amount `Loot {slot}x{amount}` Example: `~loot 1x10` loots 10 amount of the slot 1. " + Environment.NewLine +
                            "**WARNING:** When playing with others in a party, the loot is for all and is not in separate instances, make sure to divide the booty with your friends",
                            trigger = Quest.QuestTrigger.QuestLine,

                            objective = "TVII",
                            xpReward = 2000,

                            nextId = new[]
                            {
                                SetNext(0,4,7),
                            },
                        };
                    },
                    //047
                    delegate(int i, string s)
                    {
                        return new Quest("Tutorial VIII", 1)
                        {
                            description = "Eat something.",
                            longDesciption = "Use the eat command to consume something from your inventory: `Eat {slot}x{amount}` " +
                            "for example: `~eat 1x40` will eat 40 of the first item in your inventory." + Environment.NewLine +
                            "You can also `rest` to regain health over time with no additional cost." + Environment.NewLine +
                            "You can `inspect` items in your inventory to see if they are __Healing__ or __Consumable__ items. Example: `~inspect 1`",
                            trigger = Quest.QuestTrigger.Consuming,

                            xpReward = 2000,

                            nextId = new[]
                            {
                                SetNext(0,4,8),
                            },
                        };
                    },
                    //048
                    delegate(int i, string s)
                    {
                        return new Quest("Tutorial IX", 1)
                        {
                            description = "Travel somewhere.",
                            longDesciption = "Congrats, you've learned the basics." +
                            "Use the `Travel Post` command aliased `tp` to travel to other areas.",
                            trigger = Quest.QuestTrigger.Enter,

                            xpReward = 1000,
                            koinsReward = 500
                        };
                    },
                },
            },
            //1##   Side Quests
            new QuestFunc[][] //Side Quests
            {
                //10#   Muzoisu Progression
                new QuestFunc[]
                {
                    //100
                    delegate(int i, string s)
                    {
                        return new Quest("Muzoisu's Depths I", 1)
                        {
                            description = "Reach Floor 5 in Muzoisu.",
                            longDesciption = FloorLongDescription,
                            trigger = Quest.QuestTrigger.EnterFloor,

                            koinsReward = 100,
                            xpReward = 500,
                            objective = "Neitsillia\\Casdam Ilse\\Central Casdam\\Muzoisu\\Muzoisu;5",

                            nextId = new[] {
                                SetNext(1,0,1)
                            }
                        };
                    },
                    //101
                    delegate(int i, string s)
                    {
                        return new Quest("Muzoisu's Depths II", 1)
                        {
                            description = "Reach Floor 10 in Muzoisu.",
                            longDesciption = FloorLongDescription,
                            trigger = Quest.QuestTrigger.EnterFloor,

                            koinsReward = 100,
                            xpReward = 750,
                            objective = "Neitsillia\\Casdam Ilse\\Central Casdam\\Muzoisu\\Muzoisu;10",

                            nextId = new[] {
                                SetNext(1,0,2)
                            }
                        };
                    },
                    //102
                    delegate(int i, string s)
                    {
                        return new Quest("Muzoisu's Depths III", 1)
                        {
                            description = "Reach Floor 20 in Muzoisu.",
                            longDesciption = FloorLongDescription,
                            trigger = Quest.QuestTrigger.EnterFloor,

                            koinsReward = 500,
                            xpReward = 1200,
                            objective = "Neitsillia\\Casdam Ilse\\Central Casdam\\Muzoisu\\Muzoisu;20",

                            nextId = new[] {
                                SetNext(0,1,1),
                                SetNext(1,0,3)
                            }
                        };
                    },
                    //103
                    delegate(int i, string s)
                    {
                        return new Quest("Muzoisu's Depths IV", 1)
                        {
                            description = "Reach Floor 35 in Muzoisu.",
                            longDesciption = FloorLongDescription,
                            trigger = Quest.QuestTrigger.EnterFloor,

                            koinsReward = 500,
                            xpReward = 2500,
                            objective = "Neitsillia\\Casdam Ilse\\Central Casdam\\Muzoisu\\Muzoisu;35",

                            nextId = new[] {
                                SetNext(1,0,4)
                            }
                        };
                    },
                    //104
                    delegate(int i, string s)
                    {
                        return new Quest("Muzoisu's Depths V", 1)
                        {
                            description = "Reach Floor 75 in Muzoisu.",
                            longDesciption = FloorLongDescription,
                            trigger = Quest.QuestTrigger.EnterFloor,

                            koinsReward = 2000,
                            xpReward = 10000,
                            objective = "Neitsillia\\Casdam Ilse\\Central Casdam\\Muzoisu\\Muzoisu;75",

                            nextId = new[] {
                                SetNext(1,0,5)
                            }
                        };
                    },
                    //105
                    delegate(int i, string s)
                    {
                        return new Quest("Muzoisu's Depths VI", 1)
                        {
                            description = "Reach Floor 100 in Muzoisu.",
                            longDesciption = FloorLongDescription,
                            trigger = Quest.QuestTrigger.EnterFloor,

                            koinsReward = 5000,
                            xpReward = 100000,
                            objective = "Neitsillia\\Casdam Ilse\\Central Casdam\\Muzoisu\\Muzoisu;100",

                            itemReward = ItemReward("Lesser Sigil Of Avlimia", 1),//
                        };
                    },
                },

                //11#   Amethyst Gardens Progression
                new QuestFunc[]
                {
                    //110
                    delegate(int i, string s)
                    {
                        return new Quest("Amethyst Gardens I", 1)
                        {
                            description = "Reach Floor 3 in Amethyst Gardens.",
                            longDesciption = FloorLongDescription,
                            trigger = Quest.QuestTrigger.EnterFloor,

                            koinsReward = 500,
                            xpReward = 2500,
                            objective = "Neitsillia\\Casdam Ilse\\Central Casdam\\Amethyst Gardens\\Amethyst Gardens;3",

                            nextId = new[] {
                                SetNext(1,1,1)
                            }
                        };
                    },
                    //111
                    delegate(int i, string s)
                    {
                        return new Quest("Amethyst Gardens II", 1)
                        {
                            description = "Reach Floor 8 in Amethyst Gardens.",
                            longDesciption = FloorLongDescription,
                            trigger = Quest.QuestTrigger.EnterFloor,

                            koinsReward = 2500,
                            xpReward = 6000,
                            objective = "Neitsillia\\Casdam Ilse\\Central Casdam\\Amethyst Gardens\\Amethyst Gardens;8",

                            nextId = new[] {
                                SetNext(1,1,2),
                                SetNext(0,1,2),
                            }
                        };
                    },
                    //112
                    delegate(int i, string s)
                    {
                        return new Quest("Amethyst Gardens III", 1)
                        {
                            description = "Reach Floor 16 in Amethyst Gardens.",
                            longDesciption = FloorLongDescription,
                            trigger = Quest.QuestTrigger.EnterFloor,

                            koinsReward = 2000,
                            xpReward = 10000,
                            objective = "Neitsillia\\Casdam Ilse\\Central Casdam\\Amethyst Gardens\\Amethyst Gardens;16",

                            nextId = new[] {
                                SetNext(1,1,3)
                            }
                        };
                    },
                    //113
                    delegate(int i, string s)
                    {
                        return new Quest("Amethyst Gardens IV", 1)
                        {
                            description = "Reach Floor 25 in Amethyst Gardens.",
                            longDesciption = FloorLongDescription,
                            trigger = Quest.QuestTrigger.EnterFloor,

                            koinsReward = 25000,
                            xpReward = 500000,
                            objective = "Neitsillia\\Casdam Ilse\\Central Casdam\\Amethyst Gardens\\Amethyst Gardens;25",

                            itemReward = ItemReward("Lesser Sigil Of Bakora", 1),//
                        };
                    },
                },
                //12#   Peresa Forest Progression
                new QuestFunc[]
                {
                    //120
                    delegate(int i, string s)
                    {
                        i = 0;
                        Quest q = FloorProgressionQuest("Neitsillia\\Casdam Ilse\\Central Casdam\\Peresa Forest\\Peresa Forest", 5, i, 5);
                        q.nextId = SetNexts(SetNext(1,2,i+1));
                        return q;
                    },
                    //121
                    delegate(int i, string s)
                    {
                        i = 1;
                        Quest q = FloorProgressionQuest("Neitsillia\\Casdam Ilse\\Central Casdam\\Peresa Forest\\Peresa Forest", 15, i, 5);
                        q.nextId = SetNexts(SetNext(1,2,i+1));
                        return q;
                    },
                    //122
                    delegate(int i, string s)
                    {
                        i = 2;
                        Quest q = FloorProgressionQuest("Neitsillia\\Casdam Ilse\\Central Casdam\\Peresa Forest\\Peresa Forest", 30, i, 5);
                        q.nextId = SetNexts(SetNext(1,2,i+1));
                        return q;
                    },
                    //123
                    delegate(int i, string s)
                    {
                        i = 3;
                        Quest q = FloorProgressionQuest("Neitsillia\\Casdam Ilse\\Central Casdam\\Peresa Forest\\Peresa Forest", 50, i, 5);
                        q.nextId = SetNexts(SetNext(1,2,i+1));
                        return q;
                    },
                    //124
                    delegate(int i, string s)
                    {
                        i = 4;
                        Quest q = FloorProgressionQuest("Neitsillia\\Casdam Ilse\\Central Casdam\\Peresa Forest\\Peresa Forest", 75, i, 5);
                        q.nextId = SetNexts(SetNext(1,2,i+1));
                        return q;
                    },
                    //125
                    delegate(int i, string s)
                    {

                        i = 5;
                        Quest q = FloorProgressionQuest("Neitsillia\\Casdam Ilse\\Central Casdam\\Peresa Forest\\Peresa Forest", 100, i, 5);
                        q.nextId = SetNexts(SetNext(1,2,i+1), SetNext(2,0,2));
                        return q;
                    },
                    //126
                    delegate(int i, string s)
                    {
                        return Incomplete("Peresa Forest");
                        //i = 7;
                        //Quest q = FloorProgressionQuest("Neitsillia\\Casdam Ilse\\Central Casdam\\Peresa Forest\\Peresa Forest", 150, i, 5);
                        //q.nextId = SetNexts(SetNext(1,2,i+1));
                        //return q;
                    },
                    //127
                    delegate(int i, string s)
                    {
                        i = 6;
                        Quest q = FloorProgressionQuest("Neitsillia\\Casdam Ilse\\Central Casdam\\Peresa Forest\\Peresa Forest", 200, i, 5);
                        return q;
                    },
                },
            },
            //2##   Repeatable non rng quests
            new QuestFunc[][]
            {
                //20#
                new QuestFunc[]//Collecting Daily
                {
                    //210
                    delegate(int i, string s)
                    {
                        return CollectDailyQuest(1);
                    },
                    //211
                    delegate(int i, string s)
                    {
                        return CollectDailyQuest(2);
                    },
                    //212
                    delegate(int i, string s)
                    {
                        return CollectDailyQuest(3);
                    },
                    //213
                    delegate(int i, string s)
                    {
                        return CollectDailyQuest(3);
                    },
                },
                //21#
                new QuestFunc[]//Kill Quest
                {
                    //210
                    delegate(int i, string s)
                    {
                        return AnyKillQuest(1);
                    },
                    //211
                    delegate(int i, string s)
                    {
                        return AnyKillQuest(2);
                    },
                    //212
                    delegate(int i, string s)
                    {
                        return AnyKillQuest(3);
                    },
                    //213
                    delegate(int i, string s)
                    {
                        return AnyKillQuest(4);
                    },
                    //214
                    delegate(int i, string s)
                    {
                        return AnyKillQuest(5);
                    },
                    //215
                    delegate(int i, string s)
                    {
                        return AnyKillQuest(6);
                    },
                    //216
                    delegate(int i, string s)
                    {
                        return AnyKillQuest(7);
                    },
                    //217
                    delegate(int i, string s)
                    {
                        return AnyKillQuest(8);
                    },
                    //218
                    delegate(int i, string s)
                    {
                        return AnyKillQuest(9);
                    },
                    //219
                    delegate(int i, string s)
                    {
                        return AnyKillQuest(10);
                    },
                    //21 10
                    delegate(int i, string s)
                    {
                        return AnyKillQuest(10);
                    },
                },
            },
            //3##   These arrays are used for rng quest drops, do not place non duplicate quests here
            new QuestFunc[][] //Repeatable Quests
            {
                //30#   No specific objective
                new QuestFunc[]
                {
                    //300   Craft x items
                    delegate (int i, string s)
                    {
                        return new Quest("Trinky Trinkets", i*5)
                        {
                            description = $"Craft {i*5} items",
                            longDesciption = "Use `craft itemname` to craft an item. Use `schems` to view list of known schematics to craft.",
                            trigger = Quest.QuestTrigger.Crafting,

                            koinsReward = 100 * i,
                            xpReward = 5000 * i,
                            //itemReward = ItemReward("", 0),
                        };
                    },
                    //301   Scrap x items
                    delegate (int i, string s)
                    {
                        return new Quest("Dismantling Frenzy", i*75)
                        {
                            description = $"Scrap {i*75} items",
                            longDesciption = "Use `scrap #invslot` or `bulkscrap #invslot #invslot2 #invslot3...` to scrap items from your inventory.",
                            trigger = Quest.QuestTrigger.Scrapping,

                            koinsReward = 50 * i,
                            xpReward = 2000 * i,
                            //itemReward = ItemReward("", 0),
                        };
                    },
                    //302   Upgrade gear x times
                    delegate (int i, string s)
                    {
                        return new Quest("Personal Touches", i*8)
                        {
                            description = $"Upgrade gear {i*8} times",
                            longDesciption = "Use `iupgrade` command to upgrade gear.",
                            trigger = Quest.QuestTrigger.GearUpgrading,

                            koinsReward = 50 * i,
                            xpReward = 2000 * i,
                            //itemReward = ItemReward("", 0),
                        };
                    },
                    //303   Clear Dungeons
                    delegate (int i, string s)
                    {
                        return new Quest("Dungeon dweller", i)
                        {
                            description = $"Clear {i} dungeons",
                            longDesciption = "Eliminate the dungeon boss or escape from the dungeon alive to clear it.",
                            trigger = Quest.QuestTrigger.ClearDungeon,

                            koinsReward = 1000 * i,
                            xpReward = 50000 * i,
                            //itemReward = ItemReward("", 0),
                        };
                    },
                },
                //31#   Creature Race Related
                new QuestFunc[]
                {
                    //301   Kill x of y race enemies
                    delegate (int i, string s)
                    {
                        s = s ?? Utils.RandomElement("Vhoizuku", "Goq", "Octopus", "Cevharhu");
                        return new Quest($"{s} Hunting", i*10)
                        {
                            description = $"Kill {i*10} {s}",
                            trigger = Quest.QuestTrigger.Kill,

                            objective = $"null;{s};-1",

                            koinsReward = 500 * i,
                            xpReward = 1000 * i,
                            //itemReward = ItemReward("", 0),
                        };
                    },

                },
            },
        };

        public static Quest CollectDailyQuest(int n)
        {
            return new Quest($"Daily Rations", n * 5)
            {
                description = $"Collect your daily reward {n * 5} times.",
                longDesciption = "Use `daily` to collect your daily every 24 hours.",
                trigger = Quest.QuestTrigger.CollectingDaily,

                xpReward = 500 * n,
                nextId = new[] { SetNext(2, 0, n) },
                itemReward = new StackedObject<string, int>("-Repair Kit;1", n * 3)
            };
        }

        public static Quest AnyKillQuest(int n) 
        {
            int t = n * n;
            return new Quest("Blood Hunt", t * 8)
            {
                description = $"Kill {t * 8} enemies.",
                longDesciption = "Win your fights to increase your kill count. Progress is awarded to all party members.",
                trigger = Quest.QuestTrigger.Kill,

                koinsReward = 50 * t,
                xpReward = 500 * t,
                nextId = new[] { SetNext(2, 1, n) },
                
                itemReward = n == 10 ? new StackedObject<string, int>("-Rune;1", 1)
                : new StackedObject<string, int>("-Repair Kit;1", n * 2)
            };
        }

        //Enter Quests
        public static Quest FloorProgressionQuest(string areaId, int floor, int num, int mult = 1)
        {
            string[] split = areaId.Split('\\');
            string name = split[split.Length - 1];

            return new Quest($"{name} {NumbersM.GetLevelMark(num)}", 1)
            {
                description = $"Reach Floor {floor} in {name}.",
                longDesciption = FloorLongDescription,
                trigger = Quest.QuestTrigger.EnterFloor,

                koinsReward = 250 * floor * mult,
                xpReward = 500 * floor * mult,
                objective = $"{areaId};{floor}",
                itemReward = new StackedObject<string, int>($"-Rune;1", 1),
            };
        }
    }
}
