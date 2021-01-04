using AMI.Neitsillia.Collections;
using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AMI.Neitsillia.Areas.Strongholds
{
    class StrongholdProduction
    {
        internal long KoinProfit;
        internal long KoinCost;
        internal List<string> results = new List<string>();

        internal string[] FinalResult()
        {
            return new[]
            {
               $"Total Production Cost: {KoinCost}"
                + Environment.NewLine + $"Total Production Gain: {KoinProfit}"
                + Environment.NewLine + $"Result: {(KoinProfit > KoinCost ? "Gained " + (KoinProfit - KoinCost) : KoinCost > KoinProfit ? "Cost " + (-(KoinCost + KoinProfit)) : "None")}",

               results.Count > 0 ? Methods.ArrayM.ToString(results, Environment.NewLine) : null
            };
        }
    }
}
