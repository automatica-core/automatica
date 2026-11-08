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
    }
}
