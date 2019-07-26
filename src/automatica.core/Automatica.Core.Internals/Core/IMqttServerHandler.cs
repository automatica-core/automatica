using System.Threading.Tasks;
using Automatica.Core.Driver;
using Automatica.Core.EF.Models;

namespace Automatica.Core.Internals.Core
{
    public interface IRemoteServerHandler
    {
        Task Init();

        Task AddNode(string id, NodeInstance node);
        Task AddSlave(string id, IDriverFactory factory);

        Task Stop();
    }
}
