using System;
using Automatica.Core.Driver;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using System.Threading;
using P3.Driver.OpenWeather.Api;
using P3.Driver.OpenWeather.Api.Enums;
using P3.Driver.OpenWeather.Api.Models.OneCallModel;
using Timer = System.Timers.Timer;
using P3.Driver.OpenWeatherMap.DriverFactory.Forecast.Daily;
using P3.Driver.OpenWeatherMap.DriverFactory.Forecast.Hourly;

namespace P3.Driver.OpenWeatherMap.DriverFactory
{
    internal class OpenWeatherMapDriver : DriverNoneAttributeBase
    {
        private readonly Timer _timer = new();
        private (double Latitude, double Longitude) _coordinates;
        private WeatherClient _client;

        private readonly List<OpenWeatherMapDriverBaseNode> _nodes = new();

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

            _client = new WeatherClient(apiKey);

            var latitude = DriverContext.NodeTemplateFactory.GetSetting("Latitude").ValueDouble;
            var longitude = DriverContext.NodeTemplateFactory.GetSetting("Longitude").ValueDouble;

            _coordinates = (latitude!.Value, longitude!.Value);

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
            var oneCallData = await _client.GetOneCallAsync(_coordinates.Latitude, _coordinates.Longitude, [],
                Measurement.Metric, GetLanguage());

            _logger.LogDebug($"Getting data for city {oneCallData.TimezoneName}");

            foreach (var node in _nodes)
            {
                var value = node.GetValue(oneCallData);
                if (value != null)
                {
                    node.DispatchRead(value);
                }
            }
        }

        private Language GetLanguage()
        {
            switch (DriverContext.LocalizationProvider.GetLocale())
            {
                case "de":
                    return Language.German;
                default:
                    return Language.English;
            }
        }

        public override Task<bool> Stop(CancellationToken token = default)
        {
            _timer.Stop();
            _timer.Elapsed -= _timer_Elapsed;
            return base.Stop(token);
        }

        /// <summary>
        /// The daily forecasts used for the forecast average values, limited to the configured forecast count
        /// </summary>
        internal IReadOnlyList<Daily> ForecastDays(OneCallModel model)
        {
            return LimitForecastDays(model.DailyForecasts!, _forecastCount);
        }

        internal static IReadOnlyList<Daily> LimitForecastDays(IReadOnlyList<Daily> days, int forecastCount)
        {
            if (days.Count == 0)
            {
                return days;
            }

            var count = Math.Clamp(forecastCount, 1, days.Count);
            return days.Take(count).ToList();
        }

