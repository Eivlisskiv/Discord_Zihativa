using AMI.Methods;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AMI.AMIData
{
    class MongoDatabase
    {

        internal static FilterDefinition<T> FilterStartsWith<T>(string idVariableName, string targetId)
            => Builders<T>.Filter.Regex(idVariableName, $"^{targetId}");

        internal static FilterDefinition<T> FilterRegex<T>(string idVariableName, string expression)
            => Builders<T>.Filter.Regex(idVariableName, expression);

        internal static FilterDefinition<Treturn> FilterEqual<Treturn, Uid>(string idVariableName, Uid targetId)
            => Builders<Treturn>.Filter.Eq(idVariableName, targetId);

        internal static FilterDefinition<Treturn> FilterEqualAndLtAndGt<Treturn, Uid, Var2>(string idVariableName, Uid targetId, string var2, Var2 max, Var2 min)
        {
            var builder = Builders<Treturn>.Filter;
            var filter = builder.And(builder.Eq(idVariableName, targetId), builder.And(builder.Lt(var2, max), builder.Gt(var2, min)));
            return filter;
        }

        internal static FilterDefinition<Treturn> FilterLtAndGt<Treturn, Var2>
            (string var2, Var2 max, Var2 min)
        {
            var builder = Builders<Treturn>.Filter;
            var filter = builder.And(builder.Lt(var2, max), builder.Gt(var2, min));
            return filter;
        }


        readonly string name;
        readonly string url;

        internal IMongoDatabase database;

        public MongoDatabase(string dbName, string aurl, string user, string password)
        {
            name = dbName;
            url = aurl;
            Connect($"mongodb+srv://{user}:{password}@{url}/{dbName}?retryWrites=true&w=majority");
        }
        public MongoDatabase(string dbName)
        {
            //islocal = true;
            name = dbName;
            Connect("mongodb://localhost");
        }

        void Connect(string url)
        {
            var client = new MongoClient(url);
            database = client.GetDatabase(name);
        }

        string TableName<T>(string given) => given ?? TableName<T>();
        string TableName<T>() => typeof(T).Name + "s";

        //Save
        internal async Task SaveRecordAsync<T>(string tableKey, T record)
          => await database.GetCollection<T>(TableName<T>(tableKey)).InsertOneAsync(record);

        internal void SaveRecord<T>(string tableKey, T record)
          => database.GetCollection<T>(TableName<T>(tableKey)).InsertOne(record);
        internal async Task SaveRecord<T>(string tableKey, params T[] records)
          => await database.GetCollection<T>(TableName<T>(tableKey)).InsertManyAsync(records);

        //Update
        internal async Task UpdateRecordAsync<T>(string tableKey, string idVariableName, string id, T record)
        => await database.GetCollection<T>(TableName<T>(tableKey)).ReplaceOneAsync(
                new BsonDocument(idVariableName, id), record, new ReplaceOptions
                { IsUpsert = true });

        internal async Task UpdateRecordAsync<T>(string tableKey, FilterDefinition<T> filter, T record)
			=> await database.GetCollection<T>(TableName<T>(tableKey)).ReplaceOneAsync(filter, record, new ReplaceOptions
			{ IsUpsert = true });

        internal void UpdateRecord<T>(string tableKey, FilterDefinition<T> filter, T record)
            => database.GetCollection<T>(TableName<T>(tableKey)).ReplaceOne(
                filter ?? FilterEqual<T, string>("_id", Methods.Utils.GetVar<string, T>(record, "_id") ?? throw new Exception($"{typeof(T).Name} does not have default _id property"))
                , record, new ReplaceOptions
            { IsUpsert = true });

        internal void UpdateRecord<T>(string tableKey, string idVariableName, string id, T record) 
            => database.GetCollection<T>(TableName<T>(tableKey)).ReplaceOne(
                           new BsonDocument(idVariableName, id), record, new ReplaceOptions
                           { IsUpsert = true });

        public async Task Increment<T, Id>(string table, Id id, string field, int amount)
            => await database.GetCollection<T>(table).UpdateOneAsync($"{{ \"_id\" : {id} }}", $"{{ $inc: {{ \"{field}\" : {amount} }} }}");
        public async Task IncrementMany<T, Id>(string table, string query, string field, int amount)
            => await database.GetCollection<T>(table).UpdateManyAsync(query, $"{{ $inc: {{ \"{field}\" : {amount} }} }}");


        public async Task SetField<T, I>(string table, I id, string field, int value)
            => await database.GetCollection<T>(table).UpdateOneAsync($"{{ \"_id\" : {id} }}", $"{{ $set: {{ \"{field}\" : {value} }} }}");

        public async Task SetFieldMany<T, Id>(string table, string query, string field, int amount)
            => await database.GetCollection<T>(table).UpdateManyAsync(query, $"{{ $set: {{ \"{field}\" : {amount} }} }}");

        //Delete
        internal async Task DeleteRecord<T>(string tableKey, string targetId, string idVariableName = "_id")
        {
            await database.GetCollection<T>(TableName<T>(tableKey)).DeleteOneAsync
                (Builders<T>.Filter.Eq(idVariableName, targetId));
        }
        internal async Task DeleteRecord<T, U>(string tableKey, U targetId, string idVariableName = "_id")
        {
            await database.GetCollection<T>(TableName<T>(tableKey)).DeleteOneAsync
                (Builders<T>.Filter.Eq(idVariableName, targetId));
        }

        internal async Task<T> LoadRecordAsync<T>(string tableKey, FilterDefinition<T> filter)
        {
            try
            {
                var result = await database.GetCollection<T>(TableName<T>(tableKey)).FindAsync(filter);
                return await result.FirstOrDefaultAsync();
            }
            catch (Exception e)
            {
                Methods.Log.LogS(e);
                return default;
            }
        }

        internal async Task<T> LoadRecordAsync<T, I>(I id)
            => await LoadRecordAsync<T>(TableName<T>(), FilterEqual<T, I>("_id", id));
        internal async Task<T> LoadRecordAsync<T, I>(string tabName, I id)
           => await LoadRecordAsync<T>(tabName ?? TableName<T>(), FilterEqual<T, I>("_id", id));

        internal T LoadRecord<T>(string tableKey, FilterDefinition<T> filter, bool notifyOnDefault = false)
        {

            tableKey = TableName<T>(tableKey);
            var result = database.GetCollection<T>(tableKey).Find(filter);

            if (result.Any()) return result.First();

            if (notifyOnDefault) Console.WriteLine($"Failed to load Database entry from {tableKey} Table with filter: {Environment.NewLine} {filter.ToJson()}");

            return default;
        }

        internal T LoadRecord<T, I>(string tableKey, I id)
            => LoadRecord(TableName<T>(tableKey), FilterEqual<T, I>("_id", id));

        internal async Task<List<T>> LoadRecordsAsync<T>(string tableKey, FilterDefinition<T> filter = null)
        {
            var result = await database.GetCollection<T>(TableName<T>(tableKey)).FindAsync(filter ?? "{}");
            return await result.ToListAsync();
        }

        internal List<T> LoadRecords<T>(string tableKey, FilterDefinition<T> filter = null)
        {
            var docs = database.GetCollection<T>(TableName<T>(tableKey));
            var result = filter != null ? docs.Find(filter ?? "{}") : docs.Find(_ => true);
            return result.ToList();
        }

        public async Task<List<T>> LoadRecordsContain<T>(string tableKey, string fieldSearch, string contains)
         => await LoadRecordsAsync<T>(tableKey, $"{{ \"{fieldSearch}\" : {{$regex : \".*{contains}.*\", $options: \"i\" }} }}");

        internal List<T> LoadSortRecords<T>(string tableKey, FilterDefinition<T> filter, string sort)
        {
            var result = database.GetCollection<T>(TableName<T>(tableKey)).Find(filter).Sort(sort);
            return result.ToList();
        }

        internal long GetRecordsCount(string tableKey) =>
            database.GetCollection<object>(tableKey).CountDocuments(new BsonDocument());

        //Verify Existence
        internal bool IdExists<T, U>(string tableKey, U targetID, string idVariableName = "_id")
            => database.GetCollection<T>(TableName<T>(tableKey)).Find(FilterEqual<T, U>(idVariableName, targetID)).CountDocuments() > 0;

        internal string Query(string tableKey, string jsonQuery, string fields = null)
        {
            try
            {
                var collection = database.GetCollection<BsonDocument>(tableKey);

                BsonDocument query = null;
                if (jsonQuery != null && !BsonDocument.TryParse(jsonQuery, out query))
                {
                    Log.LogS("Could not parse query " + jsonQuery);
                    return "{ \"error\": \"invalid query\" }";
                }
                var filter = jsonQuery != null ? collection.Find(query) : collection.Find("{}");

                if (fields != null)
                {
                    var project = Builders<BsonDocument>.Projection.Include(fields);
                    filter = filter.Project(project);
                }

                return filter.CountDocuments() > 1 ? filter.ToList().ToJson()
                    : filter.FirstOrDefault().ToJson();

            }
            catch (Exception e)
            {
                Log.LogS(e);
                return $"{{ \"error\": \"{e.Message}\" }}";
            }
        }


        internal R Query<T, R>(string jsonQuery, string field)
        {
            bool parsed = BsonDocument.TryParse(jsonQuery, out BsonDocument query);

            try
            {
                var collection = database.GetCollection<T>(TableName<T>());

                var project = Builders<T>.Projection.Include(field);
                var filter = (parsed ? collection.Find(query) : collection.Find(jsonQuery)).Project<R>(project);

                return filter.FirstOrDefault();
            }
            catch (Exception e)
            {
                Log.LogS(e);
            }

            if (!parsed)  Log.LogS("Could not parse query " + jsonQuery);
            return default;
        }
    }
}
