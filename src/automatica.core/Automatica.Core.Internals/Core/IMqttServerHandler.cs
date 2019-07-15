using System.Threading.Tasks;
using Automatica.Core.Driver;
using Automatica.Core.EF.Models;

namespace Automatica.Core.Internals.Core
{
    public interface IMqttServerHandler
    {
        Task Init();

        Task AddMqttNode(string id, NodeInstance node);
        Task AddMqttSlave(string id, IDriverFactory factory);

        Task Stop();
    }
}
