using MongoDB.Bson.Serialization.Attributes;

namespace AMI.Neitsillia.Collections
{
    [BsonIgnoreExtraElements]
    public class Stats
    {
        public const int hpPerEnd = 3;
        public const double DurabilityPerEnd = 0.01;//% decreases durability loss
        //
        public const double aEfficiencyPerInt = 0.03; //(Ability +x% DMG, +x% Healing, etc)
        public const double SchemDropRatePerInt = 0.05; //+% chance to get a schematic
        //
        public const double dmgPerStr = 0.04; //+ x% Physical DMG
        public const int invSizePerStr = 3; //+ inventory size
        //
        public const double pricePerCha = 0.04; //% modification to price in their favor
        public const double recruitPricePerCha = 0.01; //% modification to price in their favor
        public const double koinsGainPerCha = 0.20; //% coins gained from quests/bounties/jobs
        //
        public const double restSpeed = 0.001; //Of max points restored per x seconds
        public const double passiveSp = 0.05; // * DEX = % of passive sp in combat
        //
        public const double critcPerPerc = 1.20; //Raw addition (cc + x%)
        public const double LootPerPerc = 0.25; //max items looted

        public int endurance = 0; //0
        public int intelligence = 0; //1
        public int strength = 0; //2 
        public int charisma = 0; //3
        public int dexterity = 0; //4
        public int perception = 0; //5

        public int[] baseBuffs = new int[6];

        public long maxhealth = 5;
        public int stamina = 0;
        public int agility = 50;
        public double critChance = 10;
        public double critMult = 30;

        public long[] damage = new long[ReferenceData.DmgType.Length];
        public int[] resistance = new int[ReferenceData.DmgType.Length];

        internal int GetStat(int i)
        {
            switch(i)
            {
                case 0: return endurance;
                case 1: return intelligence;
                case 2: return strength;
                case 3: return charisma;
                case 4: return dexterity;
                case 5: return perception;
            }
            return 0;
        }
        public int GetEND() => (endurance + baseBuffs[0]);
        public long MaxHealth() => maxhealth + (GetEND() * hpPerEnd);
        public int GetSTR() => (strength + baseBuffs[2]);
        public double PhysicalDMG() => 1 + (GetSTR() * dmgPerStr);
        public int GetDEX() => (dexterity + baseBuffs[4]);
        public int GetPER() => (perception + baseBuffs[5]);
        public double CritChance() => critChance + (GetPER() * critcPerPerc);
        public double CritMult() => critMult;
        public int GetCHA() => (charisma + baseBuffs[3]);
        public double PriceMod() => (GetCHA() * pricePerCha);
        public int GetINT() => (intelligence + baseBuffs[1]);
        public double Efficiency() => 1 + (GetINT() * aEfficiencyPerInt);
        //
    }
}
