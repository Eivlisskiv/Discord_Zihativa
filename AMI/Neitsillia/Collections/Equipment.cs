using Neitsillia.Items.Item;
using System;

namespace AMI.Neitsillia.Collections
{
    public class Equipment
    {
        internal const int gearCount = 9;
        //
        public Item weapon;//0
        public Item secondaryWeapon;//1
        public Item helmet;//2
        public Item mask;//3
        public Item chestp;//4
        public Item[] jewelry = new Item[3];//5-7
        public Item trousers;//8
        public Item boots;//9

        internal Item GetGear(int i)
        {
            if (jewelry == null) jewelry = new Item[3];
            switch (i)
            {
                case 0: return weapon;
                case 1: return secondaryWeapon;
                case 2: return helmet;
                case 3: return mask;
                case 4: return chestp;
                case 5:case 6:case 7:
                return jewelry[i - 5];
                case 8: return trousers;
                case 9: return boots;
            }
            return null;
        }
        internal Item GetGear(Item.IType t, int i = 0)
        {
            switch(t)
            {
                case Item.IType.Weapon: return weapon;
                case Item.IType.Helmet: return helmet;
                case Item.IType.Mask: return mask;
                case Item.IType.Chestp: return chestp;
                case Item.IType.Jewelry: return jewelry[i];
                case Item.IType.Trousers: return trousers;
                case Item.IType.Boots: return boots;
            }
            return null;
        }

        internal static object SlotName(int i)
        {
            switch (i)
            {
                case 0: return "Weapon";
                case 1: return "Secondary Weapon";
                case 2: return "Helmet";
                case 3: return "Mask";
                case 4: return "Chest Plate";
                case 5:
                case 6:
                case 7:
                    return $"Jewelry {i - 5}";
                case 8: return "Trousers";
                case 9: return "Boots";
            }
            return null;
        }

        internal static Item.IType GetItemType(int i)
        {
            switch (i)
            {
                case 0:
                case 1: return Item.IType.Weapon;
                case 2: return Item.IType.Helmet;
                case 3: return Item.IType.Mask;
                case 4: return Item.IType.Chestp;
                case 5: 
                case 6: 
                case 7: return Item.IType.Jewelry;
                case 8: return Item.IType.Trousers;
                case 9: return Item.IType.Boots;
            }
            throw Module.NeitsilliaError.ReplyError("Type is not within equipment types");
        }

        internal void SetGear(int i, Item item)
        {
            if (jewelry == null)
                jewelry = new Item[3];
            switch (i)
            {
                case 0: weapon = item; break;
                case 1: secondaryWeapon = item; break;
                case 2: helmet = item; break;
                case 3: mask = item; break;
                case 4: chestp = item; break;
                case 5:
                case 6:
                case 7: jewelry[i - 5] = item; break;
                case 8: trousers = item; break;
                case 9: boots = item; break;
            }
        }
        internal Item Equip(Item item, int i = 0)
        {
            //Put aside the currently equipped
            Item ret = GetGear(item.type, i);
            switch(item.type)
            {
                case Item.IType.Weapon: weapon = item; break;
                case Item.IType.Helmet: helmet = item; break;
                case Item.IType.Mask: mask = item; break;
                case Item.IType.Chestp: chestp = item; break;
                case Item.IType.Jewelry: jewelry[i] = item; break;
                case Item.IType.Trousers: trousers = item; break;
                case Item.IType.Boots: boots = item; break;
            }
            return ret;
        }
        //
        public long Damage(int i)
        {
            long dmg = 0;
            if (weapon != null && weapon.damage.Length > i)
                dmg += weapon.damage[i];
            if(secondaryWeapon != null && secondaryWeapon.damage.Length > i)
                dmg += secondaryWeapon.damage[i];
            if (jewelry != null)
                foreach (Item j in jewelry)
                if (j != null && j.damage.Length > i)
                    dmg += j.damage[i];
            return dmg;
        }
        public int Resistance(int r)
        {
            int value = 0;
            for (int i = 2; i <= gearCount; i++)
            {
                var gear = GetGear(i);
                if (gear != null && gear.resistance.Length > r)
                    value += gear.resistance[r];
            }
            return value;
        }
        public long Health()
        {
            long value = 0;
            for (int i = 2; i <= gearCount; i++)
            {
                var gear = GetGear(i);
                if(gear != null)
                value += gear.healthBuff;
            }
            return value;
        }
        public int Stamina()
        {
            int value = 0;
            for (int i = 2; i <= gearCount; i++)
            {
                var gear = GetGear(i);
                if (gear != null)
                    value += gear.staminaBuff;
            }
            return value;
        }
        public int Agility()
        {
            int value = 0;
            for (int i = 0; i <= gearCount; i++)
            {
                var gear = GetGear(i);
                if (gear != null)
                    value += gear.agility;
            }
            return value;
        }
        public double CritChance()
        {
            double value = 0;
            for (int i = 0; i <= gearCount; i++)
            {
                var gear = GetGear(i);
                if (gear != null)
                    value += gear.critChance;
            }
            return value;
        }
        public double CritMult()
        {
            double value = 0;
            for (int i = 0; i <= gearCount; i++)
            {
                var gear = GetGear(i);
                if (gear != null)
                    value += gear.critMult;
            }
            return value;
        }
        public int Rank()
        {
            double value = 0;
            for (int i = 0; i <= gearCount; i++)
            {
                var gear = GetGear(i);
                if (gear != null)
                    value += gear.tier;
            }
            return Convert.ToInt32(Math.Floor(value/(gearCount - 1)));
        }
        //
        public string VerifyCND()
        {
            string result = null;
            for (int i = 0; i <= gearCount; i++)
            {
                var gear = GetGear(i);
                if (gear != null && gear.condition <= 0)
                {
                    result += gear + Environment.NewLine;
                    SetGear(i, null);
                }
            }
            return result;
        }
    }
}
