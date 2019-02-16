using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace P3.Driver.EnOcean.Logger
{
    public static class Logger
    {
        public static ILogger Instance { get; set; } = NullLogger.Instance;
    }
}
