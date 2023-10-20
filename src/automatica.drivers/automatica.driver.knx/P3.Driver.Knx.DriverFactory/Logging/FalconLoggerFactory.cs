using Knx.Falcon.Logging;
using Microsoft.Extensions.Logging;

namespace P3.Driver.Knx.DriverFactory.Logging
{
    internal class FalconLoggerFactory : IFalconLoggerFactory
    {
        private readonly ILogger _logger;

        public FalconLoggerFactory(ILogger logger)
        {
            _logger = logger;
        }

        public IFalconLogger GetLogger(string name)
        {
            return new FalconLogger(_logger, name);
        }
    }
}
