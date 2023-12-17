using Automatica.Core.Control.Base;
using Automatica.Core.Control.Configuration;

namespace Automatica.Core.Control
{
    public interface IControlContext
    {
        Task<List<ISwitch>> GetSwitchesAsync(IControlConfiguration configuration, CancellationToken cancellationToken = default);
        Task<List<ISwitch>> GetSwitchesAsync(List<Guid> switches, CancellationToken cancellationToken = default);
        Task<ISwitch> GetSwitchAsync(Guid switchId, CancellationToken cancellationToken = default);


        Task<List<IDimmer>> GetDimmerAsync(IControlConfiguration configuration, CancellationToken cancellationToken = default);
        Task<List<IDimmer>> GetDimmerAsync(List<Guid> switches, CancellationToken cancellationToken = default);
        Task<IDimmer> GetDimmerAsync(Guid switchId, CancellationToken cancellationToken = default);


        Task<List<IBlind>> GetBlindsAsync(IControlConfiguration configuration, CancellationToken cancellationToken = default);
        Task<List<IBlind>> GetBlindsAsync(List<Guid> switches, CancellationToken cancellationToken = default);
        Task<IBlind> GetBlindAsync(Guid switchId, CancellationToken cancellationToken = default);
    }
}
