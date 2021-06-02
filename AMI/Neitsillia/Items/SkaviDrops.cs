using AMI.Methods;
using AMYPrototype;
using AMI.Neitsillia.Items.ItemPartials;
using System;
using System.Collections.Generic;

namespace AMI.Neitsillia.Items
{
    class SkaviDrops
    {
        static string[] Universals =
        {
            "Sketoxis Spike",
            "Sketoxis String",
            "Sketoxis Handle",
            "Sketoxis Whip",
            "Atsaukan Handle",
            "Atsaukan Blade",
            "Atsaukan Sword",
        };

        static Dictionary<string, string[]> Category = new Dictionary<string, string[]>()
        {
            {
                "Atsauka", new string[]
                {
                    "Atsaukan Handle",
                    "Atsaukan Blade",
                    "Atsaukan Sword",
                }
            }
        };

        public static Item FromArea(string areaName)
        {
            switch(areaName)
            {
                case "Muzoisu":
                case "Amethyst Gardens":
                    return DropSchematic("Atsauka");
                default:
                    return DropSchematic(areaName);
            }
        }

        public static Item DropSchematic(string category)
        {
            if (category == null || Program.Chance(50) ||
                !Category.TryGetValue(category, out string[] drops))
                drops = Universals;

            if(drops?.Length > 0)
            {
                try
                {
                    Item b = Item.LoadItem(Utils.RandomElement(drops), "Skavi");
                    if (b == null) return null;

                    return Item.NewTemporarySchematic(b);
                }catch(Exception e)
                {
                    Log.LogS(e);
                }
            }
            return null;
        }


    }
}
