using Automatica.Core.Base.IO;
using System;

namespace Automatica.Push.Data
{
    internal sealed class DispatchableInstance : IDispatchable
    {
        public DispatchableInstance(DispatchableType type, string name, Guid id, DispatchableSource source)
        {
            Type = type;
            Name = name;
            Id = id;
            Source = source;
        }

        public DispatchableType Type { get; }
        public string Name { get; }
        public Guid Id { get; }

        public DispatchableSource Source { get; }

    }
}
