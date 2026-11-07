using System;
using System.Collections.Concurrent;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Automatica.Driver.Shelly.Unit.Tests.Fakes
{
    internal sealed record LogEntry(LogLevel Level, string Message, Exception Exception);

    internal sealed class CapturingLogger : ILogger
    {
        public ConcurrentQueue<LogEntry> Entries { get; } = new();

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            Entries.Enqueue(new LogEntry(logLevel, formatter(state, exception), exception));
        }

        public bool Contains(LogLevel level, string text) =>
            Entries.Any(a => a.Level == level && a.Message.Contains(text, StringComparison.OrdinalIgnoreCase));
    }
}
