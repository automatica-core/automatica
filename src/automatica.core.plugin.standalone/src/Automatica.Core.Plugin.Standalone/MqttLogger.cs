using Microsoft.Extensions.Logging;
using MQTTnet.Diagnostics.Logger;
using System;

namespace Automatica.Core.Plugin.Standalone
{
    internal class MqttLogger : IMqttNetLogger
    {
        private readonly ILogger _logger;

        public bool IsEnabled => !String.IsNullOrEmpty(Environment.GetEnvironmentVariable("MQTT_LOG_VERBOSE"));

        public MqttLogger(ILogger logger)
        {
            _logger = logger;
        }

        private LogLevel Convert(MqttNetLogLevel logLevel)
        {
            switch (logLevel)
            {
                case MqttNetLogLevel.Verbose:
                    return LogLevel.Trace;
                case MqttNetLogLevel.Info:
                    return LogLevel.Information;
                case MqttNetLogLevel.Warning:
                    return LogLevel.Warning;
                case MqttNetLogLevel.Error:
                    return LogLevel.Error;
                default:
                    return LogLevel.None;
            }
        }

        public void Publish(MqttNetLogLevel logLevel, string source, string message, object[] parameters, Exception exception)
        {
            _logger.Log(Convert(logLevel), message, parameters);
        }
    }
}
