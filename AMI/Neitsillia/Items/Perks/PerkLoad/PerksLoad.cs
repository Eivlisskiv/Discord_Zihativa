using AMI.Neitsillia.Collections;
using AMI.Neitsillia.NPCSystems;
using AMYPrototype;
using System;

namespace AMI.Neitsillia.Items.Perks.PerkLoad
{
    static partial class PerkLoad
    {
        private static readonly AMIData.ReflectionCache reflectionCache = new AMIData.ReflectionCache(typeof(PerkLoad));

        #region Checks
        internal static object[] CheckPerksM(CharacterMotherClass player,
            Perk.Trigger trigger, params object[] arguments)
        {
            if (player.perks != null && player.perks.Count > 0)
            {
                for (int i = 0; i < player.perks.Count; i++)
                {
                    Perk p = player.perks[i];
                    try
                    {
                        if (p.trigger == trigger)
                            arguments = p.Run(player, false, arguments);
                        if (p.end != Perk.Trigger.Null && p.end == trigger)
                            arguments = p.Run(player, true, arguments);
                    }
                    catch (Exception e)
                    {
                        _ = Handlers.UniqueChannels.Instance.SendToLog(e, $"Error in perk {p.name}");
                    }
                }
            }  
            for (int i = 0; i < Equipment.gearCount; i++)
            {
                var gear = player.equipment.GetGear(i);
                if (gear?.perk != null)
                {
                    try
                    {
                        if (gear.perk.trigger == trigger)
                            arguments = gear.perk.Run(player, false, arguments);
                        if (gear.perk.end != Perk.Trigger.Null && gear.perk.end == trigger)
                            arguments = gear.perk.Run(player, true, arguments);
                    }
                    catch (Exception e) { _ = Handlers.UniqueChannels.Instance.SendToLog(e, $"Error in equipment perk {gear?.perk?.name}"); }
                }
            }
            if (player.status != null)
            {
                for (int i = 0; i < player.status.Count;)
                {
                    var p = player.status[i];
                    try
                    {
                        if (p.trigger == trigger)
                            arguments = p.Run(player, false, arguments);
                        if (p.end != Perk.Trigger.Null && p.end == trigger)
                            arguments = p.Run(player, true, arguments);
                    }
                    catch (Exception e) { _ = Handlers.UniqueChannels.Instance.SendToLog(e, $"Error in status {p.name}"); }
                    //
                    if (p.rank <= 0) player.status.RemoveAt(i);
                    else i++;
                }
            }
            return arguments;
        }//*/
        internal static T CheckPerks<T>(CharacterMotherClass player,
            Perk.Trigger trigger, T arguments)
        {
            return (T)CheckPerksM(player, trigger, arguments)[0];
        }
        internal static object[] CheckPerks(CharacterMotherClass player,
            Perk.Trigger trigger, params object[] arguments)
        {
            return CheckPerksM(player,  trigger, arguments);
        }
        //
        #endregion
        #region Loads
        internal static Perk Load(string name)
        {
            try
            {
                return reflectionCache.Run<Perk>(name.Replace(" ", ""), name);

            }catch(Exception e)
            {
                Console.WriteLine("Error with perk " + name);
                throw e;
            }
        }
        internal static Perk Effect(string name, int duration, int intensity)
        {
            return reflectionCache.Run<Perk>(name.Replace(" ", ""), name, intensity, duration);
        }
        internal static Perk RandomPerk(int rank, string type)
        {
            string[][] availablePerks = new string[0][];
            switch (type)
            {
                case "Character":
                    availablePerks = Program.data.AvailablePerks.CharacterPerks;
                    break;
                case "Armor":
                    availablePerks = Program.data.AvailablePerks.ArmorPerks;
                    break;
                case "Weapon":
                    availablePerks = Program.data.AvailablePerks.WeaponPerks;
                    break;
            }
            if (rank <= -1) rank = Program.rng.Next(availablePerks.Length);
            else rank = Math.Min(rank, availablePerks.Length - 1);

            return Load(availablePerks[rank][Program.rng.Next(availablePerks[rank].Length)]);
        }
        #endregion
        /*
        public static Perk Template(string name) => new Perk(name)
        {
                trigger = Perk.Trigger,
                rank = 1,
                maxRank = 1,
                desc = "",
        };
        //*/
        #region Race Perks
        public static Perk UskavianLearning(string name) => new Perk(name)
        {
            trigger = Perk.Trigger.BeforeOffense,
            end = Perk.Trigger.EndFight,
            rank = 0, //data store's the ability's name last used, +1 rank if name ==, else rank = 0
            maxRank = 10,
            desc = "Using the same attack stacks +10% total damage up to 100%.",
        };
        public static Perk IreskianTalent(string name) => new Perk(name)
        {
            trigger = Perk.Trigger.Crafting,
            end = Perk.Trigger.Upgrading,
            desc = "Grants a bonus to gear's stats when crafted or upgraded.",
        };
        public static Perk MiganaSkin(string name) 
            => new Perk(name)
        {
            trigger = Perk.Trigger.BeforeDefense,
            desc = "When attacked, the Miganan's skin has a chance to start strengthening. " +
                "Once full, it grants physical damage resistance as it degrades.",
        };
        public static Perk TsiunTrickery(string name) => new Perk(name)
        {
            trigger = Perk.Trigger.StartFight,
            end = Perk.Trigger.EndFight,
            rank = 0,
            maxRank = 1,
            desc = "Gain +2 of the enemy's highest main stats during combat.",
        };
        public static Perk HumanAdaptation(string name) => new Perk(name)
        {
            trigger = Perk.Trigger.Defense,
            rank = 0,
            maxRank = 5,
            desc = "Gains +1 random base stats, switches after each 5 hits taken.",
        };
        #endregion

