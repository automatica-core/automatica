using Automatica.Core.Control.Base;
using Automatica.Core.Control.Configuration;

namespace Automatica.Core.Control
{
    public interface IControlContext
    {
        Task<List<ISwitch>> GetSwitchesAsync(ControlConfiguration configuration, CancellationToken cancellationToken = default);
        Task<List<ISwitch>> GetSwitchesAsync(List<Guid> switches, CancellationToken cancellationToken = default);
        Task<ISwitch> GetSwitchAsync(Guid switchId, CancellationToken cancellationToken = default);


        Task<List<IDimmer>> GetDimmerAsync(ControlConfiguration configuration, CancellationToken cancellationToken = default);
        Task<List<IDimmer>> GetDimmerAsync(List<Guid> dimmer, CancellationToken cancellationToken = default);
        Task<IDimmer> GetDimmerAsync(Guid dimmerId, CancellationToken cancellationToken = default);


        Task<List<IBlind>> GetBlindsAsync(ControlConfiguration configuration, CancellationToken cancellationToken = default);
        Task<List<IBlind>> GetBlindsAsync(List<Guid> blinds, CancellationToken cancellationToken = default);
        Task<IBlind> GetBlindAsync(Guid blindId, CancellationToken cancellationToken = default);
    }
}
