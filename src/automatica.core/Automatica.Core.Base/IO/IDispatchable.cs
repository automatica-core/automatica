using System;

namespace Automatica.Core.Base.IO
{
    /// <summary>
    /// Dispatchable type
    /// </summary>
    public enum DispatchableType
    {
        NodeInstance = 0,
        RuleInstance = 1,
        Visualization = 2,
        Control = 3
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

    }
}
