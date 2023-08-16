using Automatica.Core.Driver;
using System.Threading;
using System.Threading.Tasks;

namespace P3.Driver.OpenWeatherMap.DriverFactory
{
    internal class OpenWeatherMapForecastDriverNode : DriverBase
    {
        private readonly OpenWeatherMapDriver _parent;

        public OpenWeatherMapForecastDriverNode(IDriverContext driverContext, OpenWeatherMapDriver parent) : base(driverContext)
        {
            _parent = parent;
        }

        public override Task<bool> Init(CancellationToken token = default)
        {
            return Task.FromResult(true);
        }

        public override Task<bool> Start(CancellationToken token = default)
        {
            return base.Start(token);
        }

        public override Task<bool> Read(CancellationToken token = default)
        {
            return _parent.Read(token);
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            var driverNode = _parent.CreateDriverNode(ctx);
            return driverNode;
        }
    }
}
