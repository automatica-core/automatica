using System;
using System.Collections.Generic;

namespace Automatica.Core.Base.Cache
{
    public interface IStore<T1, T2>
    {
        void Add(T1 key, T2 value);
        bool Contains(T1 key);

        T2 Get(T1 key);

        ICollection<T2> All();

        void Clear();
        IDictionary<T1, T2> Dictionary();
    }

    public interface IStore<T> : IStore<Guid, T>
    {
      
    }
}
