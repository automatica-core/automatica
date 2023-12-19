using Automatica.Core.Base.IO;
using System;

namespace Automatica.Core.Driver
{
    public sealed class GenericDispatchableNode : IDispatchable
    {
        public GenericDispatchableNode(string name, Guid id, DispatchableSource source)
        {
            Type = DispatchableType.NodeInstance;
            Name = name;
            Id = id;
            Source = source;
            IsRemanent = false;
        }

        public DispatchableType Type { get; }
        public string Name { get; }
        public Guid Id { get; }
        public bool IsRemanent { get; }

        public DispatchableSource Source { get; }
    }
}
