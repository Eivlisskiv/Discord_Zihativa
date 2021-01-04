using System;
using System.Threading.Tasks;
using AMI.Methods;
using AMI.Neitsillia.Collections;
using AMI.Neitsillia.User;
using AMI.Neitsillia.User.PlayerPartials;
using AMI.Neitsillia.User.UserInterface;
using AMYPrototype;
using AMYPrototype.Commands;
using Discord;
using Neitsillia.Items.Item;

namespace AMI.Neitsillia.NPCSystems.Companions.Pets
{
    class Pet
    {
        internal enum PetStatus
        {
            Idle, InParty,
            Resting, Fetching
        }



        public NPC pet;
        public string id;
        public PetStatus status;

        public int trust;
        public int points;

        public DateTime statusTime;

        public Pet(NPC pet, PetStatus status)
        {
            this.pet = pet;
            this.status = status;
            id = $"{pet.origin}\\{Guid.NewGuid()}";
            pet.origin = id;
            trust = 25;
        }

        internal void VerifyPet()
        {
            switch(status)
            {
                case PetStatus.Resting:
                    {
                        if(statusTime <= DateTime.UtcNow)
                        {
                            pet.health = pet.Health();
                            pet.stamina = pet.Stamina();
                            status = PetStatus.Idle;
                        }
                    }break;
            }
        }

        internal async Task GetInfo(Player player, IMessageChannel channel, int i, bool isEdit = true)
        {
            var (title, desc) = GetField();
            EmbedBuilder em = DUtils.BuildEmbed(title, desc, color: player.userSettings.Color());
            await player.EnUI(isEdit, null, em.Build(), channel, MsgType.InspectPet, i.ToString());
        }

        internal (string title, string desc) GetField(int i = -1)
        {
            VerifyPet();
            string desc = $"Status: [{status}]";
            switch (status)
            {
                case Pet.PetStatus.Idle:
                    {
                        pet.HealthStatus(out string hp);
                        desc =  $"Status: [{status}]"
                            + Environment.NewLine + $"Level {pet.level}, Rank {pet.Rank()}"
                            + Environment.NewLine + $"{hp}, {pet.StaminaStatus()}"
                            + Environment.NewLine + $"Class: {pet.role}"
                            + Environment.NewLine + $"Type: {pet.name}";
                    }break;
                case PetStatus.Resting:
                        desc = $"Resting time left: {Timers.CoolDownToString(statusTime)}";
                    break;
                case PetStatus.Fetching:
                        desc = $"Has been exploring for {Timers.CoolDownToString(DateTime.UtcNow - statusTime)}";
                    break;
            }
            return ($"{EUI.GetNum(i)} **{pet.displayName}**", desc);
        }

        internal void UpdatePet(NPC npc)
        {
            points += Math.Max(npc.level - pet.level, 0);

            pet = npc;
            pet.faction = Reputation.Faction.Factions.Pet;

            long hp = pet.Health();
            if (pet.health < hp)
            {
                if (pet.health < 1)
                    trust -= 1;

                status = Pet.PetStatus.Resting;
                statusTime = DateTime.UtcNow.AddMinutes((hp - pet.health) / pet.stats.GetDEX());
            }
            else
                status = Pet.PetStatus.Idle;
        }

        #region Upgrades

        internal async Task UpgradeOptionsUI(Player player, IMessageChannel chan, int i, bool isEdit = true)
        {
            string desc = null;
            string[] ups = Enum.GetNames(typeof(PetUpgrades.Upgrade));
            for (int k = 0; k < ups.Length; k++)
            {
                desc += $"{EUI.GetNum(k)} {ups[k]} : {(PetUpgrades.Costs.ContainsKey(pet.race) ? PetUpgrades.Costs[pet.race] : PetUpgrades.Costs["Default"])[k]} pts {Environment.NewLine}";
            }
            EmbedBuilder embed = DUtils.BuildEmbed($"Training {pet.displayName}",
                desc, $"Points: {points}", player.userSettings.Color());

            await player.EnUI(isEdit, null, embed.Build(), chan, MsgType.PetUpgrade, i.ToString());
        }

        internal async Task UpgradeStatUI(Player player, IMessageChannel chan, int i, int k)
        {

            string desc = $"Cost per increase: {(PetUpgrades.Costs.ContainsKey(pet.race) ? PetUpgrades.Costs[pet.race] : PetUpgrades.Costs["Default"])[k]}pts {Environment.NewLine}"
                + $"{(PetUpgrades.Upgrade)k}: ";

            switch ((PetUpgrades.Upgrade)k)
            {
                case PetUpgrades.Upgrade.Health: desc += pet.stats.maxhealth; break;
                case PetUpgrades.Upgrade.Stamina: desc += pet.stats.stamina; break;

                case PetUpgrades.Upgrade.Damage:
                    {
                        desc += Environment.NewLine;
                        string[] n = Enum.GetNames(typeof(ReferenceData.DamageType));
                        for (int j = 0; j < n.Length; j++)
                        {
                            desc += $"{EUI.GetElement((ReferenceData.DamageType)j)} {EUI.attack}: {pet.stats.damage[j]} {Environment.NewLine}";
                        }

                    } break;
                case PetUpgrades.Upgrade.Resistance:
                    {
                        desc += Environment.NewLine;
                        string[] n = Enum.GetNames(typeof(ReferenceData.DamageType));
                        for (int j = 0; j < n.Length; j++)
                        {
                            desc += $"{EUI.GetElement((ReferenceData.DamageType)j)} {EUI.shield}: {pet.stats.resistance[j]} {Environment.NewLine}";
                        }
                    }
                    break; 
            }

            EmbedBuilder embed = DUtils.BuildEmbed($"Training {pet.displayName}'s {(PetUpgrades.Upgrade)k}",
                desc, $"Points: {points}", player.userSettings.Color());

            await player.EditUI(null, embed.Build(), chan, MsgType.PetUpgrade, $"{i};{k}");
        }

