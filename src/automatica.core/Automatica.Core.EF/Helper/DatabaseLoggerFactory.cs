using Microsoft.Extensions.Logging;

namespace Automatica.Core.EF.Helper
{
    internal class DatabaseLoggerFactory : ILoggerFactory
    {
        private readonly DatabaseLogger _logger;

        public DatabaseLoggerFactory()
        {
            _logger = new DatabaseLogger();
        }
        public void AddProvider(ILoggerProvider provider)
        {
        }

        public ILogger CreateLogger(string categoryName)
        {
            return _logger;
        }

        public void Dispose()
        {
         
        }
    }
}
