using System;

namespace Automatica.Core.Base.IO
{
    /// <summary>
    /// Dispatchable type
    /// </summary>
    public enum DispatchableType
    {
        NodeInstance,
        RuleInstance
    }

    /// <summary>
    /// Dispatchable source
    /// </summary>
    public enum DispatchableSource
    {
        NodeInstance,
        RuleInstance,
        Visualisation
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

    }
}
