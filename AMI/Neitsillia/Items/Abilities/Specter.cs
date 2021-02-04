using AMI.Methods;
using AMI.Methods.Graphs;
using AMI.Neitsillia.User.PlayerPartials;
using AMYPrototype.Commands;
using Discord;
using Neitsillia.Items.Item;
using System;

namespace AMI.Neitsillia.Items.Abilities
{
    public class Specter
    {
        //--Statics--//

        internal const int TierLevel = 25;

        static readonly string[][] Abilities = new string[][]
        {
            new string[]{ "Heat" },
            new string[]{ "Toxin" },
            new string[]{ "Static" },
        };

        internal static string Get(ReferenceData.DamageType element, int tier) => Abilities[(int)element][tier];
        internal static string Get(int element, int tier) => Abilities[element][tier];
        internal static string Get(int tier) => Utils.RandomElement(Abilities)[tier];

        //--Instance--//
        internal int Tier => level/ TierLevel;

        public int level;
        public long xp;

        public Ability essence;

        //--Methods--//
        public Specter() { }
        /// <summary>
        /// Equips the essence and applies the ability
        /// </summary>
        /// <param name="e">New essence to equip</param>
        /// <returns>string result</returns>
        internal bool Equip(Item e)
        {
            if (e.tier > Tier) return false;

            essence = Ability.Load(e.originalName, level - (e.tier * 25));

            return true;
        }

        internal void GainXP(long amount)
        {
            if (amount < 0 || essence.tier < Tier) return;

            long req = Quadratic.F_longQuad(level + 1, ReferenceData.xpToLvlAbility * (Tier + 1), 0, 0);
            while(xp >= req)
            {
                level++;
                xp -= req;
                req = Quadratic.F_longQuad(level + 1, ReferenceData.xpToLvlAbility * (Tier + 1), 0, 0);
            }
        }

        public override string ToString() => $"{essence?.name ?? "Virgin"} Specter";

        public EmbedBuilder Info(Player player)
        {
            return DUtils.BuildEmbed($"{player.name}'s Binded Specter", null, null, player.userSettings.Color,
                DUtils.NewField("Power", $"Tier [{Tier}] | Level { Utils.XpDetail(level, xp, ReferenceData.xpToLvlAbility * (Tier + 1))}"),
                DUtils.NewField(essence?.name ?? "Essence", essence == null ? "Virgin Specter are without essence. Use a `Essence Vial` on your specter to grant it an element."
                : essence.description + Environment.NewLine + Environment.NewLine + essence.GetStats())
                );
        }
    }
}
