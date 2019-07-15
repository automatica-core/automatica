using System;
using System.Collections.Generic;

namespace Automatica.Core.Runtime.Abstraction.Plugins
{
    internal interface IStore<T1, T2>
    {
        void Add(T1 key, T2 value);
        bool Contains(T1 key);

        T2 Get(T1 key);

        ICollection<T2> All();

        void Clear();
        IDictionary<T1, T2> Dictionary();
    }

    internal interface IStore<T> : IStore<Guid, T>
    {
      
    }
}
