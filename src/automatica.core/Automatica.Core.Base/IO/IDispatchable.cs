using System;

namespace Automatica.Core.Base.IO
{
    /// <summary>
    /// Dispatchable type
    /// </summary>
    public enum DispatchableType
    {
        NodeInstance,
        RuleInstance,
        Visualization
    }

    /// <summary>
    /// Dispatchable source
    /// </summary>
    public enum DispatchableSource
    {
        NodeInstance,
        RuleInstance,
        Visualization,
        Remote,
        RemanentValue
    }

    /// <summary>
    /// Dispatchable interface
    /// </summary>
    public interface IDispatchable
    {
        DispatchableSource Source { get; }
        DispatchableType Type { get;  }
        string Name { get; }
        Guid Id { get;  }

        public bool IsRemanent { get; }
        public bool WriteOnlyIfChanged { get; }

    }
}
