using Automatica.Core.Base.IO;
using System;

namespace Automatica.Push.Data
{
    internal sealed class DispatchableInstance : IDispatchable
    {
        public DispatchableInstance(DispatchableType type, string name, Guid id, DispatchableSource source, bool isRemanent)
        {
            Type = type;
            Name = name;
            Id = id;
            Source = source;
            IsRemanent = isRemanent;
        }

        public DispatchableType Type { get; }
        public string Name { get; }
        public Guid Id { get; }
        public bool IsRemanent { get; }
        public bool WriteOnlyIfChanged => false;

        public DispatchableSource Source { get; }

    }
}
