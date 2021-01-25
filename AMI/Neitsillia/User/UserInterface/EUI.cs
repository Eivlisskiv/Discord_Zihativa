using Discord;
using Neitsillia.Items.Item;
using System;
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
        {
            switch (i)
            {
                case zero: return 0;
                case one: return 1;
                case two: return 2;
                case three: return 3;
                case four: return 4;
                case five: return 5;
                case six: return 6;
                case seven: return 7;
                case eight: return 8;
                case nine: return 9;
                default: return -1;
            }
        }

        internal static string GetLetter(int i)
        {
            switch (i)
            {
                case 0: return "🇦"; //U+1F1E6
                case 1: return "🇧";
                case 2: return "🇨";
                case 3: return "🇩";
                case 4: return "🇪";
                case 5: return "🇫";
                case 6: return "🇬";
                case 7: return "🇭";
                case 8: return "🇮";
                case 9: return "🇯";
                case 10: return "🇰";
                case 11: return "🇱";
                case 12: return "🇲";
                case 13: return "🇳";
                case 14: return "🇴";
                case 15: return "🇵";
                case 16: return "🇶";
                case 17: return "🇷";
                case 18: return "🇸";
                case 19: return "🇹";
                case 20: return "🇺";
                case 21: return "🇻";
                case 22: return "🇼";
                case 23: return "🇽";
                case 24: return "🇾";
                case 25: return "🇿"; // 	U+1F1FF
            }
            return null;
        }

        internal static int GetLetter(string i)
        {
            switch (i)
            {
                case "🇦": return 0; //U+1F1E6
                case "🇧": return 1;
                case "🇨": return 2;
                case "🇩": return 3;
                case "🇪": return 4;
                case "🇫": return 5;
                case "🇬": return 6;
                case "🇭": return 7;
                case "🇮": return 8;
                case "🇯": return 9;
                case "🇰": return 10;
                case "🇱": return 11;
                case "🇲": return 12;
                case "🇳": return 13;
                case "🇴": return 14;
                case "🇵": return 15;
                case "🇶": return 16;
                case "🇷": return 17;
                case "🇸": return 18;
                case "🇹": return 19;
                case "🇺": return 20;
                case "🇻": return 21;
                case "🇼": return 22;
                case "🇽": return 23;
                case "🇾": return 24;
                case "🇿": return 25; // 	U+1F1FF
            }
            return -1;
        }

        internal static string SpecIcon(int i)
        {
            if (i < 0 || i > specs.Length - 1)
                return "";
            return specs[i];
        }
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
