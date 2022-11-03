using System.Collections.Generic;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using Automatica.Core.EF.Models;

namespace Automatica.Core.Internals.Core
{
    public interface IRemoteServerHandler
    {
        IList<string> GetConnectedClients();
        Task Init();

        Task AddNode(string id, NodeInstance node);
        Task AddSlave(string id, IDriverFactory factory, NodeInstance nodeInstance);

        Task Stop();
    }
}
