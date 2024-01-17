using Automatica.Core.Control.Base;
using Automatica.Core.Control.Cache;

namespace Automatica.Core.Control
{
    internal class ControlContext(IControlCache cache) : IControlContext
    {
        public Task<List<IControl>> GetAsync(List<Guid> switches, CancellationToken cancellationToken = default)
        {
            var ret = new List<IControl>();
            foreach (var @switch in switches)
            {
                var controlContext = cache.Get(@switch);
                if(controlContext != null)
                {
                    ret.Add(controlContext);
                }
                
            }

            return Task.FromResult(ret);
        }

        public Task<IControl> GetAsync(Guid controlId, CancellationToken cancellationToken = default)
        {
            var controlContext = cache.Get(controlId);
            return Task.FromResult(controlContext);
        }

       
        public Task<List<ISwitch>> GetSwitchesAsync(List<Guid> switches, CancellationToken cancellationToken = default)
        {
            var ret = new List<ISwitch>();
            foreach (var @switch in switches)
            {
                var switchContext = cache.Get(@switch);
                if (switchContext != null && switchContext is ISwitch iSwitch)
                {
                    ret.Add(iSwitch);
                }
            }

            return Task.FromResult(ret);
        }

        public Task<ISwitch> GetSwitchAsync(Guid switchId, CancellationToken cancellationToken = default)
        {
            var switchContext = cache.Get(switchId);
            return Task.FromResult(switchContext as ISwitch);
        }

       
        public Task<List<IDimmer>> GetDimmerAsync(List<Guid> switches, CancellationToken cancellationToken = default)
        {
            var ret = new List<IDimmer>();
            foreach (var dimmer in switches)
            {
                var switchContext = cache.Get(dimmer);
                if (switchContext != null && switchContext is IDimmer iSwitch)
                {
                    ret.Add(iSwitch);
                }
            }

            return Task.FromResult(ret);
        }

        public Task<IDimmer> GetDimmerAsync(Guid switchId, CancellationToken cancellationToken = default)
        {
            var switchContext = cache.Get(switchId);
            return Task.FromResult(switchContext as IDimmer);
        }

     
        public Task<List<IBlind>> GetBlindsAsync(List<Guid> switches, CancellationToken cancellationToken = default)
        {
            var ret = new List<IBlind>();
            foreach (var dimmer in switches)
            {
                var switchContext = cache.Get(dimmer);
                if (switchContext != null && switchContext is IBlind iSwitch)
                {
                    ret.Add(iSwitch);
                }
            }

            return Task.FromResult(ret);
        }

        public Task<IBlind> GetBlindAsync(Guid switchId, CancellationToken cancellationToken = default)
        {
            var switchContext = cache.Get(switchId);
            return Task.FromResult(switchContext as IBlind);
        }
    }
}