        #region non Combat Perks

        #endregion

        #region Creature
        //Tier 0
        public static Perk PrickledSkin(string name) => new Perk(name)
        {
            trigger = Perk.Trigger.BeforeDefense,
            desc = "Return 5% of physical damage received for every endurance point. [Max 50%]",
        };
        public static Perk HeadSick(string name) => new Perk(name)
        {
            trigger = Perk.Trigger.Health,
            rank = 0,
            maxRank = 2,
            desc = "+1% total damage for each % or full health lost.",
        };
        public static Perk CarelessWhisper(string name) => new Perk(name)
        {
            trigger = Perk.Trigger.BeforeDefense,
            desc = "Each charisma points above the target's charisma points grants 0.5% chance to make them miss their attack. [Max 20%]",
        };
        //Tier 1
        public static Perk SharpClaws(string name) => new Perk(name)
        {
            trigger = Perk.Trigger.Offense,
            desc = "Attacks have a 15% chance to cause -1% PHY RES per STR point [Max 50%] for 8 turns to target.",
        };
        public static Perk RascallyAttacks(string name) => new Perk(name)
        {
            trigger = Perk.Trigger.Offense,
            desc = "Attacks have a 10% chance to exhaust the target out of 50 stamina points.",
        };
        public static Perk Evassive(string name) => new Perk(name)
        {
            trigger = Perk.Trigger.Defense,
            desc = "+25% Martial and Enchanted attacks dodge chance",
        };
        public static Perk SixthSense(string name) => new Perk(name)
        {
            trigger = Perk.Trigger.Defense,
            desc = "+20% Elemental and Tactical attacks dodge chance",
        };
        #endregion

