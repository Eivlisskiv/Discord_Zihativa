using AMI.Neitsillia.Items.ItemPartials;
using Discord;
using System.Collections.Generic;

namespace AMI.Neitsillia.User.UserInterface
{
    static class EUI
    {
        #region Emotes
        internal const string info = "ℹ️";

        internal const string prev = "◀";
        internal const string next = "▶";
        internal const string cycle = "🔄";
        internal const string uturn = "↩️";
        internal const string sheet = "📝";
        internal const string stats = "📊";
        internal const string equip = "🛡";
        internal const string ability = "🔰";
        internal const string xp = "🏆";
        internal const string inv = "🎒";
        internal const string storage = "📦";
        internal const string schem = "🛠";
        internal const string loot = "💰";
        internal const string trade = "💱";
        internal const string brawl = "⚔";
        internal const string run = "🏃🏻";
        internal const string explore = "🗺";
        internal const string tpost = "🚩";
        internal const string help = "❓";
        internal const string skills = "🌟";
        internal const string pickSpec = "🎭";
        internal const string npc = "👤";
        internal const string enterFloor = "⬇";
        internal const string ticket = "🎟";

        internal const string egg = "🥚";
        internal const string eggPocket = "👝";
        internal const string pets = "🐾";
        internal const string summon = "↖️";
        internal const string whistle = "<:whistle:729830584184078347>";

        internal const string bounties = "💀";
        internal const string sideQuest = "❔";
        internal const string mainQuest = "❗";
        internal const string eventQuest = "‼️";
        //
        internal const string health = "❤️";
        internal const string brokenhealth = "💔";
        internal const string stamina = "⚡";
        internal const string tired = "💨";

        internal const string shield = "<:RES:729709743157018674>";
        internal const string attack = "<:ATK:729709741881688076>";

        internal const string greaterthan = "<:UpGreen:677220681070411796>";
        internal const string lowerthan = "🔻";
        internal const string equalStats = "🔸";

        internal const string building = "🏘";
        internal const string explosive = "💥";
        internal const string produce = "🔧";
        internal const string collect = "📥";

        #region Numbers
        internal const string zero = "\u0030\u20e3";
        internal const string one = "\u0031\u20e3";
        internal const string two = "\u0032\u20e3";
        internal const string three = "\u0033\u20e3";
        internal const string four = "\u0034\u20e3";
        internal const string five = "\u0035\u20e3";
        internal const string six = "\u0036\u20e3";
        internal const string seven = "\u0037\u20e3";
        internal const string eight = "\u0038\u20e3";
        internal const string nine = "\u0039\u20e3";
        #endregion

        #region Dices
        private static List<string> dices = new List<string>
        {
            "<:Dice_1_Dark:721087774609899660>",
            "<:Dice_2_Dark:721087834668138538>",
        };
        internal static string Dice(int i)
            => dices[i - 1];
        internal static int Dice(string d)
        {
            int i = dices.FindIndex(x => x == d);
            return i == -1 ? i : i + 1;
        }
        #endregion

        #region Cards
        internal const string card_hit = "<:plus_card:723651321189761095>";
        internal const string card_stand = "🎴";
        #endregion

        #region Others
        internal static string ItemType(Item.IType type)
        {
            switch (type)
            {
                case Item.IType.Material: return "<:material:737722612108623942>";
                case Item.IType.Healing:
                case Item.IType.Consumable:
                    return "<:consumable:737723890801049732>";

                case Item.IType.Usable:
                case Item.IType.Schematic:
                case Item.IType.RepairKit:
                case Item.IType.Rune:
                case Item.IType.Mysterybox:
                    return "";
                default: return null;
            }
        }

        internal static string ItemRarity(Item item)
        {
            if (!item.CanBeEquip()) return null;
            bool perk = item.perk != null;
            //                              Red     Orange
            if (perk) return item.isUnique ? "<:Red:808027517272588378>" 
                    : "<:Orange:808027517058547743>";
            if (item.isUnique) return "<:Purple:808027517063659520>"; //Purple

            return (item.rarity > 3) ? "<:Blue:808027516748431384>" //Blue
            : (item.baseTier > 2) ? "<:Green:808032848044228630>" //Green
            : "<:White:808032848305061898>";
        }

        internal const string ok = "<:confirm:717080796908748910>"; //"✅";
        internal const string cancel = "<:cancel:717080808099414118>";//"❌";
        //
        internal static string[] specs =
        {
            "🃏", //Joker
            "🗡️", //Fighter
            "💗", //Healer
            "⚒️", //Blacksmith
            //"🕵️", //Rogue
            //"✨", //Caster
        };
        internal const string classAbility = "💠";
        internal const string classPerk = "✳️";
        #endregion

        #endregion

        #region Parsing
        internal static IEmote ToEmote(string e)
        {
            if (e[0] == '<')
                return Emote.Parse(e);
            else
                return new Emoji(e);
        }
        internal static string GetNum(int i)
        {
            //return "\u00" + 30 + i + "\u20e3";
            switch (i)
            {
                case 0: return zero;
                case 1: return one;
                case 2: return two;
                case 3: return three;
                case 4: return four;
                case 5: return five;
                case 6: return six;
                case 7: return seven;
                case 8: return eight;
                case 9: return nine;
                default: return null;
            }
        }

