using Automatica.Core.Control.Base;
using Automatica.Core.Control.Cache;
using Automatica.Core.Control.Configuration;

namespace Automatica.Core.Control
{
    internal class ControlContext : IControlContext
    {
        private readonly IControlCache _cache;

        public ControlContext(IControlCache cache)
        {
            _cache = cache;
        }
        
        public Task<List<ISwitch>> GetSwitchesAsync(IControlConfiguration configuration, CancellationToken cancellationToken = default)
        {
           var list = configuration.Switches.Select(x => x.Id).ToList();
           return GetSwitchesAsync(list, cancellationToken);
        }

        public Task<List<ISwitch>> GetSwitchesAsync(List<Guid> switches, CancellationToken cancellationToken = default)
        {
            var ret = new List<ISwitch>();
            foreach (var @switch in switches)
            {
                var switchContext = _cache.Get(@switch);
                if (switchContext != null && switchContext is ISwitch iSwitch)
                {
                    ret.Add(iSwitch);
                }
            }

            return Task.FromResult(ret);
        }

        public Task<ISwitch> GetSwitchAsync(Guid switchId, CancellationToken cancellationToken = default)
        {
            var switchContext = _cache.Get(switchId);
            return Task.FromResult(switchContext as ISwitch);
        }

        public Task<List<IDimmer>> GetDimmerAsync(IControlConfiguration configuration, CancellationToken cancellationToken = default)
        {
            var list = configuration.Switches.Select(x => x.Id).ToList();
            return GetDimmerAsync(list, cancellationToken);
        }

        public Task<List<IDimmer>> GetDimmerAsync(List<Guid> switches, CancellationToken cancellationToken = default)
        {
            var ret = new List<IDimmer>();
            foreach (var dimmer in switches)
            {
                var switchContext = _cache.Get(dimmer);
                if (switchContext != null && switchContext is IDimmer iSwitch)
                {
                    ret.Add(iSwitch);
                }
            }

            return Task.FromResult(ret);
        }

        public Task<IDimmer> GetDimmerAsync(Guid switchId, CancellationToken cancellationToken = default)
        {
            var switchContext = _cache.Get(switchId);
            return Task.FromResult(switchContext as IDimmer);
        }

        public Task<List<IBlind>> GetBlindsAsync(IControlConfiguration configuration, CancellationToken cancellationToken = default)
        {
            var list = configuration.Switches.Select(x => x.Id).ToList();
            return GetBlindsAsync(list, cancellationToken);
        }

        public Task<List<IBlind>> GetBlindsAsync(List<Guid> switches, CancellationToken cancellationToken = default)
        {
            var ret = new List<IBlind>();
            foreach (var dimmer in switches)
            {
                var switchContext = _cache.Get(dimmer);
                if (switchContext != null && switchContext is IBlind iSwitch)
                {
                    ret.Add(iSwitch);
                }
            }

            return Task.FromResult(ret);
        }

        public Task<IBlind> GetBlindAsync(Guid switchId, CancellationToken cancellationToken = default)
        {
            var switchContext = _cache.Get(switchId);
            return Task.FromResult(switchContext as IBlind);
        }
    }
}