        #region Weapons
        public static Perk TrialAndError(string name) => new Perk(name)
        {
            trigger = Perk.Trigger.Offense,
            end = Perk.Trigger.EndFight,
            rank = 0,
            maxRank = 1,
            desc = "Missing an attack grants +8% damage on the next attack, missing the next attack cancel the buff. " +
                "Getting a non critical hit grants +3% critical damage on the next attack, not getting a critical hit on the next attack cancels the buff.",
        };
        public static Perk Blazed(string name) => new Perk(name)
        {
            trigger = Perk.Trigger.BeforeOffense,
            desc = "Adds 5% of your total non Blaze damage to Blaze damage",
        };
        public static Perk Icy(string name) => new Perk(name)
        {
            trigger = Perk.Trigger.BeforeOffense,
            desc = "Adds 5% of your total non Cold damage to Cold damage",
        };
        public static Perk Toxic(string name) => new Perk(name)
        {
            trigger = Perk.Trigger.BeforeOffense,
            desc = "Adds 5% of your total non Toxic damage to Toxic damage",
        };
        public static Perk Wired(string name) => new Perk(name)
        {
            trigger = Perk.Trigger.BeforeOffense,
            desc = "Adds 5% of your total non Electric damage to Electric damage",
        };
        //T1
        public static Perk Curare(string name) => new Perk(name)
        {
            trigger = Perk.Trigger.Offense,
            desc = $"Has a 45% chance to apply paralysis to the target.",
        };
        public static Perk Vengeful(string name) => new Perk(name)
        {
            trigger = Perk.Trigger.Turn,
            desc = "While health is bellow 35%, gain +1% damage per 1% of max health under 35%."
        };
        #endregion

        #region Status
        //Basic Elements
        public static Perk Bleeding(string name, int tier, int rank) 
        => new Perk(name, tier, rank)
        {
            trigger = Perk.Trigger.Turn,
            type = Perk.StatusType.Negative,
            desc = $"Takes {2 * tier} physical damage for {rank} turns."
        };
        public static Perk Burning(string name, int tier, int rank)
        => new Perk(name, tier, rank)
        {
                trigger = Perk.Trigger.Turn,
            type = Perk.StatusType.Negative,
            desc = $"Takes {2 * tier} blaze damage for {rank} turns."
        };
        public static Perk Poisoned(string name, int tier, int rank)
        => new Perk(name, tier, rank)
        {
                trigger = Perk.Trigger.Turn,
            type = Perk.StatusType.Negative,
            desc = $"Takes {2 * tier} toxic damage for {rank} turns."
        };

        //Base Stats
        #region Resistances
        //Positive
        public static Perk Hardened(string name, int tier, int rank) => new Perk(name, tier, rank)
        {
            trigger = Perk.Trigger.Turn,
            type = Perk.StatusType.Positive,
            desc = $"Increases physical resistance by {20 * tier}."
        };
        //Negatives
        public static Perk Punctured(string name, int tier, int rank) => new Perk(name, tier, rank)
        {
            trigger = Perk.Trigger.Turn,
            type = Perk.StatusType.Negative,
            desc = $"Reduces physical resistance by {10 * tier} for {rank} Turns."
        };
        public static Perk MalformedArmor(string name, int tier, int rank) => new Perk(name, tier, rank)
        {
            trigger = Perk.Trigger.Turn,
            type = Perk.StatusType.Negative,
            desc = $"Reduces physical resistance by {tier}% for {rank} Turns."
        };
        public static Perk Decay(string name, int tier, int rank) => new Perk(name, tier, rank)
        {
            trigger = Perk.Trigger.Turn,
            desc = $"loses {tier} physical resistance per turn for {rank} turns.",
        };
        #endregion

        public static Perk ElementalResillience(string name, int tier, int rank) => new Perk(name, tier, rank)
        {
            trigger = Perk.Trigger.Turn,
            type = Perk.StatusType.Positive,
            desc = $"+{tier} Elemental Resistances.",
        };
        public static Perk Suppressed(string name, int tier, int rank) => new Perk(name, tier, rank)
        {
            trigger = Perk.Trigger.Offense,
            end = Perk.Trigger.Turn,
            type = Perk.StatusType.Negative,
            desc = $"Reduces damage by {tier}% for {rank} turns. ",
        };
        public static Perk Merciless(string name, int tier, int rank)
        => new Perk(name, tier, rank)
        {
            trigger = Perk.Trigger.Offense,
            end = Perk.Trigger.Turn,
            type = Perk.StatusType.Positive,
            desc = $"Increases physical damage by {tier}% for {rank} turns",
        };
        public static Perk Focused(string name, int tier, int rank)
        => new Perk(name, tier, rank)
        {
            trigger = Perk.Trigger.BeforeOffense,
            end = Perk.Trigger.Turn,
            type = Perk.StatusType.Positive,
            desc = $"Increases critical chance by {tier * 1.5}% for {rank} turns",
        };
        public static Perk CriticalWindow(string name, int tier, int rank) => new Perk(name, tier, rank)
        {
            trigger = Perk.Trigger.Offense,
            end = Perk.Trigger.Turn,
            type = Perk.StatusType.Positive,
            desc = $"Increases critical damage by {tier * 5}% for {rank} turns",
        };

