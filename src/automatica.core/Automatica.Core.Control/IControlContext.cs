using Automatica.Core.Control.Base;

namespace Automatica.Core.Control
{
    public interface IControlContext
    { 
        Task<List<IControl>> GetAsync(List<Guid> controls, CancellationToken cancellationToken = default);
        Task<IControl> GetAsync(Guid controlId, CancellationToken cancellationToken = default);

        Task<List<ISwitch>> GetSwitchesAsync(List<Guid> switches, CancellationToken cancellationToken = default);
        Task<ISwitch> GetSwitchAsync(Guid switchId, CancellationToken cancellationToken = default);


        Task<List<IDimmer>> GetDimmerAsync(List<Guid> dimmer, CancellationToken cancellationToken = default);
        Task<IDimmer> GetDimmerAsync(Guid dimmerId, CancellationToken cancellationToken = default);

        Task<List<IBlind>> GetBlindsAsync(List<Guid> blinds, CancellationToken cancellationToken = default);
        Task<IBlind> GetBlindAsync(Guid blindId, CancellationToken cancellationToken = default);
    }
}
