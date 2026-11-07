using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using Automatica.Core.EF.Models;

namespace Automatica.Core.Internals.Core
{
    public interface ICoreServer
    {
        void Restart();
        Task ReInit();
        Task ReloadLogicServices();
        Task ReloadLogic(Guid ruleInstanceId);
        Task StopLogic(Guid ruleInstanceId);
        Task RemoveLink(Guid linkId);
        Task ReloadLinks();

        Task StopDriver(IDriver driver);
        Task InitializeAndStartDriver(NodeInstance nodeInstance, NodeTemplate nodeTemplate);

        Task StartStopDriver(NodeInstance nodeInstance);

        /// <summary>
        /// Installs the staged plugin packages. A plugin that is in use is updated without a restart where possible:
        /// only its drivers and logics are stopped, the old version is unloaded, the new one loaded and its drivers and logics started again.
        /// </summary>
        /// <returns>true if a restart is still required, the packages that need it stay staged and are installed on the next start</returns>
        Task<bool> UpdatePlugins(IReadOnlyCollection<PluginUpdate> updates);
    }
}
