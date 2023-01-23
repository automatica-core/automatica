using System;
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
    }
}
