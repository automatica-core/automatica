using System.Threading.Tasks;

namespace P3.Driver.EBus
{
    public interface IEBus
    {
        Task<bool> Connect();
        Task<bool> Disconnect();

    }
}
