using System.Threading;
using System.Threading.Tasks;

namespace Automatica.Driver.Shelly.Common
{
    public interface IShellyCommonClient
    {
        Task<ShellyInfoDto> GetInfo(CancellationToken token = default);
    }
}
