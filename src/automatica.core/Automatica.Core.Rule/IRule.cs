using System.Collections.Generic;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;

namespace Automatica.Core.Rule
{
    /// <summary>
    /// Rule interface
    /// </summary>
    public interface IRule
    {
        /// <summary>
        /// Will be called when an input value has changed
        /// </summary>
        /// <param name="instance">The instance of the interface</param>
        /// <param name="source">The source of the value</param>
        /// <param name="value">The value itself</param>
        /// <returns>A list of <see cref="IRuleOutputChanged"/> with the output values who has changed</returns>
        IList<IRuleOutputChanged> ValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value);

        /// <summary>
        /// Special data for the UI
        /// </summary>
        /// <returns>Only the UI impl knows</returns>
        object GetDataForVisu();

        /// <summary>
        /// Will be called on start
        /// </summary>
        /// <returns>True if success full</returns>
        Task<bool> Start();

        /// <summary>
        /// Will be called on stop
        /// </summary>
        /// <returns>True if success full</returns>
        Task<bool> Stop();
    }
}
