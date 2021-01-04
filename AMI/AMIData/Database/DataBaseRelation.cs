using AMI.Methods;
using MongoDB.Driver;
using System.Threading.Tasks;

namespace AMI.AMIData
{
    class DataBaseRelation<I, O>
    {
        public I _id;
        string TableName => typeof(O).Name;
        private O _Data;
        internal O Data
        {
            get { return NullOrDefault(_Data) ? Load() : _Data; }
            set
            {
                _Data = value;
                if(NullOrDefault(_id)) _id = Utils.GetVar<I, O>(_Data, "_id", true);
            }
        }

        public DataBaseRelation(I id, O obj)
        {
            _id = id;
            _Data = obj;
            if (_Data != null)
                Save();
        }

        internal void Null()
        {
            _id = default;
            _Data = default;
        }

        private FilterDefinition<O> Equal()
        => MongoDatabase.FilterEqual<O, I>("_id", _id);

        public O Load()
        {
            if (NullOrDefault(_id)) return default;
            _Data = AMYPrototype.Program.data.database.LoadRecord<O>
                (TableName, Equal());
            return _Data;
        }

        public async Task<O> LoadAsync()
        {
            if (_id.Equals(default(I))) return default;
            _Data = await AMYPrototype.Program.data.database.LoadRecordAsync<O>
                (TableName, Equal());
            return _Data;
        }

        public void Save()
        {
            if (NullOrDefault(_id) || NullOrDefault(_Data)) return;
            AMYPrototype.Program.data.database.UpdateRecord<O>
                (TableName, Equal(), _Data);
        }

        public async Task SaveAsync()
        {
            if (_id.Equals(default(I)) || NullOrDefault(_Data)) return;
            await AMYPrototype.Program.data.database.UpdateRecordAsync<O>
                (TableName, Equal(), _Data);
        }

        public async Task Delete(string tabName = null)
        {
            if (NullOrDefault(_id)) return;
            await AMYPrototype.Program.data.database.DeleteRecord<O, I>(tabName ?? TableName, _id);
            _id = default;
            _Data = default;
        }

        public async Task ChangeId(I nId)
        {
            O data = _Data == null ? Load() : _Data;

            await AMYPrototype.Program.data.database.DeleteRecord<O, I>
                (TableName, _id);

            _id = nId;

            await SaveAsync();
        }

        bool NullOrDefault<T>(T o) => o == null || o.Equals(default(T));
    }
}
