using Automatica.Core.Driver;
using OpenWeatherMap;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace P3.Driver.OpenWeatherMap.DriverFactory
{
    internal class OpenWeatherMapDriverNode : DriverBase
    {
        private readonly Func<CurrentWeatherResponse, object> _valueFunc;

        public OpenWeatherMapDriverNode(IDriverContext driverContext, Func<CurrentWeatherResponse, object> valueFunc) : base(driverContext)
        {
            _valueFunc = valueFunc;
        }

        public object GetValue(CurrentWeatherResponse weatherResponse)
        {
            return _valueFunc.Invoke(weatherResponse);
        }

        public override Task<bool> Read(CancellationToken token = default)
        {
            return Parent.Read(token);
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            return null;
        }
    }
}