        /// <summary>
        /// Average of angles in degrees (e.g. 350° and 10° = 0°), result is normalized to 0..360
        /// </summary>
        internal static double CircularMeanDegrees(IEnumerable<double> degrees)
        {
            double sin = 0, cos = 0;
            foreach (var degree in degrees)
            {
                var rad = degree * Math.PI / 180.0;
                sin += Math.Sin(rad);
                cos += Math.Cos(rad);
            }

            var mean = Math.Atan2(sin, cos) * 180.0 / Math.PI;
            if (mean < 0)
            {
                mean += 360.0;
            }

            return mean >= 360.0 ? mean - 360.0 : mean;
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            OpenWeatherMapDriverBaseNode node = null;
            switch (ctx.NodeInstance.This2NodeTemplateNavigation.Key)
            {
                case "openweathermap-sunrise":
                {
                    node = new OpenWeatherMapDriverNode(ctx,
                        (x) => x.CurrentWeather!.SunriseTime.ToLocalTime().AddHours(_timeZoneOffset));
                    break;
                }
                case "openweathermap-sunset":
                {
                    node = new OpenWeatherMapDriverNode(ctx,
                        (x) => x.CurrentWeather!.SunsetTime.ToLocalTime().AddHours(_timeZoneOffset));
                    break;
                }
                case "openweathermap-humidity":
                {
                    node = new OpenWeatherMapDriverNode(ctx, (x) => x.CurrentWeather!.HumidityPercentage);
                    break;
                }
                case "openweathermap-pressure":
                {
                    node = new OpenWeatherMapDriverNode(ctx, (x) => x.CurrentWeather!.AtmosphericPressure);
                    break;
                }
                case "openweathermap-wind-speed":
                {
                    node = new OpenWeatherMapDriverNode(ctx, (x) => x.CurrentWeather!.WindSpeed);
                    break;
                }
                case "openweathermap-wind-direction":
                {
                    node = new OpenWeatherMapDriverNode(ctx, (x) => x.CurrentWeather!.WindDirection);
                    break;
                }
                case "openweathermap-temperature":
                {
                    node = new OpenWeatherMapDriverNode(ctx, (x) => x.CurrentWeather!.Temperature);
                    break;
                }
                case "openweathermap-temperature-max":
                {
                    node = new OpenWeatherMapDriverNode(ctx, (x) => x.DailyForecasts!.First().Temperature.Maximum);
                    break;
                }
                case "openweathermap-temperature-min":
                {
                    node = new OpenWeatherMapDriverNode(ctx, (x) => x.DailyForecasts.First().Temperature.Minimum);
                    break;
                }
                case "openweathermap-forecast-humidity":
                {
                    node = new OpenWeatherMapDriverNode(ctx,
                        (y) => Math.Round(ForecastDays(y).Average(a => a.HumidityPercentage), 2));
                    break;
                }
                case "openweathermap-forecast-pressure":
                {
                    node = new OpenWeatherMapDriverNode(ctx,
                        (y) => Math.Round(ForecastDays(y).Average(a => a.AtmosphericPressure), 2));
                    break;
                }
                case "openweathermap-forecast-wind-speed":
                {
                    node = new OpenWeatherMapDriverNode(ctx,
                        (y) => Math.Round(ForecastDays(y).Average(a => a.WindSpeed), 2));
                    break;
                }
                case "openweathermap-forecast-wind-direction":
                {
                    node = new OpenWeatherMapDriverNode(ctx,
                        (y) => Math.Round(CircularMeanDegrees(ForecastDays(y).Select(a => (double)a.WindDirection)), 2));
                    break;
                }
                case "openweathermap-forecast-temperature":
                {
                    node = new OpenWeatherMapDriverNode(ctx,
                        (y) => Math.Round(ForecastDays(y).Average(a => a.Temperature.Day), 2));
                    break;
                }
                case "openweathermap-forecast-temperature-min":
                {
                    node = new OpenWeatherMapDriverNode(ctx,
                        (y) => Math.Round(ForecastDays(y).Min(a => a.Temperature.Minimum), 2));
                    break;
                }
                case "openweathermap-forecast-temperature-max":
                {
                    node = new OpenWeatherMapDriverNode(ctx,
                        (y) => Math.Round(ForecastDays(y).Max(a => a.Temperature.Maximum), 2));
                    break;
                }
                case "openweathermap-forecast-clouds":
                {
                    node = new OpenWeatherMapDriverNode(ctx,
                        (y) => Math.Round(ForecastDays(y).Average(a => a.CloudPercentage), 2));
                    break;
                }
                case "openweathermap-forecast-clouds-description":
                {
                    node = new OpenWeatherMapDriverNode(ctx,
                        (y) => ForecastDays(y).Last().Description.First().Description);
                    break;
                }
                case "openweathermap-forecast-precipitation":
                {
                    node = new OpenWeatherMapDriverNode(ctx, (y) => y.DailyForecasts!.First().PrecipitationProbability);
                    break;
                }
                case "openweathermap-forecast-precipitation-description":
                {
                    node = new OpenWeatherMapDriverNode(ctx, (y) => y.DailyForecasts!.First().Summary);
                    break;
                }
                case "openweathermap-forecast-from":
                {
                    node = new OpenWeatherMapDriverNode(ctx, (y) => y.DailyForecasts!.First().AnalysisDate);
                    break;
                }
                case "openweathermap-forecast-to":
                {
                    node = new OpenWeatherMapDriverNode(ctx, (y) => ForecastDays(y).Last().AnalysisDate);
                    break;
                }
                case "openweathermap-forecast-average":
                {
                    return new OpenWeatherMapForecastDriverNode(ctx, this);
                }
                case "openweathermap-forecast-daily":
                {
                    node = new ForecastDailyNode(ctx);
                    break;
                }
                case "openweathermap-forecast-hourly":
                {
                    node = new ForecastHourlyNode(ctx);
                    break;
                }
                //case "openweathermap-forecast-minutely":
                //{
                //    return new OpenWeatherMapForecastDriverNode(ctx, this);
                //}
            }

            if (node != null)
            {
                _nodes.Add(node);
            }
            return node;
        }
    }
}
