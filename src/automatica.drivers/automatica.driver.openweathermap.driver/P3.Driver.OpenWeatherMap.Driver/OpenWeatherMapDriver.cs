using System;
using Automatica.Core.Driver;
using Microsoft.Extensions.Logging;
using OpenWeatherMap;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using System.Threading;
using Timer = System.Timers.Timer;
using Newtonsoft.Json.Linq;

namespace P3.Driver.OpenWeatherMap.DriverFactory
{
    internal class OpenWeatherMapDriver : DriverNoneAttributeBase
    {
        private readonly Timer _timer = new();
        private Coordinates _cords;
        private OpenWeatherMapClient _client;

        private readonly List<OpenWeatherMapDriverNode> _nodes = new();

        private readonly ILogger _logger;
        private readonly int _timeZoneOffset = 0;
        private int _forecastCount;

        public OpenWeatherMapDriver(IDriverContext driverContext) : base(driverContext)
        {
            _logger = driverContext.Logger;

            var timeZoneOffset = driverContext.NodeTemplateFactory.GetSetting("timezoneOffset");

            try
            {
                if (timeZoneOffset != null)
                {
                    _timeZoneOffset = timeZoneOffset!.ValueInt!.Value;
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


        public override Task<bool> Init(CancellationToken token = default)
        {
            var pollTime = GetPropertyValueInt("poll");
            var apiKey = GetPropertyValueString("api-key");
            _forecastCount = GetPropertyValueInt("forecast_count");

            _timer.Elapsed += _timer_Elapsed;
            _timer.Interval = pollTime * 1000;

            _client = new OpenWeatherMapClient(apiKey);

            var latitude = DriverContext.NodeTemplateFactory.GetSetting("Latitude").ValueDouble;
            var longitude = DriverContext.NodeTemplateFactory.GetSetting("Longitude").ValueDouble;

            _cords = new Coordinates
            {
                Latitude = latitude!.Value,
                Longitude = longitude!.Value
            };

            _logger.LogInformation($"Using longitude {longitude} latitude {latitude} refresh rate {_timer.Interval}ms");
          


            return base.Init(token);
        }

        public override async Task<bool> Start(CancellationToken token = default)
        {
            _timer.Start();

            try
            {
                await ReadValues();
            }
            catch(Exception e)
            {
                DriverContext.Logger.LogError(e, "Could not read values...");
            }
            return await base.Start(token);
        }

        private async void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                await ReadValues();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error reading values...{e}");
            }
        }

        protected override async Task<bool> Read(IReadContext readContext, CancellationToken token = new CancellationToken())
        {
            await ReadValues();
            return true;
        }


        private async Task ReadValues()
        {
            var data = await _client.CurrentWeather.GetByCoordinates(_cords, MetricSystem.Metric, OpenWeatherMapLanguage.EN);

            var forecast = await _client.Forecast.GetByCoordinates(_cords, metric: MetricSystem.Metric, language: OpenWeatherMapLanguage.EN, count: _forecastCount);

            _logger.LogDebug($"Getting data for city {data.City.Country} - {data.City.Name}");

            foreach (var node in _nodes)
            {
                var value = node.GetValue(data, forecast);
                node.DispatchRead(value);
            }
        }

        public override Task<bool> Stop(CancellationToken token = default)
        {
            _timer.Elapsed -= _timer_Elapsed;
            return base.Stop(token);
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            OpenWeatherMapDriverNode node = null;
            switch(ctx.NodeInstance.This2NodeTemplateNavigation.Key)
            {
                case "openweathermap-sunrise":
                {
                    node = new OpenWeatherMapDriverNode(ctx, (x,_) => x.City.Sun.Rise.ToLocalTime().AddHours(_timeZoneOffset));
                    break;
                }
                case "openweathermap-sunset":
                {
                    node = new OpenWeatherMapDriverNode(ctx, (x, _) => x.City.Sun.Set.ToLocalTime().AddHours(_timeZoneOffset));
                    break;
                }
                case "openweathermap-humidity":
                {
                    node = new OpenWeatherMapDriverNode(ctx, (x, _) => x.Humidity.Value);
                    break;
                }
                case "openweathermap-pressure":
                {
                    node = new OpenWeatherMapDriverNode(ctx, (x, _) => x.Pressure.Value);
                    break;
                }
                case "openweathermap-wind-speed":
                {
                    node = new OpenWeatherMapDriverNode(ctx, (x, _) => x.Wind.Speed.Value);
                    break;
                }
                case "openweathermap-wind-direction":
                {
                    node = new OpenWeatherMapDriverNode(ctx, (x, _) => x.Wind.Direction.Value);
                    break;
                }
                case "openweathermap-temperature":
                {
                    node = new OpenWeatherMapDriverNode(ctx, (x, _) => x.Temperature.Value);
                    break;
                }
                case "openweathermap-temperature-max":
                {
                    node = new OpenWeatherMapDriverNode(ctx, (x, _) => x.Temperature.Max);
                    break;
                }
                case "openweathermap-temperature-min":
                {
                    node = new OpenWeatherMapDriverNode(ctx, (x, _) => x.Temperature.Min);
                    break;
                }

                case "openweathermap-forecast-humidity":
                {
                    node = new OpenWeatherMapDriverNode(ctx, (_, y) => Math.Round(y.Forecast.Average(a => a.Humidity.Value), 2));
                    break;
                }
                case "openweathermap-forecast-pressure":
                {
                    node = new OpenWeatherMapDriverNode(ctx, (_, y) => Math.Round(y.Forecast.Average(a => a.Pressure.Value), 2));
                    break;
                }
                case "openweathermap-forecast-wind-speed":
                {
                    node = new OpenWeatherMapDriverNode(ctx, (_, y) => Math.Round(y.Forecast.Average(a => a.WindSpeed.Mps), 2));
                    break;
                }
                case "openweathermap-forecast-wind-direction":
                    {
                    node = new OpenWeatherMapDriverNode(ctx, (_, y) => Math.Round(y.Forecast.Average(a => a.WindSpeed.Mps), 2));
                    break;
                    }
                case "openweathermap-forecast-temperature":
                {
                    node = new OpenWeatherMapDriverNode(ctx, (_, y) => Math.Round(y.Forecast.Average(a => a.Temperature.Value), 2));
                    break;
                }
                case "openweathermap-forecast-temperature-min":
                {
                    node = new OpenWeatherMapDriverNode(ctx, (_, y) => Math.Round(y.Forecast.Average(a => a.Temperature.Min), 2));
                    break;
                }
                case "openweathermap-forecast-temperature-max":
                {
                    node = new OpenWeatherMapDriverNode(ctx, (_, y) => Math.Round(y.Forecast.Average(a => a.Temperature.Max), 2));
                    break;
                }
                case "openweathermap-forecast-clouds":
                {
                    node = new OpenWeatherMapDriverNode(ctx, (_, y) => Math.Round(y.Forecast.Average(a => a.Clouds.All), 2));
                    break;
                }
                case "openweathermap-forecast-clouds-description":
                {
                    node = new OpenWeatherMapDriverNode(ctx, (_, y) => y.Forecast.Last().Clouds.Value);
                    break;
                }
                case "openweathermap-forecast-precipitation":
                {
                    node = new OpenWeatherMapDriverNode(ctx, (_, y) => y.Forecast.First().Precipitation.Value);
                    break;
                }
                case "openweathermap-forecast-precipitation-description":
                {
                    node = new OpenWeatherMapDriverNode(ctx, (_, y) => y.Forecast.First().Precipitation.Type);
                    break;
                }
                case "openweathermap-forecast-from":
                {
                    node = new OpenWeatherMapDriverNode(ctx, (_, y) => y.Forecast.First().From);
                    break;
                }
                case "openweathermap-forecast-to":
                {
                    node = new OpenWeatherMapDriverNode(ctx, (_, y) => y.Forecast.Last().To);
                    break;
                }

                case "openweathermap-forecast":
                {
                    return new OpenWeatherMapForecastDriverNode(ctx, this);
                }
            }

            if(node != null)
            {
                _nodes.Add(node);
            }

            return node;
        }

        internal void AddNode(OpenWeatherMapDriverNode node)
        {
            _nodes.Add(node);
        }
    }
}
