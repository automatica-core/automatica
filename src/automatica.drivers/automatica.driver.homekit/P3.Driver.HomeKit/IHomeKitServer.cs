using System.Threading.Tasks;

namespace P3.Driver.HomeKit
{
    public interface IHomeKitServer
    {
        Task<bool> Start();
        Task<bool> Stop();
    }
}
