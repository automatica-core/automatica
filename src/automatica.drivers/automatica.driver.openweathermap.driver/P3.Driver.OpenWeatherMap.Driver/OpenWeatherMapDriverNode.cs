using Automatica.Core.Driver;
using OpenWeatherMap;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace P3.Driver.OpenWeatherMap.DriverFactory
{
    internal class OpenWeatherMapDriverNode : DriverBase
    {
        private readonly Func<CurrentWeatherResponse, ForecastResponse, object> _valueFunc;

        public OpenWeatherMapDriverNode(IDriverContext driverContext, Func<CurrentWeatherResponse, ForecastResponse, object> valueFunc) : base(driverContext)
        {
            _valueFunc = valueFunc;
        }

        public object GetValue(CurrentWeatherResponse weatherResponse, ForecastResponse forecastResponse)
        {
            return _valueFunc.Invoke(weatherResponse, forecastResponse);
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
