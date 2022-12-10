using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Automatica.Core.Base.Cache
{
    public abstract class GuidStoreBase<T> : StoreBase<Guid, T> 
    {

    }

    public abstract class StoreBase<T1, T2> : IStore<T1, T2> 
    {
        private readonly IDictionary<T1, T2> _store = new ConcurrentDictionary<T1, T2>();

        public virtual void Add(T1 key, T2 value)
        {
            if (!_store.ContainsKey(key))
            {
                _store.Add(key, value);
            }
        }

        public virtual void Update(T1 key, T2 value)
        {
            if (_store.ContainsKey(key))
            {
                _store[key] = value;
            }
        }

        public virtual void Remove(T1 key)
        {
            if (Contains(key))
            {
                _store.Remove(key);
            }
        }

        public virtual bool Contains(T1 key)
        {
            return _store.ContainsKey(key);
        }

        public virtual T2 Get(T1 key)
        {
            if (Contains(key))
            {
                return _store[key];
            }
            return default(T2);
        }

        public ICollection<T2> Get(params T1[] keys)
        {
            var ret = new List<T2>();

            foreach (var key in keys)
            {
                if (Contains(key))
                {
                    ret.Add(_store[key]);
                }
            }

            return ret;
        }

        public virtual ICollection<T2> All()
        {
            return _store.Values;
        }

        public IDictionary<T1, T2> Dictionary()
        {
            return _store;
        }

        public virtual void Clear()
        {
            _store.Clear();
        }
    }
}
