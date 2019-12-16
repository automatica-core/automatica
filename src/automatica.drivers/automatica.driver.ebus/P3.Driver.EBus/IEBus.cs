using System.Runtime.CompilerServices;
using System.Threading.Tasks;

[assembly: InternalsVisibleTo("P3.Driver.EBus.Tests")]

namespace P3.Driver.EBus
{
    public interface IEBus
    {
        Task<bool> Connect();
        Task<bool> Disconnect();

    }
}