        internal bool UpgradePet(PetUpgrades.Upgrade up, int i = 0)
        {
            int cost = (
                PetUpgrades.Costs.ContainsKey(pet.race) ?
                PetUpgrades.Costs[pet.race] : PetUpgrades.Costs["Default"]
            )[(int)up];

            if (cost > points) return false;

            switch (up)
            {
                case PetUpgrades.Upgrade.Health: pet.stats.maxhealth++; break;
                case PetUpgrades.Upgrade.Stamina: pet.stats.stamina++; break;

                case PetUpgrades.Upgrade.Damage: pet.stats.damage[i]++; break;
                case PetUpgrades.Upgrade.Resistance: pet.stats.resistance[i]++; break;
            }
            points -= cost;
            return true;
        }

        #endregion

        #region Evolve

        internal async Task ViewEvolves(Player player, IMessageChannel channel, int i, bool isEdit = true)
        {
            (int level, string name)[] options = Evolves.GetOptions(pet.race, pet.name);
            if (options == null)
            {
                await channel.SendMessageAsync($"{pet.displayName} has no evolve options");
                return;
            }

            string desc = null;
            for(int k = 0; k < options.Length; k++)
            {
                bool hasLevel = pet.level >= options[k].level;
                desc += $"{EUI.GetNum(k)} => {(hasLevel ? null : "~~")}{options[k].name} {(hasLevel ? null : $"~~ (Requires level {options[k].level})")}" + Environment.NewLine;
            }

            EmbedBuilder embed = DUtils.BuildEmbed($"Evolving {pet.displayName}", desc, null, player.userSettings.Color());

            await player.EnUI(isEdit, null, embed.Build(), channel, MsgType.PetEvolve, $"{i};{options.Length}");
        }

        #endregion

        internal async Task ToggleFetching(Player player, IMessageChannel chan, int i)
        {
            if(status == PetStatus.Fetching)
            {
                int pts = 0;
                TimeSpan time = DateTime.UtcNow - statusTime;
                double hours = time.TotalHours;
                int mult = Math.Min(pet.level / 5, 10);
                if (hours > 24) pts = Methods.NumbersM.FloorParse<int>(hours / 24) * mult;
                if (Program.Chance((hours - pts) / 0.24)) pts += mult;

                StackedItems si = null;
                int koins = Methods.NumbersM.FloorParse<int>(hours * mult);
                Random rng = Program.rng;

                if (hours >= 36)
                {
                    si = new StackedItems(Items.SkaviDrops.DropSchematic("Universal"), 1);
                }
                else if (hours >= 24)
                {
                    si = new StackedItems(Item.RandomItem(pet.level * 5,
                        Utils.RandomElement(5,6,7,8,9,10,11)), 1);
                    si.item.Scale(pet.level * 5);
                }
                else if (hours >= 18)
                {
                    si = new StackedItems(Item.RandomItem(rng.Next(1, pet.level * 5),
                        Utils.RandomElement(6, 7, 8, 9, 10, 11)), 1);
                    si.item.Scale(pet.level * 5);
                }
                else if (time.Hours > 1)
                {
                    si = new StackedItems(Item.RandomItem(rng.Next(1, pet.level * 5),
                        Utils.RandomElement(0,1,2)), time.Hours);
                }

                string msg = null;

                if (pts > 0)
                {
                    msg += $"+{pts} Training Points" + Environment.NewLine;
                    points += pts;
                }
                if (koins > 0)
                {
                    msg += $"+{koins} Kutsyei Coins" + Environment.NewLine;
                    player.KCoins += koins;
                }
                if (si != null)
                {
                    msg += si.ToString() + Environment.NewLine;
                    player.CollectItem(si);
                }

                await chan.SendMessageAsync((msg == null ? "Nothing was fetched." :
                    $"{pet.displayName} has brought back some loot "
                    + Environment.NewLine + msg));

                status = PetStatus.Idle;
                await GetInfo(player, chan, i);
            }
            else
            {
                status = PetStatus.Fetching;
                statusTime = DateTime.UtcNow;

                await chan.SendMessageAsync($"{pet.displayName} went out to explore.");

                await GetInfo(player, chan, i);
            }
        }
    }
}