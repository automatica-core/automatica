using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;

namespace Automatica.Core.NamedPipeSink
{
    public static class PipeLoggerExtensions
    {

        private static IDictionary<string, NamedPipeSink> _sinks = new ConcurrentDictionary<string, NamedPipeSink>();

        public static LoggerConfiguration NamedPipe(
            this LoggerSinkConfiguration sinkConfiguration,
            string pipeName,
            LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
            LoggingLevelSwitch levelSwitch = null,
            Encoding encoding = null
        )
        {
            if (sinkConfiguration == null) throw new ArgumentNullException(nameof(sinkConfiguration));
            if (pipeName == null) throw new ArgumentNullException(nameof(pipeName));

            NamedPipeSink sink;
            if (_sinks.ContainsKey(pipeName))
            {
                sink = _sinks[pipeName];
            }
            else
            {
                sink = new NamedPipeSink(pipeName, encoding);
                _sinks.Add(pipeName, sink);
            }

            return sinkConfiguration.Sink(sink, restrictedToMinimumLevel, levelSwitch);
        }

    }
}
