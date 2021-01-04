using AMI.Neitsillia.Collections;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace AMYPrototype
{
    [BsonIgnoreExtraElements]
    public class PlayerActivity
    {
        [BsonId]
        public string _id;

        public DateTime start;

        public List<StackedObject<ulong, long>> activeUsers;
        public long total;

        public override string ToString()
        {
            return $"{_id} week of {start} with total of {total} {Environment.NewLine}";
        }

        public string ListTopUsers(int top = 3)
        {
            string s = null;
            if (activeUsers == null || activeUsers.Count < 1)
                return "No registered user activity";
            for (int i = 0; i < top && i < activeUsers.Count; i++)
                s += $"<@{activeUsers[i].item}> : {activeUsers[i].count}" + Environment.NewLine;
            return s;
        }

        [JsonConstructor]
        public PlayerActivity()
        {

        }

        public PlayerActivity(bool l)
        {
            _id = "current";
            start = DateTime.UtcNow;
            activeUsers = new List<StackedObject<ulong, long>>();
            Save();
        }

        internal void Activity(ulong u)
        {
            int i = activeUsers.FindIndex((x) => { return x.item == u; });
            if (i > -1)
                activeUsers[i].count++;
            else
                activeUsers.Add(new StackedObject<ulong, long>(u, 1));
            total++;
        }

        internal void Archive()
        {
            PlayerActivity top = LoadTop();
            if (top == null || top.total < total)
            {
                this._id = "top";
                this.Save();
            }
        }

        internal void Save()
        {
            _ = Program.data.database.UpdateRecordAsync("PlayerActivity", "_id", _id, this);
        }

        internal static PlayerActivity LoadTop()
        => Program.data.database.LoadRecord("PlayerActivity", AMI.AMIData.MongoDatabase.FilterEqual<PlayerActivity, string>("_id", "top"));
        internal static PlayerActivity LoadCurrent()
        => Program.data.database.LoadRecord("PlayerActivity", AMI.AMIData.MongoDatabase.FilterEqual<PlayerActivity, string>("_id", "current"));
    }
}