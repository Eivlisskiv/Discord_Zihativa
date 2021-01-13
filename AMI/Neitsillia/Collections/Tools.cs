using AMI.Methods;
using AMI.Methods.Graphs;
using AMI.Module;
using AMI.Neitsillia.User;
using AMI.Neitsillia.User.PlayerPartials;
using Neitsillia.Items.Item;
using System;

namespace AMI.Neitsillia.Collections
{
    public class Tools
    {
        public string _id;

        const int MinutesCoolDown = 120;
        //Sickle
        public static readonly string[] sickleTiers = { "Wooden Sickle" }; //, "Bone Sickle" };
        public static readonly string[] sickleDefault = { "Healing Herb" };
        //
        public int sickleLevel = 0;
        public int sickleTier = 0;
        public long sickleXP = 0;
        //Axe
        public static readonly string[] axeTiers = { "HandMade Axe" }; //, "Scrap Axe" };
        public static readonly string[] axeDefault = { "Wood" };
        //
        public int axeLevel = 0;
        public int axeTier = 0;
        public long axeXP = 0;
        //Pick Axe
        public static readonly string[] pickaxeTiers = { "HandMade Pick Axe" }; //, "Scrap Pick Axe" };
        public static readonly string[] pickaxeDefault = { "Metal Scrap" };
        //
        public int pickaxeLevel = 0;
        public int pickaxeTier = 0;
        public long pickaxeXP = 0;
        //Spear
        public static readonly string[] spearTiers = { "HandMade Spear" }; //, "Scrap Pick Axe" };
        public static readonly string[] spearDefault = { "Leather" };
        //
        public int spearLevel = 0;
        public int spearTier = 0;
        public long spearXP = 0;


        public Tools(string id)
        {
            _id = id;
        }

        //Gets
        internal int GetTier(string tool) => Utils.GetVar<int, Tools>(this, $"{tool}Tier");
        internal int AddTier(string tool, int newTier) => Utils.SetVar(this, $"{tool}Tier", GetTier(tool) + newTier);

        internal int GetLevel(string tool) => Utils.GetVar<int, Tools>(this, $"{tool}Level");
        internal int AddLevel(string tool, int newTier) => Utils.SetVar(this, $"{tool}Level", GetLevel(tool) + newTier);
        internal int SetLevel(string tool, int newTier) => Utils.SetVar(this, $"{tool}Level", newTier);
        //
        internal long SetXP(string tool, long newxp) => Utils.SetVar(this, $"{tool}XP", newxp);
        internal long AddXP(string tool, long amount) => SetXP(tool, GetXP(tool) + amount);
        internal long GetXP(string tool) => Utils.GetVar<long, Tools>(this, $"{tool}XP");

        internal string[] GetAllTiers(string tool) => Utils.GetVar<string[], Tools>(this, $"{tool}Tiers");
        //
        public string ToolName(string tool, int tier = -1, int level = -1)
        {
            if(tier < 0)
                tier = GetTier(tool);
            if(level < 0)
                level = GetLevel(tool);

            return $"{GetAllTiers(tool)[tier]} {NumbersM.GetLevelMark(level)}";
        }

