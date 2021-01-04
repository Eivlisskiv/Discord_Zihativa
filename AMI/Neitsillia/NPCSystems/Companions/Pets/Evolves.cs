using AMI.Methods;
using AMI.Module;
using AMI.Neitsillia.Items;
using AMI.Neitsillia.Items.Perks.PerkLoad;
using AMYPrototype;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AMI.Neitsillia.NPCSystems.Companions.Pets
{
    static class Evolves
    {
        #region Evolve Options

        /*private static KeyValuePair<string, Dictionary<string, (int, string)[]>> 
            BuildTree(string race, params KeyValuePair<string, (int, string)[]>[] sub)
        {
            Dictionary<string, (int, string)[]>
        }//*/

        private static Dictionary<string, (int, string)[]>
            BuildTree(params KeyValuePair<string, (int, string)[]>[] sub)
        {
            return sub.ToDictionary(x => x.Key, y => y.Value);
        }

        private static KeyValuePair<string, (int, string)[]> BuildSubTree
            (string name, params (int, string)[] evolv)
        {
            return new KeyValuePair<string, (int, string)[]>(name, evolv);
        }

        internal static (int, string)[] GetOptions(string race, string name)
        {
            var property = Utils.GetFunction(typeof(Evolves), "Options_" + race, true);
            if (property == null) return null;

            var dict = (Dictionary<string, (int, string)[]>)property.Invoke(null, null);

            if (!dict.ContainsKey(name)) return null;

            return dict[name];
        }

        public static Dictionary<string, (int, string)[]> Options_Vhoizuku() => BuildTree(
            BuildSubTree("Young Vhoizuku", (10, "Vhoizuku")),
            BuildSubTree("Vhoizuku", (35, "Vhoizuku Warrior")),
            BuildSubTree("Vhoizuku Warrior", (50, "Vhoizuku Mother"), (60, "Vhoizuku King"))
            );


        #endregion

        #region Evolve
        internal static string Evolve(Pet pet, string ev)
        {
            var methode = Utils.GetFunction(typeof(Evolves), "Evolve_" + ev.Replace(" ", ""));
            if (methode == null) throw NeitsilliaError.ReplyError("Evolve unavailable");

            string reply = (string)methode.Invoke(null, new[] { pet.pet });
            pet.pet.name = ev;
            return reply;
        }

        #region Vhoizuku
        public static string Evolve_Vhoizuku(NPC pet)
        {
            pet.stats.maxhealth += 100;
            pet.stats.stamina += 25;
            pet.stats.resistance[0] += 10;
            pet.stats.damage[0] += 20;

            pet.stats.endurance += 1;

            pet.stats.critChance += 10;
            pet.stats.critMult += 10;

            pet.perks.Add(PerkLoad.PrickledSkin("Prickled Skin"));

            return $"{pet.displayName} has evolved into a Vhoizuku. "
                + Environment.NewLine + $"+ Base stats"
                + Environment.NewLine + $"+ Prickled Skin perk";
        }
        public static string Evolve_VhoizukuWarrior(NPC pet)
        {
            pet.stats.maxhealth += 120;
            pet.stats.stamina += 35;

            pet.stats.resistance[0] += 25;
            pet.stats.resistance[Program.rng.Next(pet.stats.resistance.Length)] += 25;

            pet.stats.damage[0] += 25;
            pet.stats.damage[Program.rng.Next(pet.stats.damage.Length)] += 15;

            pet.stats.endurance += 2;
            pet.stats.strength += 2;

            pet.stats.critChance += 10;
            pet.stats.critMult += 10;

            pet.perks.Add(PerkLoad.RandomPerk(0, "Character"));

            return $"{pet.displayName} has evolved into a Vhoizuku Warrior. "
                + Environment.NewLine + $"+ Base stats"
                + Environment.NewLine + $"+ Random damage and Resistance"
                + Environment.NewLine + $"+ Random perk";
        }
        public static string Evolve_VhoizukuMother(NPC pet)
        {
            pet.stats.maxhealth += 500;
            pet.stats.stamina += 35;

            pet.stats.resistance[0] += 15;
            pet.stats.resistance[Program.rng.Next(pet.stats.resistance.Length)] += 15;
            pet.stats.resistance[Program.rng.Next(pet.stats.resistance.Length)] += 15;

            pet.stats.damage[0] += 25;

            pet.stats.critChance = 60;
            pet.stats.critMult = 80;

            pet.stats.endurance += 2;
            pet.stats.dexterity += 2;

            pet.perks.Add(PerkLoad.RandomPerk(0, "Character"));
            pet.perks.Add(PerkLoad.RandomPerk(0, "Character"));

            return $"{pet.displayName} has evolved into a Vhoizuku Mother. "
                + Environment.NewLine + $"+ Base stats"
                + Environment.NewLine + $"+ 2 Random Resistance"
                + Environment.NewLine + $"+ 2 Random perk";
        }
        public static string Evolve_VhoizukuKing(NPC pet)
        {
            pet.stats.maxhealth += 600;
            pet.stats.stamina += 35;

            pet.stats.resistance[0] += 25;

            pet.stats.damage[0] += 75;
            pet.stats.damage[Program.rng.Next(pet.stats.damage.Length)] += 25;
            pet.stats.damage[Program.rng.Next(pet.stats.damage.Length)] += 25;

            pet.stats.perception += 2;
            pet.stats.strength += 2;

            pet.stats.critChance = 50;
            pet.stats.critMult = 100;

            pet.perks.Add(PerkLoad.RandomPerk(0, "Character"));
            pet.perks.Add(PerkLoad.RandomPerk(0, "Character"));

            return $"{pet.displayName} has evolved into a Vhoizuku King. "
                + Environment.NewLine + $"+ Base stats"
                + Environment.NewLine + $"+ 2 Random damage"
                + Environment.NewLine + $"+ 2 Random perk";
        }
        #endregion
        #endregion
    }
}