        internal static string GetElement(int k) => GetElement((ReferenceData.DamageType)k);
        internal static string GetElement(ReferenceData.DamageType k)
        {
            switch (k)
            {
                case ReferenceData.DamageType.Physical: return "<:Physical:729706182175883334>";
                case ReferenceData.DamageType.Blaze: return "<:Fire:729706176543195236>";
                case ReferenceData.DamageType.Cold: return "<:Cold:729706175922438165>";
                case ReferenceData.DamageType.Toxic: return "<:Toxic:729706181924225086>";
                case ReferenceData.DamageType.Electric: return "<:Electric:729706181777424405>";
            }
            return null;
        }

        internal static ReferenceData.DamageType GetElement(string k)
        {
            switch (k)
            {
                case "<:Physical:729706182175883334>": return ReferenceData.DamageType.Physical;
                case "<:Fire:729706176543195236>": return ReferenceData.DamageType.Blaze;
                case "<:Cold:729706175922438165>": return ReferenceData.DamageType.Cold;
                case "<:Toxic:729706181924225086>": return ReferenceData.DamageType.Toxic;
                case "<:Thunder:729706181777424405>": return ReferenceData.DamageType.Electric;
            }
            return ReferenceData.DamageType.Physical;
        }

        internal static int GetNum(string i) 
            => i switch { zero => 0, one => 1,
                two => 2, three => 3, four => 4,
                five => 5, six => 6, seven => 7,
                eight => 8, nine => 9, _ => -1,
            };

        internal static string GetLetter(int i)
            => i switch { 0 => "🇦", 1 => "🇧",
                2 => "🇨", 3 => "🇩", 4 => "🇪",
                5 => "🇫", 6 => "🇬", 7 => "🇭",
                8 => "🇮", 9 => "🇯", 10 => "🇰",
                11 => "🇱", 12 => "🇲", 13 => "🇳",
                14 => "🇴", 15 => "🇵", 16 => "🇶",
                17 => "🇷", 18 => "🇸",  19 => "🇹",
                20 => "🇺", 21 => "🇻", 22 => "🇼",
                23 => "🇽", 24 => "🇾", 25 => "🇿",
                _ => null,
            };

        internal static int GetLetter(string i)
            => i switch { "🇦" => 0, "🇧" => 1,
                "🇨" => 2, "🇩" => 3, "🇪" => 4,
                "🇫" => 5, "🇬" => 6, "🇭" => 7,
                "🇮" => 8, "🇯" => 9, "🇰" => 10,
                "🇱" => 11, "🇲" => 12, "🇳" => 13,
                "🇴" => 14, "🇵" => 15, "🇶" => 16,
                "🇷" => 17, "🇸" => 18, "🇹" => 19,
                "🇺" => 20, "🇻" => 21, "🇼" => 22,
                "🇽" => 23, "🇾" => 24, "🇿" => 25,
                _ => -1,
            };

        internal static string SpecIcon(int i)
            => (i < 0 || i > specs.Length - 1)
            ? "" : specs[i];
        #endregion

        internal static string GetReactionDescription(string s, MsgType type)
        {
            return s switch
            {
                ok => type switch
                {
                    MsgType.ArenaModifiers => "Activate Modifier",
                    _ => "Confirm",
                },
                cancel => type switch
                {
                    MsgType.ArenaModifiers => "Deactivate Modifier",
                    MsgType.SetSkill => "Re assign stats",
                    MsgType.Adventure => "End or Cancel adventure",
                    _ => "Cancel",
                },
                prev => "Go to previous page",
                next => "Go to next page",
                uturn => type switch
                {
                    MsgType.ArenaModifiers => "Return to challenge selection.",
                    _ => "Back",
                },
                cycle => type switch
                {
                    MsgType.Inventory => "Cycle filter type [all > gear > consumable]",
                    MsgType.Adventure => "Refresh",
                    _ => "Cycle"
                },
                stats => "View short stats sheet (use ``~ls`` to view full stats)",
                sheet => "View custom character information",
                //Ability\Leveling
                ability => "View character abilities and levels",
                xp => "View character level and level options",
                pickSpec => "Select character specialization",
                skills => "Spend stat point",
                classAbility => "View available specialization Abilities",
                classPerk => "View available specialization perks",
                //
                inv => "View inventory",
                schem => "View known permanent schematics",
                loot => "Loot all loot (Use ``~view loot`` to view the list and ``~loot #`` to loot specifics)",
                trade => type == MsgType.Event ? "View event shop" : "View NPC's trading inventory",
                //Combat
                brawl => "Reuse last attack on last target (default: Brawl on m0)",
                run => "Attempt to run from combat",
                //Exploring
                tpost => "View and travel to accessible areas",
                explore => "Explore current area",
                enterFloor => type switch
                {
                    MsgType.ArenaModifiers => "Enter Arena",
                    _ => "Enter Floor/Dungeon",
                },
                ticket => "Buy a lottery ticket",
                eventQuest => "View Event Quests",
                mainQuest => "View Main Quests",
                sideQuest => "View Side Quests",

                _ => OtherReactionDescription(s, type),
            };
        }
        private static string OtherReactionDescription(string s, MsgType type)
        {
            if (GetLetter(s) > -1)
            {
                switch (type)
                {
                    case MsgType.SetSkill:
                        return $"Select roll {s} for the indicated stat";
                    case MsgType.EventShop:
                        if (s == "🇮") return "Purchase 1";
                        if (s == "🇻") return "Purchase 5";
                        if (s == "🇽") return "Purchase 10";
                        if (s == "🇲") return "Purchase maximum";

                        break;
                }
            }

            foreach (string specS in specs)
                if (s.Equals(specS))
                    return "Specialization abilities and perks.";
            //
            return null;
        }
    }
}
