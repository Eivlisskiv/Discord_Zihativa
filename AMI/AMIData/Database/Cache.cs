using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AMI.AMIData.Database
{
    class Cache<Key, Obj>
    { 
        Dictionary<Key, CachedObject<Key, Obj>> cache;

        public Cache()
        {
            cache = new Dictionary<Key, CachedObject<Key, Obj>> ();
        }

        public Obj Load(Key key)
        {
            if (cache.ContainsKey(key))
            {
                cache[key].ResetTimer(DisposeElement);
                return cache[key].obj;
            }
            return default;
        }

        public void Save(Key key, Obj obj)
        {
            if (cache.ContainsKey(key)) cache[key].obj = obj;
            else cache.Add(key, new CachedObject<Key, Obj>(key, obj));

            cache[key].ResetTimer(DisposeElement);
        }

        void DisposeElement(Key key)
        {
            cache.Remove(key);
        }
    }

    class CachedObject<K, O>
    {
        const int cacheTime = 60000;

        public K key;
        public O obj;

        private CancellationTokenSource token; 
        public Task timer;

        public CachedObject(K key, O obj)
        {
            this.key = key;
            this.obj = obj;
            token = new CancellationTokenSource();
        }

        public void ResetTimer(Action<K> func)
        {
            if (timer != null && timer.Status == TaskStatus.Running)
            {
                token.Cancel();
                timer.Dispose();
            }

            timer = Task.Run(async () =>
            {
                await Task.Delay(cacheTime);
                func(key);

            }, token.Token);
        }
    }
}
