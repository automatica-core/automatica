using Microsoft.Extensions.Logging;

namespace Automatica.Core.Logging
{
    public interface ICoreLogger : ILogger
    {
        public LogLevel LogLevel { get; set; }
        public string Facility { get; }
    }
}
