using System.Threading.Tasks;
using Automatica.Core.Driver;

namespace Automatica.Core.Plugin.Standalone.Abstraction
{
    public interface IDriverConnection : IConnection
    {
        IDriverFactory Factory { get; }

        Task<bool> Start();
        Task<bool> Stop();

        Task Run();

        Task<bool> Send(string topic, object data);
    }
}
