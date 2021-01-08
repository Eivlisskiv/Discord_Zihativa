using AMI.Methods;
using AMI.Neitsillia.Areas.AreaPartials;
using AMI.Neitsillia.User.PlayerPartials;
using AMI.Neitsillia.User.UserInterface;
using AMYPrototype.Commands;
using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AMI.Neitsillia.Areas.Arenas
{
    class Arena
    {
        internal enum ArenaMode { Survival, };
        static readonly string[] ModesDesc =
        {
            "Survive never ending waves of enemies with no breaks in between each fight.",
            "Test Mode 1.",
            "Test Mode 2.",
            "Test Mode 3.",
            "Test Mode 4.",
            "Test Mode 5.",
        };
            
        internal static async Task Service(Player player, ISocketMessageChannel chan)
        {
            await player.NewUI("", DUtils.BuildEmbed("Arena Lobby Services",
                $"{EUI.sideQuest} View arena quests" /*+ Environment.NewLine +
                $"{EUI.bounties} View arena challenges"*/,
                null, player.userSettings.Color()).Build(), chan, MsgType.ArenaService);
        }

        internal static async Task SelectMode(Player player, int i, ISocketMessageChannel chan, bool edit = false)
        {
            Array modes = Enum.GetValues(typeof(ArenaMode));
            //Loop index
            if (i >= modes.Length)
                i = 0;
            else if (i < 0)
                i = modes.Length - 1;
            //Create Embed
            EmbedBuilder embed = DUtils.BuildEmbed(
                "Arena", "Select Challenge", color : player.userSettings.Color(),
                fields : DUtils.NewField($"**{(ArenaMode)i}**", ModesDesc[i])
                );
            if (edit)
                await player.EditUI(null, embed.Build(), chan,
                    MsgType.ArenaGameMode, i.ToString());
            else
                await player.NewUI(await chan.SendMessageAsync(embed: embed.Build()),
             MsgType.ArenaGameMode, i.ToString());
        }

        internal static async Task SelectModifiers(Player player, string mode, int page,
            string boolArray, ISocketMessageChannel chan, bool edit = false)
        {
            Array mods = Enum.GetValues(typeof(ArenaModifier.ArenaModifiers));
            //Loop index
            if (page >= mods.Length)
                page = 0;
            else if (page < 0)
                page = mods.Length - 1;

            EmbedBuilder embed = DUtils.BuildEmbed(
                "Arena", "Activate Modifiers", color: player.userSettings.Color(),
                fields: DUtils.NewField( $"**{(ArenaModifier.ArenaModifiers)page}**" ,
                ArenaModifier.ModifiersDesc[page])
                );

            if(edit)
                await player.EditUI(null, embed.Build(), chan,
                    MsgType.ArenaModifiers, $"{mode};{page};{boolArray}");
            else
                await player.NewUI(await chan.SendMessageAsync(embed: embed.Build()),
                 MsgType.ArenaModifiers, $"{mode};{page};{boolArray}");
        }

        internal static async Task<Area> Generate(Area parent, Player player, int mode, string[] boolArray)
        {
            Area dungeon = new Area(AreaType.Arena,
                $"{parent.name} {(ArenaMode)mode}", parent)
            {
                eMobRate = 100,
                mobs = new List<string>[]
                {
                new List<string>{
                    "Young Vhoizuku",
                    "Vhoizuku",
                    "Vhoizuku",
                    "Vhoizuku Warrior",
                    "Vhoizuku Warrior",
                    "Vhoizuku Warrior",
                    "Vhoizuku Mother",
                }},

                /*Arena = new Arena((ArenaMode)mode)
                {
                    ParentID = parent.AreaId,
                    Modifiers = boolArray != null ? new ArenaModifier(boolArray) : null,
                }*/
            };

            await player.SetArea(dungeon);
            player.SaveFileMongo();

            return dungeon;
        }

        //This instance for for per player/party data

        public string ParentID;
        public ArenaMode gameMode;
        public ArenaModifier Modifiers;
        
        public Arena(ArenaMode mode)
        {
            gameMode = mode;
        }

        internal long KutyeiCoinsReward(long score)
        {
            switch(gameMode)
            {
                case ArenaMode.Survival: return NumbersM.NParse<long>(score * Modifiers.KoinMult);
            }
            return 0;
        }
        internal EmbedBuilder Explore(Area area, Player player, EmbedBuilder embed)
        {
            switch (gameMode)
            {
                case ArenaMode.Survival:
                    {

                        area.LocationMob(embed, player, 0);

                    }break;
            }
            return null;
        }


        
    }
    //TEST - IGNORE
    class ArenaScoreList
    {
        //same as area (ArenaLobby) id
        public string _id;
               //charname\userid, score
        public SortedList<string, long> PlayerScores { get; set; }

        public ArenaScoreList()
        {

        }
    }
    //TEST - IGNORE
    class ArenaModifier
    {
        internal enum ArenaModifiers { };
        internal static readonly string[] ModifiersDesc = 
        {
            "test 1",
            "test 2",
            "test 3",
            "test 4",
            "test 5",
        };

        //Active Arena
        public List<ArenaModifiers> ActiveMods = new List<ArenaModifiers>();

        //->//Party Stats
        public long CurrentScore = 0;
        public int wavesPerRounds = 5;
        //
        public double KoinMult = 1;
        public double XPMult = 1;

        public ArenaModifier(string[] bools)
        {
            for (int i = 0; i < bools.Length; i++)
                if (bools[i] == "1")
                {
                    ArenaModifiers m = (ArenaModifiers)i;
                    LoadMod(m);
                    ActiveMods.Add(m);
                }
        }

        void LoadMod(ArenaModifiers mod)
        {
            switch(mod)
            {
                default: //todo
                    break;
            }
        }
    }
}
