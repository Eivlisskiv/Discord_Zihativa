using AMI.Methods;
using AMI.Neitsillia.Items;
using AMI.Neitsillia.Items.Perks.PerkLoad;
using AMI.Neitsillia.User.PlayerPartials;
using AMI.Neitsillia.User.UserInterface;
using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AMI.Neitsillia.User.Specialization
{
    class Specialization
    {
        internal enum Specs { Joker, Fighter, Healer, Blacksmith,
            //Rogue, Caster
        };
        internal static string[] SpecDescription =
        {
            "The Joker can mimic any class with no restriction. [Only available for Platinum Premium]",
            "Fighters focus on dealing damage using physical attack and surviving their many fights.",
            "Healers specialize in healing abilities to maintain their own or their party member's health and defensive. ",
            "Blacksmith focus on custom gear. They gain bonuses for crafted items and combat advantages from custom gear. ",
            //"Rogue use their skills to leech health, stamina and coins of enemies.",
            //"Caster specialize in their non physical abilities and confront their opponents with tactical moves. ",

        };
        internal static int CurrentPerkAmount = 3;
        internal static int CurrentAbilityAmount = 1;

        //Class variables

        //Vars
        public Specs specType;
        public int specPoints;

        public List<string> AbilitiesObtained = new List<string>();
        public List<string> PerksObtained = new List<string>();

        //Params
        internal int SpentPoints => (AbilitiesObtained.Count * 50) + (PerksObtained.Count * 20);

        //Static Methods
        internal static async Task SpecializationChoice(Player player, ISocketMessageChannel chan)
        {
            EmbedBuilder embed = player.UserEmbedColor();
            embed.WithTitle("Character Class/Specialization");
            embed.WithDescription("**Class may not be changed.**");
            string data = null;
            if (player.IsPremium(5))
            {
                data += "0;";
                embed.AddField($"{EUI.specs[0]} Joker", SpecDescription[0]);
            }
            for (int i = 1; i < SpecDescription.Length; i++)
            {
                embed.AddField($"{EUI.specs[i]} {(Specs)i}", SpecDescription[i]);
                data += $"{i};";
            }
            await player.NewUI(await chan.SendMessageAsync(embed: embed.Build()), MsgType.SpecSelection, data);
        }
        internal static async Task LoadChosenSpec(Player player, int specindex, ISocketMessageChannel chan)
        {
            player.Specialization = new Specialization(specindex, player.level);
            player.SaveFileMongo();
            await chan.SendMessageAsync($"{player.name} is now specialized as a {player.Specialization.specType} with {player.Specialization.specPoints} spendable Spec Points"
                + Environment.NewLine + $"A new reaction option ({EUI.SpecIcon((int)player.Specialization.specType)}) is available under ``~xp``, use it to purchase perks and abilities.");
        }

        internal static void ResetSpec(Player player)
        {
            if(player.Specialization != null)
            {
                player.Specialization.Reset(player);
                player.Specialization = null;
                player.SaveFileMongo();
            }
        }
            
        //Class Methods
        public Specialization(int specindex, int currentPoints)
        {
            specType = (Specs)specindex;
            specPoints = currentPoints;
        }

        void Reset(Player player)
        {
            foreach(string ability in AbilitiesObtained)
            {
                player.abilities.RemoveAt(
                player.abilities.FindIndex(item => {

                     return item.name == ability;

                }));
            }

            foreach (string perk in PerksObtained)
            {
                player.perks.RemoveAt(
                player.perks.FindIndex(item => {

                    return item.name == perk;

                }));
            }
        }

        internal async Task MainMenu(Player player, ISocketMessageChannel chan)
        {
            EmbedBuilder e = new EmbedBuilder();
            e.WithTitle(specType.ToString() + " " + player.name);
            e.WithDescription(
                $"{EUI.classAbility} Class Abilities: {AbilitiesObtained.Count}/{CurrentAbilityAmount} " + Environment.NewLine +
                $"{EUI.classPerk} Class Perks: {PerksObtained.Count}/{CurrentPerkAmount}");
            e.WithFooter($"{specPoints} Specialization Points");
            await player.NewUI(await chan.SendMessageAsync(embed: e.Build()), MsgType.SpecMain);
        }

        //Titles
        internal string GetTitle()
        {
            int tier = SpentPoints/54;

            switch(specType)
            {
                case Specs.Blacksmith:
                    return new string[]{
                        "Blacksmith",
                        "Rookie Blacksmith",
                        "Adept Blacksmith",
                        "Expert Blacksmith",
                        "Master Blacksmith",

                    }[tier];
                case Specs.Healer:
                    return new string[]{
                        "Healer",
                        "Shaman",

                    }[tier];
                default: return specType.ToString();
            }
        }

        //Abilities and Perks
        internal List<string> GetAbilityList()
        => Utils.RunMethod<List<string>>(specType.ToString() + "Ability", this);
        internal List<string> GetPerkList()
        => Utils.RunMethod<List<string>>(specType.ToString() + "Perks", this);

        internal EmbedBuilder AbilityListEmbed(Player player, out string available)
        {
            EmbedBuilder e = new EmbedBuilder();
            List<string> abilities = GetAbilityList();
            available = "";
            for (int i = 0; i < abilities.Count; i++)
            {
                string fieldTitle = abilities[i];
                if (AbilitiesObtained.Contains(abilities[i]))
                    fieldTitle += " [Owned]";
                else if (specPoints < 50)
                    fieldTitle += $" [Missing {50 - specPoints} Spec Points]";
                else
                    available += $";{i}";
                e.AddField(fieldTitle, Ability.Load(abilities[i]).description);
            }
            e.WithFooter($"{specPoints} Specialization Points");
            return e;
        }
        internal async Task ShowAbilityList(Player player, ISocketMessageChannel chan)
        {
            EmbedBuilder e = AbilityListEmbed(player, out string available);
            await player.NewUI(await chan.SendMessageAsync(embed: e.Build()), MsgType.SpecAbility, available);
        }
        internal string PurchaseAbility(Player player, int i)
        {
            if (specPoints >= 50)
            {
                List<string> list = GetAbilityList();
                if (player.HasAbility(list[i], out _))
                {
                    AbilitiesObtained.Add(list[i]);
                    player.abilities.Add(Ability.Load(list[i]));
                    specPoints -= 50;
                    player.SaveFileMongo();
                    return list[i];
                }
                return $"Player already has ability {list[i]}";
            }
            return $"Missing {50-specPoints} spec points";
        }

        internal EmbedBuilder PerkListEmbed(Player player, out string available)
        {
            EmbedBuilder e = new EmbedBuilder();
            List<string> perks = GetPerkList();
            available = "";
            for (int i = 0; i < perks.Count; i++)
            {
                string r = null;
                if (player.HasPerk(perks[i]) > -1)
                    r = " [Owned]";
                else if (specPoints < 20)
                    r += $" [Missing {20 - specPoints} Spec Points]";
                else
                    available += $";{i}";
                e.AddField($"{EUI.GetNum(i)} {perks[i]} {r}", PerkLoad.Load(perks[i]).desc);
            }
            e.WithFooter($"{specPoints} Specialization Points");
            return e;
        }
        internal async Task ShowPerkList(Player player, ISocketMessageChannel chan)
        {
            EmbedBuilder e = PerkListEmbed(player, out string available);
            await player.NewUI(await chan.SendMessageAsync(embed: e.Build()), MsgType.SpecPerks, available);
        }
        internal string PurchasePerk(Player player, int i)
        {
            if (specPoints >= 20)
            {
                List<string> list = GetPerkList();
                if (player.HasPerk(list[i]) == -1)
                {
                    PerksObtained.Add(list[i]);
                    player.perks.Add(PerkLoad.Load(list[i]));
                    specPoints -= 20;
                    player.SaveFileMongo();
                    return list[i];
                }
                return $"Player already has perk {list[i]}";
            }
            return $"Missing {20-specPoints} spec points";
        }

        //Abilities
        public List<string> JokerAbility()
        {
            return new List<string>()
            {
                "Full Impact",
                "Healing Seed",
                "Execute",
            };
        }
        public List<string> FighterAbility()
        {
            return new List<string>()
            {
                "Execute",
            };
        }
        public List<string> CasterAbility()
        {
            return new List<string>()
            {

            };
        }
        public List<string> HealerAbility()
        {
            return new List<string>()
            {
                "Healing Spore",
            };
        }
        public List<string> BlacksmithAbility()
        {
            return new List<string>()
            {
                "Full Impact",
            };
        }
        public List<string> RogueAbility()
        {
            return new List<string>()
            {

            };
        }
        //Perks
        public List<string> JokerPerks()
        {
            return new List<string>()
            {
                "Unstoppable Force",
                "Precision Enhancement",
                //
                "Adrenaline",
                "Energizing Touch",
                //
                "Built To Last",
                "Reinforced Materials",
                "Finishing Touch",
            };
        }
        public List<string> FighterPerks()
        {
            return new List<string>()
            {
                "Unstoppable Force",
                "Precision Enhancement",
                "Fighting Spirit",
            };
        }
        public List<string> CasterPerks()
        {
            return new List<string>()
            {
                "",
            };
        }
        public List<string> HealerPerks()
        {
            return new List<string>()
            {
                "Adrenaline",
                "Energizing Touch",
                "Adaptation",
            };
        }
        public List<string> BlacksmithPerks()
        {
            return new List<string>()
            {
                "Built To Last",
                "Reinforced Materials",
                "Finishing Touch",
            };
        }
        public List<string> RoguePerks()
        {
            return new List<string>()
            {
                "",
            };
        }
    }
}
