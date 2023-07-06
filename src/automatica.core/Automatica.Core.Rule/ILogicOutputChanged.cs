using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;

namespace Automatica.Core.Logic
{
    /// <summary>
    /// Interface for rule output changes
    /// </summary>
    public interface ILogicOutputChanged
    {
        /// <summary>
        /// Dispatchable instance
        /// </summary>
        IRuleInterfaceInstanceDispatchable Instance { get; }

        /// <summary>
        /// The value
        /// </summary>
        object Value { get; }

        /// <summary>
        /// Source information
        /// </summary>
        string SourceInformation { get; }

        /// <summary>
        /// Value double
        /// </summary>
        double ValueDouble { get; }

        /// <summary>
        /// Value boolean
        /// </summary>
        bool ValueBoolean { get; }

        /// <summary>
        /// Value integer
        /// </summary>
        int ValueInteger { get; }
    }

    /// <summary>
    /// Rule output dispatchable interface
    /// </summary>
    public interface IRuleInterfaceInstanceDispatchable : IDispatchable
    {
        /// <summary>
        /// The instance of the rule interface
        /// </summary>
        RuleInterfaceInstance RuleInterfaceInstance { get; }
    }
}
