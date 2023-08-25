using Automatica.Core.Driver;
using System.Threading;
using System.Threading.Tasks;

namespace P3.Driver.OpenWeatherMap.DriverFactory
{
    internal class OpenWeatherMapForecastDriverNode : DriverNotWriteableBase
    {
        private readonly OpenWeatherMapDriver _parent;

        public OpenWeatherMapForecastDriverNode(IDriverContext driverContext, OpenWeatherMapDriver parent) : base(driverContext)
        {
            _parent = parent;
        }

        protected override Task<bool> Read(IReadContext readContext, CancellationToken token = new CancellationToken())
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
