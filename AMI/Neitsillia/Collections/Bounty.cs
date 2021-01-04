using AMI.Neitsillia.NPCSystems;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace AMI.Neitsillia.Collections
{
    [Table("Bounties")]
    class Bounties
    {
        public List<Bounty> bounties = new List<Bounty>();

        public int Count => bounties.Count;
        public Bounty this[int index] => bounties[index];

        public void Sort()
        {
            bounties.Sort(delegate (Bounty bt1, Bounty bt2)
            {
                return bt1.floor.CompareTo(bt2.floor);
            });
        }
        public int[] FindIndexRanges(int floor)
        {
            int[] res = new int[2];
                res[0] = bounties.FindIndex(FindFloor(floor));
                res[1] = res[0];
                if (res[0] == -1)
                    return res;
                while (bounties.Count > res[1] + 1 && 
                bounties[res[1] + 1].floor == floor)
                    res[1]++;
            return res;
        }
        public static Predicate<Bounty> FindFloor(int argFloor)
        {
            return delegate (Bounty item) { return item.floor == argFloor; };
        }
        private static Predicate<Bounty> FindName(string displayName)
        {
            return delegate (Bounty item) { return item.target.displayName == displayName; };
        }
        public NPC GetAt(int index)
        {
            return bounties[index].target;
        }
        public void Add(Bounty bounty)=>bounties.Add(bounty);
        public void Remove(int index)=>bounties.RemoveAt(index);

        public int FindIndex(string displayName)
        {
            return bounties.FindIndex(Bounties.FindName(displayName));
        }

        
    }
    class Bounty
    {
        public NPC target;
        public int floor;

        [JsonConstructor]
        public Bounty(bool json) { }
        public Bounty(NPC mob, int onFloor)
        {
            target = mob; floor = onFloor;
        }

        public string PinBoard()
        {
            return $"{target.displayName} |Level: {target.level} |Floor:{floor}{Environment.NewLine}";
        }
        //
    }
}
