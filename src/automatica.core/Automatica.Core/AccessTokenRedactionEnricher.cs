using System.Linq;
using Automatica.Core.Logging;
using Serilog.Core;
using Serilog.Events;

namespace Automatica.Core
{
    /// <summary>
    /// Redacts the SignalR access token (query string) in logged properties (request path, query string, url)
    /// </summary>
    internal sealed class AccessTokenRedactionEnricher : ILogEventEnricher
    {
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            foreach (var property in logEvent.Properties.ToList())
            {
                if (property.Value is ScalarValue { Value: string text })
                {
                    var redacted = LogRedaction.Redact(text);
                    if (!ReferenceEquals(redacted, text))
                    {
                        logEvent.AddOrUpdateProperty(new LogEventProperty(property.Key, new ScalarValue(redacted)));
                    }
                }
            }
        }
    }
}
