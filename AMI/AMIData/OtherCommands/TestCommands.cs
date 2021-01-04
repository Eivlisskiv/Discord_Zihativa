using AMI.Methods;
using AMI.Methods.Graphs;
using AMI.Neitsillia.Items;
using AMI.Neitsillia.NPCSystems;
using AMI.Neitsillia.User.PlayerPartials;
using AMYPrototype;
using AMYPrototype.Commands;
using Discord;
using Discord.Commands;
using Neitsillia.Items.Item;
using Neitsillia.Methods;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AMI.AMIData.OtherCommands
{
    public class TestCommands : ModuleBase<AMI.Commands.CustomSocketCommandContext>
    {
        [Command("Calculate Armor")]
        public async Task CalcArmor(int armor)
        {
            await DUtils.Replydb(Context, Exponential.Armor(armor).ToString() + "% Damage resistance");
        }
        [Command("Calculate Durability")]
        public async Task CalcDurability(int x)
        {
            await DUtils.Replydb(Context, Exponential.Durability(x).ToString() + "% Chance to decay condition.");
        }
        [Command("Test Array Rates")]
        [Alias("testAR")]
        public async Task TestArrayRatesTest(int length = 10)
        {
            string r = null;
            for (int i = 0; i < length; i++)
                r += $"{i} | {Math.Round(Exponential.RateFromIndex(length, i), 3)}%" + Environment.NewLine;
            EmbedBuilder ratesTest = new EmbedBuilder();
            ratesTest.WithTitle("Array Rate Calculation Test");
            ratesTest.AddField(" Array Length: " + length, r);
            await ReplyAsync(embed: ratesTest.Build());

        }
        [Command("TestTable")]
        [Alias("testt")]
        public async Task TestAreaFloorTest(string tables, int floor, int maxfloor = 100)
        {
            var l = tables.Split(';');
            List<string>[] table = new List<string>[l.Length];
            for(int i = 0; i < l.Length; i++)
                table[i] = new List<string>(l[i].Split(','));
            string lootb = ArrayM.ToString(table);
            string loota = "To do"; //ArrayM.ToString(ArrayM.FloorEffect(table, floor, maxfloor));
            EmbedBuilder test = new EmbedBuilder();
            test.WithTitle("Area Floor Effect Test");
            test.AddField("Table", "Before:" + Environment.NewLine + lootb
                + Environment.NewLine +
                "After:" + Environment.NewLine + loota);
            await ReplyAsync(embed: test.Build());
        }
        [Command("Test Next SkillPoint Gain")]
        [Alias("testnspg")]
        public async Task TestSPGain(int level = 0)
        {
            string res = null;
            int i = 5;
            int p = 0;
            for (; i < level;)
                i += p += 5;
            for (int j = 1; j < 10; j++)
            {
                res += i + Environment.NewLine;
                i += p += 5;
            }
            res += i + Environment.NewLine;
            EmbedBuilder em = new EmbedBuilder();
            em.WithTitle("Next Skill Point Rewards");
            em.WithDescription(res);
            var m = new CharacterMotherClass
            {
                level = level
            };
            em.WithFooter(m.IsGainSkillPoint().ToString());
            await ReplyAsync(embed: em.Build());
        }
        [Command("Generate Key")]
        [Alias("genkey")]
        public async Task RandomKeyTest(int length = 10)
        {
            await ReplyAsync(RNG.GenerateKey(length));
        }
        [Command("DBL Voters")]
        public async Task DBL_Voters()
        {
            var list = (await Program.dblAPI.GetVotersAsync()).GroupBy(u => u.Username)
                .ToDictionary(u => u.First().Username, u => u.Count());

            string display = "";
            int i = 0;
            for (; i < list.Count && display.Length < 1010; i++)
            {
                display += $"{list.ElementAt(i)}{Environment.NewLine}";
            }
            if (display.Length >= 1010)
                display += $"+ {list.Count - i} More";

            EmbedBuilder em = new EmbedBuilder();
            em.WithTitle($"Voters");
            em.WithDescription(display);
            await ReplyAsync(embed: em.Build());
        }
        [Command("Ability Level")]
        public async Task Ability_Level(int level, params string[] arg)
        {
            Ability ability = Ability.Load(StringM.UpperAt(ArrayM.ToString(arg)), level);
            await ReplyAsync(embed: ability.InfoPage(new EmbedBuilder(), true).Build());
        }
        [Command("Enemy Level")]
        public async Task Enemy_Level(int level, params string[] args)
        {
            Context.AdminCheck();

            string name = StringM.UpperFormat(args);
            NPC mob = NPC.GenerateNPC(level, name);
            await (mob == null ? ReplyAsync("Creature not found") :
            ReplyAsync(embed: mob.StatEmbed().Build()));
        }
        [Command("CharacterLevel")]
        public async Task Character_Level(int level)
        {
            Context.AdminCheck();

            CharacterMotherClass character = new CharacterMotherClass();

            for(int i = 0; i < 3; i++)
                character.equipment.Equip(Item.RandomItem(level * 5, 5, false), i);

            for(int i = 0; i < 6; i++)
                character.equipment.Equip(Item.RandomItem(level * 5, i + 6, false));

            await ReplyAsync(embed: character.StatEmbed().Build());
        }

        [Command("Random Item")]
        async Task RandomItem(int tier, int type = -1)
        {
            Context.AdminCheck();

            Item rng = Item.RandomItem(tier, type);
            await ReplyAsync(embed: rng.EmdebInfo(new EmbedBuilder()).Build());
        }

        [Command("AbilityTree")]
        async Task AbilityTree()
        {
            Context.AdminCheck();
            await ReplyAsync(Ability.StarterAbilityTree());
        }

        [Command("TestGearScale")]
        public async Task TestGearScale(int tier = 0, int type = -1)
        {
            Context.AdminCheck();

            Item generated = Item.RandomItem(tier, type);
            generated.Scale(tier);
            Player player = Player.Load(Context.BotUser, Player.IgnoreException.All);
            Item compare = player.equipment.GetGear(generated.type);

            EmbedBuilder embed = compare?.CompareTo(generated, 0) ?? generated.EmdebInfo(new EmbedBuilder());

            await ReplyAsync(embed: embed.Build());
        }
        [Command("TestNumberDisplay")]
        public async Task TestNumberDisplay(long n) => await ReplyAsync(Utils.Display(n));

        [Command("Graphs")]
        public async Task GetMathGraphs()
        {
            EmbedBuilder embed = DUtils.BuildEmbed("RPG Equations", "The following are equations used in the RPG game",
                null, Color.DarkRed,

                DUtils.NewField("Mob Scaling", 
                "Last Updated: August 5th 2020"
                + Environment.NewLine + "https://www.desmos.com/calculator/v1fxt29dir"
                )
                
                );
            await ReplyAsync(embed: embed.Build());
        }

        [Command("Benchmark")]
        public async Task BenchmarkCommand(int i)
        {
            if (i == 1) await Benchmark1();
            if (i == 2) await Benchmark2();
        }

        public async Task Benchmark1(int i = 0)
        {
            if (i > 50) await ReplyAsync("Ping pong");
            else await Benchmark1(i + 1);
        }

        public async Task Benchmark2(int i = 0)
        {
            if (i > 50)  throw Module.NeitsilliaError.ReplyError("Ping pong");
            else await Benchmark2(i + 1);
        }
    }
}