        internal string UseTool(string toolName, Player player, Areas.AreaType areaType)
        {
            DateTime cooldown = Utils.GetVar<DateTime, Timers>(player.userTimers, $"{toolName}Usage");
            int tier = GetTier(toolName);
            int level = GetLevel(toolName);
            string name = ToolName(toolName, tier, level);
            if (cooldown > DateTime.UtcNow)
                return $"{player.name}, your {name} is on cooldown for {Timers.CoolDownToString(cooldown)}";
            else
            {
                string[] options = null;
                if((options = Utils.GetVar<string[], Tools>(this, $"{toolName}{areaType}", true)) == null)
                    options = Utils.GetVar<string[], Tools>(this, $"{toolName}Default");

                level += tier - Verify.Max(tier, options.Length - 1);
                StackedItems st = new StackedItems(Item.LoadItem(options[Verify.Max(tier, options.Length - 1)]), level + 1);

                if (player.CollectItem(st))
                {
                    Utils.SetVar(player.userTimers, $"{toolName}Usage", DateTime.UtcNow.AddMinutes(MinutesCoolDown));
                    long currentXp = Utils.GetVar<long, Tools>(this, $"{toolName}XP");
                    long newXP = Utils.SetVar(this, $"{toolName}XP", currentXp + player.level);
                    player.SaveFileMongo();
                    return $"{player.name} collected {st} using {name}.";
                }
                return $"{player.name}'s Inventory may not currently contain {st}.";
            }
        }

        internal string ToolInfo(string toolName)
        {
            int tier = GetTier(toolName);
            int level = GetLevel(toolName);
            long xp = GetXP(toolName);
            string name = ToolName(toolName, tier, level);
            return name + Environment.NewLine
                + $"Tier: " + tier + Environment.NewLine
                + $"Level: " + level + Environment.NewLine
                + $"XP: {xp}/{XPRequired(level, tier)}" + Environment.NewLine
                + $"``{UpgradeCost(toolName, level, tier)}``";
        }

        internal long XPRequired(int level, int tier)
        {
            if (level >= 10)
                tier++;
            else level++;
            return Quadratic.F_longQuad(level * (tier + 1), 13, 0, 0);
        }

        internal string UpgradeCost(string toolName, int level, int tier)
        {
            if(level >= 10 && tier >= GetAllTiers(toolName).Length - 1)
                return "No upgrades available.";
            return "Upgrade Cost:" + Environment.NewLine +
                Utils.RunMethod<string>(StringM.UpperAt(toolName) + "UpgradeInfo", this, level, tier);
        }
        internal string Upgrade(string toolName, Player player)
        {
            int tier = GetTier(toolName);
            int level = GetLevel(toolName);
            long xp = GetXP(toolName);
            if (xp < XPRequired(level, tier))
                return "Tool is not ready to be upgraded.";
            Inventory cost = Utils.RunMethod<Inventory>(StringM.UpperAt(toolName) + "Upgrade", this, level, tier);
            string result = null;
            foreach (StackedItems si in cost.inv)
            {
                int i = -1;
                if ((i = player.inventory.FindIndex(si.item)) < 0)
                    result += si.ToString() + Environment.NewLine;
                else if (player.inventory.GetCount(i) < si.count)
                    result += $"{si.count - player.inventory.GetCount(i)} {si.item.name}" + Environment.NewLine;
                else
                    player.inventory.Remove(i, si.count);
            }
            if (result != null)
                return "Missing Materials:" + Environment.NewLine + result;
            else
            {
                result = ToolName(toolName, tier, level);
                AddXP(toolName, -XPRequired(level, tier));
                if (level >= 10)
                {
                    SetLevel(toolName, 0);
                    AddTier(toolName, 1);
                    tier++; level = 0;
                }
                else { AddLevel(toolName, 1); level++; }
                player.SaveFileMongo();
                return $"Upgraded {result} to {ToolName(toolName, tier, level)}";
            }
            throw NeitsilliaError.ReplyError("Feature unavailable");
        }

