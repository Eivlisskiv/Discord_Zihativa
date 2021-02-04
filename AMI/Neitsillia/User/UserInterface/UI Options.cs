using System;
using System.Collections.Generic;
using System.Linq;
using static AMI.Neitsillia.User.UserInterface.EUI;
using AMI.Neitsillia.Encounters;
using AMI.Neitsillia.Items;
using AMI.Neitsillia.Gambling.Games;
using AMI.Methods;

namespace AMI.Neitsillia.User.UserInterface
{
    partial class UI
    {
        static readonly Action[] OptionsInitializers = { InitO_Events, InitO_Area, 
            InitO_Inventory, InitO_Strongholds, InitO_Dynasty, InitO_Sandbox };
        static Dictionary<MsgType, Action<UI>> OptionsLoad;

        public static void InitialiseOptionLoaderDict()
        {
            OptionsLoad = new Dictionary<MsgType, Action<UI>>();
            for (int i = 0; i < OptionsInitializers.Length; i++)
                OptionsInitializers[i].Invoke();
        }

        partial void InitialiseOption()
        {
            switch (type)
            {
                #region Basics
                case MsgType.Main:
                    options = new List<string>
                    {
                        EUI.explore,EUI.tpost, EUI.help
                    };
                    break;
                case MsgType.Sheet:
                    options = new List<string>
                    {
                        EUI.stats, EUI.xp,
                        EUI.loot, EUI.help
                    };
                    break;
                case MsgType.Stats:
                    options = new List<string>
                    {
                        loot, explore, tpost
                        //help
                    };
                    break;
                case MsgType.XP:
                    {
                        options = new List<string>
                        {
                            loot, ability
                        //help
                        };
                        if (player.skillPoints > 0)
                            options.Add(skills);
                        if (player.Specialization != null)
                            options.Add(EUI.SpecIcon(
                                (int)player.Specialization.specType));
                        else if (player.level > 19)
                            options.Add(pickSpec);
                    }
                    break;
                case MsgType.Skills:
                    options = new List<string>
                    {
                        GetLetter(4), GetLetter(8), GetLetter(18),
                        GetLetter(2), GetLetter(3), GetLetter(15),
                        //help
                    };
                    break;
                case MsgType.Schems:
                    options = new List<string>
                    {
                        prev, next
                    };
                    break;
                case MsgType.Loot:
                    {
                        options = new List<string>
                        {
                            prev, next, loot, inv,
                            explore, help
                        };
                        if (player.Encounter != null)
                        {
                            int.TryParse(data, out int page);
                            var ploot = player.Encounter.loot;
                            if (ploot != null)
                            {
                                if (ploot.Count < 30 ||
                                    page <= 0)
                                    options.Remove(prev);
                                if (ploot.Count < 16 || page + 1
                                    >= (Math.Ceiling(ploot.Count / 25.00)))
                                    options.Remove(next);
                            }
                        }
                    }
                    break;
                case MsgType.Puzzle: options = player.Encounter.puzzle.emotes.ToList(); break;
                #endregion

                #region Pets
                case MsgType.EggPocket:
                    options = new List<string>()
                    {
                        pets, cancel, //discard egg
                    };
                    break;
                case MsgType.EggInfo:
                    //when a new egg is found
                    options = new List<string>()
                    {
                        egg, //pickup
                    };
                    break;
                case MsgType.InspectPet:
                    {
                        switch (player.PetList[int.Parse(data)].status)
                        {
                            case NPCSystems.Companions.Pets.Pet.PetStatus.Idle:
                                options = new List<string>()
                                {
                                    EUI.summon, EUI.skills, EUI.whistle, EUI.pickSpec
                                }; break;
                            case NPCSystems.Companions.Pets.Pet.PetStatus.Fetching:
                                options = new List<string>() { EUI.whistle }; break;
                        }

                    }
                    break;
                case MsgType.PetList:
                    {
                        options = new List<string>();
                        for (int i = 0; i < player.PetList.Count; i++)
                        {
                            options.Add(GetNum(i));
                        }

                    }
                    break;
                case MsgType.PetUpgrade:
                    {
                        options = new List<string>() { uturn };
                        int k = -1;
                        string[] split = data.Split(';');
                        if (split.Length > 1) int.TryParse(data.Split(';')[1], out k);

                        if (k > -1 && k < 2)
                        {
                            options.Add(greaterthan);
                        }
                        else
                        {
                            int max = k > -1 ? ReferenceData.DmgType.Length :
                            Enum.GetNames(typeof(NPCSystems.Companions.Pets.PetUpgrades.Upgrade)).Length;
                            for (int i = 0; i < max; i++)
                            {
                                options.Add(k > -1 ? GetElement((ReferenceData.DamageType)i) : GetNum(i));
                            }
                        }
                    }
                    break;
                case MsgType.PetEvolve:
                    options = new List<string>();
                    int count = int.Parse(data.Split(';')[1]);
                    for (int i = 0; i < count; i++)
                        options.Add(GetNum(i));
                    break;
                #endregion

                #region NPCs
                case MsgType.NPC:
                    {
                        options = new List<string>
                {
                    inv, stats,explore,
                            help
                };
                        if (player.Encounter != null
                        && player.Encounter.npc != null && player.Encounter.npc.inventory != null
                        && player.Encounter.npc.inventory.Count >= 1)
                            options.Insert(0, trade);
                    }
                    break;
                case MsgType.NPCInv:
                    {
                        options = new List<string>
                    {stats, inv, help};
                        if (player.Encounter != null && player.Encounter.npc != null
                            && player.Encounter.npc.inventory != null
                            && player.Encounter.npc.inventory.Count > 15)
                        {
                            options.Insert(0, next);
                            options.Insert(0, prev);
                        }
                    }
                    break;
                #endregion

                #region Combat
                case MsgType.Combat:
                    {
                        options = new List<string>
                        {
                            ability,
                         help
                        };
                        if (player != null)
                        {
                            if (player.IsEncounter("Combat"))
                            {
                                options.Insert(0, brawl);
                                if (player.Encounter.Name != Encounter.Names.PVP)
                                    options.Insert(1, run);
                            }
                        }
                    }
                    break;
                #endregion

                #region Character Set Up
                case MsgType.SetSkill:
                    {
                        options = new List<string>();
                        string[] arrays = data.Split(';');
                        bool[] rused = Utils.JSON<bool[]>(arrays[1]);
                        for (int i = 0; i < 6; i++)
                            if (!rused[i])
                                options.Add(GetLetter(i));
                    }
                    break;
                case MsgType.ChooseRace:
                    {
                        options = new List<string>()
                        {
                            GetLetter(7), GetLetter(19), GetLetter(20),
                            GetLetter(12), GetLetter(8)
                        };
                    }
                    break;
                case MsgType.AbilityLevel:
                    {

                        Ability a = player.abilities[int.Parse(data)];
                        if (a.level >= a.maxLevel && a.evolves != null)
                        {
                            options = new List<string>();
                            for (int i = 0; i < a.evolves.Length; i++)
                                options.Add(GetNum(i));
                        }
                        else options = new List<string>() { stats };
                    }
                    break;
                case MsgType.StarterAbilities:
                    {
                        options = new List<string>();
                        string[] split = data.Split('/');
                        int p = int.Parse(split[0]);
                        int x = int.Parse(split[1]);
                        int z = int.Parse(split[2]);
                        if (p != 0)
                            options.Add(prev);
                        if (p != 2)
                            options.Add(next);
                        for (int i = 0; i < 3; i++)
                            if (z != p || (z == p && i != x))
                                options.Add(GetNum(i));
                    }
                    break;
                case MsgType.AutoNewCharacter:
                    options = new List<string>()
                    { ok, next, info };
                    break;
                #endregion

                #region Area
                case MsgType.Adventure:
                    {
                        if (player.IsInAdventure)
                            options = new List<string>(new[] { cancel });
                        else if (data == null) options = new List<string>(new[] { ok });
                        else if (data[0] == 'D') //Select difficulty
                        {
                            options = new List<string>();
                            for (int i = 0; i < 4; i++) options.Add(EUI.GetNum(i + 1));
                        }
                        else //Select a quest
                        {

                        }
                    }
                    break;
                case MsgType.PetShop:
                    {

                        options = new List<string>()
                        {

                        };
                        if (data != null && data.Length > 2)
                            options.Add(mainQuest);
                    }
                    break;
                case MsgType.Tavern:
                    {
                        options = new List<string>()
                        {
                            bounties, sideQuest, Dice(1),
                        };
                    }
                    break;
                case MsgType.Junctions:
                    {
                        string[] args1 = data.Split(';');
                        int page = int.Parse(args1[0]);
                        int total = int.Parse(args1[1]);
                        string[] juncIds = Utils.JSON<string[]>(args1[2]);
                        options = new List<string>();
                        //
                        if (page > 0)
                            options.Add(prev);
                        if (juncIds.Length >= 5 && page * 5 < total)
                            options.Add(next);
                        for (int i = 0; i < juncIds.Length; i++)
                            options.Add(GetNum(i));

                    }
                    break;
                #endregion

                #region Arena
                case MsgType.ArenaGameMode:
                    {
                        options = new List<string>()
                        {prev, ok, cancel, next};
                    }
                    break;
                case MsgType.ArenaModifiers:
                    {
                        //          0       1   2    3    4
                        //date = gameMode;page;bool,bool,bool
                        string[] args = data.Split(';');

                        int.TryParse(args[1], out int i);
                        bool.TryParse(args[2].Split(',')[i], out bool b);

                        options = new List<string>()
                        {
                            uturn, prev,
                            b ? cancel : ok,
                            next,
                            enterFloor
                        };

                    }
                    break;
                #endregion

                #region Social
                case MsgType.OfferList:
                    {
                        string[] args2 = data.Split('.');
                        int page = int.Parse(args2[0]);
                        int total = int.Parse(args2[1]);
                        Guid[] guids = Utils.JSON<Guid[]>(args2[3]);
                        options = new List<string>();
                        if (page > 0)
                            options.Add(prev);
                        if ((page + 1) * 5 <= total)
                            options.Add(next);
                        if (guids.Length >= 5)
                            options.AddRange(new[] { one, two, three, four, five });
                        for (int i = 0; i < guids.Length; i++)
                            options.Add(GetNum(i + 1));
                    }
                    break;
                #endregion

                #region Specs
                case MsgType.SpecSelection:
                    {
                        options = new List<string>();
                        string[] args = data.Split(';');
                        foreach (string s in args)
                            if (int.TryParse(s, out int i))
                                options.Add(specs[i]);
                    }
                    break;
                case MsgType.SpecPerks:
                case MsgType.SpecAbility:
                    {
                        string[] itemIndexes = data.Split(';');
                        options = new List<string>() { uturn };
                        foreach (string s in itemIndexes)
                            if (int.TryParse(s, out int i))
                                options.Add(GetNum(i));
                    }
                    break;
                case MsgType.SpecMain:
                    options = new List<string>() { EUI.classAbility, EUI.classPerk };
                    break;
                #endregion

                #region Crates
                case MsgType.ResourceCrateList:
                    options = new List<string>()
                    {
                        GetNum(0),
                        GetNum(1),
                        GetNum(2),
                        GetNum(3),
                        GetNum(4),
                    };
                    break;
                case MsgType.ResourceCrateOpening:
                    {
                        int cAmount = int.Parse(data.Split(';')[1]);
                        options = new List<string>();
                        for (int i = 0; i < cAmount; i++)
                            options.Add(GetNum(i));
                    }
                    break;
                #endregion

                #region Quests
                case MsgType.QuestInfo:
                    {
                        int.TryParse(data, out int index);
                        options = new List<string>()
                        {
                            uturn
                        };
                        if (index > 0)
                            options.Add(prev);
                        if (index < player.quests.Count - 1)
                            options.Add(next);

                    }
                    break;
                case MsgType.QuestList:
                    {
                        int.TryParse(data, out int page);

                        options = new List<string>();
                        if (page > 0)
                            options.Add(prev);
                        int perpage = Items.Quests.Quest.PerPage;
                        for (int i = 0; i < perpage && (page * perpage) + i < player.quests.Count; i++)
                            options.Add(GetNum(i));
                        if (page < player.quests.Count / Items.Quests.Quest.PerPage)
                            options.Add(next);
                    }
                    break;
                case MsgType.AcceptQuests:
                    {
                        //data = id1,id2,id3;id1,id2,id3...
                        int questAmount = data.Split(';').Length - 1;
                        options = new List<string>();
                        for (int i = 0; i < questAmount; i++)
                            options.Add(GetNum(i));

                    }
                    break;
                case MsgType.DailyQuestBoard:
                    {
                        options = new List<string>();
                        string[] nums = data.Split(';');

                        for (int i = 0; i < nums.Length; i++)
                            if (int.TryParse(nums[i], out int k))
                                options.Add(GetNum(k));
                    }
                    break;
                #endregion

                #region Next/Previous Only
                case MsgType.BountyBoard:

                #endregion

                #region Okay/Cancel Only
                case MsgType.ConfirmTransaction:
                case MsgType.ConfirmSkills:
                case MsgType.ConfirmCharDel:
                case MsgType.EndRest:
                case MsgType.ConfirmUpgrade:
                case MsgType.PartyInvite:
                case MsgType.ConfirmOffer:
                case MsgType.InspectOffer:
                case MsgType.DuelOffer:
                case MsgType.ComfirmBuilding:
                case MsgType.ConfirmEggDiscard:
                case MsgType.DynastyUpgrade:
                case MsgType.DynastyInvite:
                    options = new List<string> { ok, cancel };
                    break;
                #endregion

                #region Gambling
                case MsgType.GamblingGames:
                    {
                        options = new List<string>();
                        switch (data)
                        {
                            case "Tavern":
                                options.Add(Dice(1));
                                options.Add(GetNum(0));
                                break;
                        }
                    }
                    break;
                case MsgType.DiceGame:
                    {
                        options = new List<string>()
                        {
                            prev,
                            EUI.Dice(1),
                            EUI.Dice(2),
                            next,
                            two, five, zero,
                            cancel
                        };
                    }
                    break;
                case MsgType.CardGame:
                    {
                        if (data.Split(';').Length > 1)
                        {
                            options = new List<string>() { prev, next, two, five, zero, ok, cancel };
                        }
                        else
                        {
                            Type type = GamblingGame.GetGameType(data);
                            Dictionary<string, string> actions = Utils.GetVar<Dictionary<string, string>>(type, "Actions", true);
                            options = new List<string>(actions.Keys);
                        }
                    }
                    break;
                #endregion

                #region Other
                case MsgType.Lottery:
                    options = new List<string>()
                    { ticket};
                    break;
                #endregion

                default:
                    if (OptionsLoad.ContainsKey(type))
                        OptionsLoad[type].Invoke(this);
                    break;
            }

        }
    }
}
