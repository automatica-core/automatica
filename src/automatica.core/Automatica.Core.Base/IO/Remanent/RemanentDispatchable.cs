using System;

namespace Automatica.Core.Base.IO.Remanent
{
    internal class RemanentDispatchable : IDispatchable
    {
        public DispatchableSource Source { get; } = DispatchableSource.RemanentValue;
        public DispatchableType Type { get; } = DispatchableType.NodeInstance;
        public string Name => $"remanent source {Id}";
        public Guid Id { get; }
        public bool IsRemanent => false;

        public RemanentDispatchable(Guid id)
        {
            Id = id;
        }
    }
}