        //Upgrades
        public string SickleUpgradeInfo(int level, int tier)
        {
            if (level >= 10)
                tier++;
            else
                level++;
            switch (tier)
            {
                case 0:
                        return ""  
                        + $"{10*level} Wood" + Environment.NewLine
                        + $"{5*level} String" + Environment.NewLine
                        ;
                case 1:
                        return ""
                        + $"{1*level} Tsuu Bone" + Environment.NewLine
                        + $"{20} Wood" + Environment.NewLine
                        + $"{10} String" + Environment.NewLine
                        ;
            }
            return "No upgrades available.";
        }
        public Inventory SickleUpgrade(int level, int tier)
        {
            if (level >= 10)
                tier++;
            else
                level++;
            switch (tier)
            {
                case 0:
                    return new Inventory(
                        new StackedItems("Wood", level * 10),
                        new StackedItems("String", level * 5)
                        );
                case 1:
                    return new Inventory(
                        new StackedItems("Wood", level * 20),
                        new StackedItems("Tsuu Bone", level),
                        new StackedItems("String", level * 10)
                        );
            }
            return null;
        }
        //
        public string AxeUpgradeInfo(int level, int tier)
        {
            if (level >= 10)
                tier++;
            else
                level++;
            switch (tier)
            {
                case 0:
                    return ""
                    + $"{10 * level} Wood" + Environment.NewLine
                    + $"{5 * level} String" + Environment.NewLine
                    ;
                case 1:
                    return ""
                    + $"{1 * level} Tsuu Bone" + Environment.NewLine
                    + $"{10} Wood" + Environment.NewLine
                    + $"{10} String" + Environment.NewLine
                    ;
            }
            return "No upgrades available.";
        }
        public Inventory AxeUpgrade(int level, int tier)
        {
            if (level >= 10)
                tier++;
            else
                level++;
            switch (tier)
            {
                case 0:
                    return new Inventory(
                        new StackedItems("Wood", level * 10),
                        new StackedItems("String", level * 5)
                        );
                case 1:
                    return new Inventory(
                        new StackedItems("Wood", level * 20),
                        new StackedItems("Tsuu Bone", level),
                        new StackedItems("String", level * 10)
                        );
            }
            return null;
        }
        //
        public string PickaxeUpgradeInfo(int level, int tier)
        {
            if (level >= 10)
                tier++;
            else
                level++;
            switch (tier)
            {
                case 0:
                    return ""
                    + $"{10 * level} Wood" + Environment.NewLine
                    + $"{5 * level} String" + Environment.NewLine
                    ;
                case 1:
                    return ""
                    + $"{1 * level} Tsuu Bone" + Environment.NewLine
                    + $"{10} Wood" + Environment.NewLine
                    + $"{10} String" + Environment.NewLine
                    ;
            }
            return "No upgrades available.";
        }
        public Inventory PickaxeUpgrade(int level, int tier)
        {
            if (level >= 10)
                tier++;
            else
                level++;
            switch (tier)
            {
                case 0:
                    return new Inventory(
                        new StackedItems("Wood", level * 10),
                        new StackedItems("String", level * 5)
                        );
                case 1:
                    return new Inventory(
                        new StackedItems("Wood", level * 20),
                        new StackedItems("Tsuu Bone", level),
                        new StackedItems("String", level * 10)
                        );
            }
            return null;
        }
        //
        public string SpearUpgradeInfo(int level, int tier)
        {
            if (level >= 10)
                tier++;
            else
                level++;
            switch (tier)
            {
                case 0:
                    return ""
                    + $"{10 * level} Wood" + Environment.NewLine
                    + $"{5 * level} String" + Environment.NewLine
                    ;
                case 1:
                    return ""
                    + $"{1 * level} Tsuu Bone" + Environment.NewLine
                    + $"{10} Wood" + Environment.NewLine
                    + $"{10} String" + Environment.NewLine
                    ;
            }
            return "No upgrades available.";
        }
        public Inventory SpearUpgrade(int level, int tier)
        {
            if (level >= 10)
                tier++;
            else
                level++;
            switch (tier)
            {
                case 0:
                    return new Inventory(
                        new StackedItems("Wood", level * 10),
                        new StackedItems("String", level * 5)
                        );
                case 1:
                    return new Inventory(
                        new StackedItems("Wood", level * 20),
                        new StackedItems("Tsuu Bone", level),
                        new StackedItems("String", level * 10)
                        );
            }
            return null;
        }
    }
}
