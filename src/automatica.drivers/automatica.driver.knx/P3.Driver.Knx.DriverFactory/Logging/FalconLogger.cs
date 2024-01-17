using Knx.Falcon.Logging;
using System;
using Microsoft.Extensions.Logging;

namespace P3.Driver.Knx.DriverFactory.Logging
{
    internal class FalconLogger : IFalconLogger
    {
        private readonly ILogger _logger;
        private readonly string _name;

        public FalconLogger(ILogger logger, string name)
        {
            _logger = logger;
            _name = name;
        }
        public void Debug(object message)
        {
            _logger.LogDebug($"{_name}: {message}");
        }

        public void Debug(object message, Exception exception)
        {
            _logger.LogDebug($"{_name}: {message} {exception}");
        }

        public void DebugFormat(string format, params object[] args)
        {
            _logger.LogDebug(format, args);
        }

        public void Error(object message)
        {
            _logger.LogError($"{_name}: {message}");
        }

        public void Error(object message, Exception exception)
        {
            _logger.LogError($"{_name}: {message} {exception}");
        }

        public void ErrorFormat(string format, params object[] args)
        {
            _logger.LogError(format, args);
        }

        public void Info(object message)
        {
            _logger.LogInformation($"{_name}: {message}");
        }

        public void Info(object message, Exception exception)
        {
            _logger.LogInformation($"{_name}: {message} {exception}");
        }

        public void InfoFormat(string format, params object[] args)
        {
            _logger.LogInformation(format, args);
        }

        public void Warn(object message)
        {
            _logger.LogWarning($"{_name}: {message}");
        }

        public void Warn(object message, Exception exception)
        {
            _logger.LogWarning($"{_name}: {message} {exception}");
        }

        public void WarnFormat(string format, params object[] args)
        {
            _logger.LogWarning(format, args);
        }

        public bool IsDebugEnabled { get; } = true;
        public bool IsErrorEnabled { get; } = true;
        public bool IsInfoEnabled { get; } = true;
        public bool IsWarnEnabled { get; } = true;
    }
}