        public static Perk Vigorous(string name, int tier, int rank) => new Perk(name, tier, rank)
        {
            trigger = Perk.Trigger.MaxHealth,
            type = Perk.StatusType.Positive,
            desc = $"Increases max health by {tier * 10} for {rank} turns",
        };
        #region Charging
        //Positive
        public static Perk PatientRequite(string name, int tier, int rank) => new Perk(name, tier, rank)
        {
            trigger = Perk.Trigger.Turn,
            end = Perk.Trigger.Defense,
            type = Perk.StatusType.Positive,
            desc = $"Charges {tier * 2} physical damage for each turn you are not attacked, damage is released" +
                $" on attacker once a hit is taken."
        };
        public static Perk KeenEvaluation(string name, int tier, int rank) => new Perk(name, tier, rank)
        {
            trigger = Perk.Trigger.Turn,
            type = Perk.StatusType.Positive,
            desc = $"While not using Martial, Elemental or Enchantment abilities, +{tier} Critical Chance and +{tier * 0.02}x Critical Damage." +
            $"Effect wears off after casting an offense ability.",
        };
        //negatives
        public static Perk Rigged(string name, int tier, int rank) => new Perk(name, tier, rank)
        {
            trigger = Perk.Trigger.Turn,
            type = Perk.StatusType.Negative,
            desc = $"For each turn passed damage received is increased by {tier}% damage. " +
            $"Effect wears off after {rank} turns or after receiving a hit.",
        };
        #endregion
        //Proc
        public static Perk Discourage(string name, int tier, int rank) => new Perk(name, tier, rank)
        {
            trigger = Perk.Trigger.Defense,
            type = Perk.StatusType.Positive,
            desc = $"Has a {tier * 5}% chance to reduce attacker damage by {tier * 2}% for {4 + (tier / 10)} turns.",
        };
        public static Perk HospitableAura(string name, int tier, int rank) => new Perk(name, tier, rank)
        {
            trigger = Perk.Trigger.Defense,
            end = Perk.Trigger.Turn,
            type = Perk.StatusType.Positive,
            desc = $"Receiving defensive casts has a {60 + tier}% chance for +{10 + tier} elemental RES to self and caster for {2 + (tier / 8)} turns.",
        };
        public static Perk EnergyLeak(string name, int tier, int rank)
        => new Perk(name, tier, rank)
        {
            trigger = Perk.Trigger.Defense,
            end = Perk.Trigger.Turn,
            type = Perk.StatusType.Negative,
            desc = $"When hit, attacker has {35 + tier}% chance to drain {tier*5} stamina",
        };
        public static Perk Warded(string name, int tier, int rank) => new Perk(name, tier, rank)
        {
            trigger = Perk.Trigger.Defense,
            type = Perk.StatusType.Positive,
            desc = $"Has a {tier} chance to nullify any attack."
        };
        //Healing
        public static Perk Recovering(string name, int tier, int rank)
        => new Perk(name, tier, rank)
        {
            trigger = Perk.Trigger.Turn,
            type = Perk.StatusType.Positive,
            desc = $"Restore {tier * 5}HP per turn",
        };
        public static Perk FullBreaths(string name, int tier, int rank)
            => new Perk(name, tier, rank)
           {
                trigger = Perk.Trigger.Turn,
                type = Perk.StatusType.Positive,
                desc = $"Restore {tier * 5}% SP per turn",
            };
        //Disease 
        public static Perk FleshCurse(string name, int tier, int rank)
        => new Perk(name, tier, rank)
        {
            trigger = Perk.Trigger.Turn,
            type = Perk.StatusType.Negative,
            desc = "This recently growing disease eats and twists the flesh of its host. " +
            "Receive toxic damage each turn. "
            //+ "This curse has a chance to mutate."
        };

