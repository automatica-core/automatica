using Automatica.Core.Driver;
using Microsoft.Extensions.Logging;
using OpenWeatherMap;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Timers;

namespace P3.Driver.OpenWeatherMap.DriverFactory
{
    internal class OpenWeatherMapDriver : DriverBase
    {
        private Timer _timer = new Timer();
        private Coordinates _cords;
        private OpenWeatherMapClient _client;

        private List<OpenWeatherMapDriverNode> _nodes = new List<OpenWeatherMapDriverNode>();

        private ILogger _logger;
        private readonly int _timeZoneOffset = 0;

        public OpenWeatherMapDriver(IDriverContext driverContext) : base(driverContext)
        {
            _logger = driverContext.Logger;

            var timeZoneOffset = driverContext.NodeTemplateFactory.GetSetting("timezoneOffset");

            try
            {
                if (timeZoneOffset != null)
                {
                    _timeZoneOffset = timeZoneOffset.ValueInt.Value;
                }
                else
                {
                    _timeZoneOffset = 0;
                }
            }
            catch
            {
                //ignore exception
            }
        }

        public override bool Init()
        {
            var pollTime = GetPropertyValueInt("poll");
            var apiKey = GetPropertyValueString("api-key");

            _timer.Elapsed += _timer_Elapsed;
            _timer.Interval = pollTime * 1000;

            _client = new OpenWeatherMapClient("17cd16005872728916a7475b71d6050e");

            var latitude = DriverContext.NodeTemplateFactory.GetSetting("Latitude").ValueDouble;
            var longitude = DriverContext.NodeTemplateFactory.GetSetting("Longitude").ValueDouble;

            _cords = new Coordinates()
            {
                Latitude = latitude.Value,
                Longitude = longitude.Value
            };

            _logger.LogInformation($"Using longitude {longitude} latitude {latitude} refresh rate {_timer.Interval}ms");
          


            return base.Init();
        }

        public override async Task<bool> Start()
        {
            _timer.Start();

            await ReadValues();

            return await base.Start();
        }

        private async void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            await ReadValues();

        }

        public override async Task<bool> Read()
        {
            await ReadValues();

            return true;
        }

        private async Task ReadValues()
        {
            var data = await _client.CurrentWeather.GetByCoordinates(_cords, MetricSystem.Metric, OpenWeatherMapLanguage.EN);

            _logger.LogDebug($"Getting data for city {data.City.Country} - {data.City.Name}");

            foreach (var node in _nodes)
            {
                var value = node.GetValue(data);
                node.DispatchValue(value);
            }
        }

        public override Task<bool> Stop()
        {
            _timer.Elapsed -= _timer_Elapsed;
            return base.Stop();
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            OpenWeatherMapDriverNode node = null;
            switch(ctx.NodeInstance.This2NodeTemplateNavigation.Key)
            {
                case "openweathermap-sunrise":
                    node = new OpenWeatherMapDriverNode(ctx, (x) => x.City.Sun.Rise.ToLocalTime().AddHours(_timeZoneOffset));
                    break;
                case "openweathermap-sunset":
                    node = new OpenWeatherMapDriverNode(ctx, (x) => x.City.Sun.Set.ToLocalTime().AddHours(_timeZoneOffset));
                    break;
                case "openweathermap-humidity":
                    node = new OpenWeatherMapDriverNode(ctx, (x) => x.Humidity.Value);
                    break;
                case "openweathermap-pressure":
                    node = new OpenWeatherMapDriverNode(ctx, (x) => x.Pressure.Value);
                    break;
                case "openweathermap-wind-speed":
                    node = new OpenWeatherMapDriverNode(ctx, (x) => x.Wind.Speed.Value);
                    break;
                case "openweathermap-wind-direction":
                    node = new OpenWeatherMapDriverNode(ctx, (x) => x.Wind.Direction.Value);
                    break;
                case "openweathermap-temperature":
                    node = new OpenWeatherMapDriverNode(ctx, (x) => x.Temperature.Value);
                    break;
                case "openweathermap-temperature-max":
                    node = new OpenWeatherMapDriverNode(ctx, (x) => x.Temperature.Max);
                    break;
                case "openweathermap-temperature-min":
                    node = new OpenWeatherMapDriverNode(ctx, (x) => x.Temperature.Min);
                    break;
            }

            if(node != null)
            {
                _nodes.Add(node);
            }

            return node;
        }
    }
}
