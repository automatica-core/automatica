using System;
using System.Threading.Tasks;
using Automatica.Core.EF.Models;

namespace Automatica.Core.Runtime.IO
{
    public interface ILogicEngineDispatcher : IDisposable
    {
        Task<bool> Load();
        Task<bool> Reload();
        Task Unlink(Guid id);
        Task Unload();

        /// <summary>
        /// Routes values the visualization writes to the interfaces of <paramref name="logicInstance"/> into the logic.
        /// <see cref="Load"/> does this for every logic it finds; logics added or changed later need this call. Interfaces
        /// that are already registered are skipped, so calling it again is harmless.
        /// </summary>
        Task RegisterLogicInterfaces(RuleInstance logicInstance);

        /// <summary>
        /// Hands the current value of every data point linked to a logic parameter to the logic, only to the logic
        /// <paramref name="ruleInstanceId"/> when given. Call it after logics (re)started or links changed: a logic resets its
        /// parameters to the configured values on start and a link only delivers the next dispatched value.
        /// </summary>
        Task ReplayLinkedParameters(Guid? ruleInstanceId = null);
    }
}