        #endregion

        #region Blessings
        public static Perk BlessingOfAvlimia(string name, int tier, int rank) => new Perk(name, tier, rank)
        {
            trigger = Perk.Trigger.Offense,
            type = Perk.StatusType.Blessing,
            desc = $"Attacks have {30 + (tier / 5)}% chance to reduce target's physical resistance by {10}."
        };
        public static Perk BlessingOfBakora(string name, int tier, int rank) => new Perk(name, tier, rank)
        {
            trigger = Perk.Trigger.Offense,
            type = Perk.StatusType.Blessing,
            desc = $"Defensive abilities have a {30 + (tier/5)}% chance to ward the target for 1 attack."
        };
        public static Perk BlessingOfKoocioli(string name, int tier, int rank) => new Perk(name, tier, rank)
        {
            trigger = Perk.Trigger.Turn,
            end = Perk.Trigger.Defense,
            type = Perk.StatusType.Blessing,
            desc = $"Hits taken have a {20 + (tier/4)}"
        };
        #endregion

        #region Specs
        //Blacksmith
        public static Perk BuiltToLast(string name) => new Perk(name)
        {
            trigger = Perk.Trigger.Crafting,
            desc = "Upon crafting, increases base durability by 24%. Each INT and END points increases by 1%.",
        };
        public static Perk ReinforcedMaterials(string name) => new Perk(name)
        {
            trigger = Perk.Trigger.Crafting,
            desc = "Upon crafting, Item receives 1 bonus condition per INT and END points. Amount multiplied for each 5 character levels reached.",
        };
        public static Perk FinishingTouch(string name) => new Perk(name)
        {
            trigger = Perk.Trigger.Upgrading,
            desc = "Upon upgrading, Item has a small chance to increase it's maximum rank.",
        };
        //Healer
        public static Perk Adrenaline(string name) => new Perk(name)
        {
            trigger = Perk.Trigger.Healing,
            desc = "15% chance to restore stamina when receiving healing.",
        };
        public static Perk EnergizingTouch(string name) => new Perk(name)
        {
            trigger = Perk.Trigger.Offense,
            rank = 0,
            maxRank = 0,
            desc = "Casting a defensive ability has 20% chance to regenerate " +
            "target's stamina by 5% per turn for 8 turns.",
        };
        public static Perk Adaptation(string name) => new Perk(name)
        {
            trigger = Perk.Trigger.Defense,
            end = Perk.Trigger.BeforeDefense,
            rank = 0,
            maxRank = 8,
            desc = "Receiving a hit has a 40% chance to increase damage resistance by 50. " +
            "Effect ends after 8 stacks with a 30% chances to restore health.",
        };
        //Fighter
        public static Perk UnstoppableForce(string name) => new Perk(name)
        {
            trigger = Perk.Trigger.Offense,
            desc = "Dealing damage while health is below 30% has a chance to restore stamina (Increases with dexterity).",
        };
        public static Perk PrecisionEnhancement(string name) => new Perk(name)
        {
            trigger = Perk.Trigger.Offense,
            desc = "Critical hits have a chance to increase critical damage for 3 turns.",
        };
        public static Perk FightingSpirit(string name) => new Perk(name)
        {
            //FightingSpirit
            trigger = Perk.Trigger.Offense,
            end = Perk.Trigger.Turn,
            desc = "Missing an attack grants +100% Critical Chance for 1 turn",
        };
        #endregion

        #region Uniques
        public static Perk BambooSnack(string name) => new Perk(name)
        {
            trigger = Perk.Trigger.Offense,
            type = Perk.StatusType.Positive,
            desc = "Casting a defensive ability has a 20%(10% for self) chance to increase the target's maximum health by 10% of your max health for 8 turns.",
        };
        #endregion
    }
}
