using System;
using System.Threading.Tasks;
using System.Threading;
using Automatica.Driver.Shelly.Dtos.Shelly1PM;

namespace Automatica.Driver.Shelly.Clients
{
    public interface IShelly<T>
    {
        Task<RelayDto> SetStatus(int channelId, bool value, CancellationToken token);
        Task<ShellyResult<T>> GetStatus(CancellationToken cancellationToken, TimeSpan? timeout = null);
    }
}
