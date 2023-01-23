using System;
using System.Collections.Generic;
using System.Linq;
using Serilog.Events;

namespace Automatica.Core.NamedPipeSink
{
    /// <summary>
    /// A wrapper class for <see cref="LogEvent"/> that is sent as a message to SignalR clients.
    /// </summary>
    public class SimplifiedLogEvent
    {
        /// <summary>
        /// Construct a new <see cref="LogEvent"/>.
        /// </summary>
        public SimplifiedLogEvent() { }

        /// <summary>
        /// Construct a new <see cref="LogEvent"/>.
        /// </summary>
        public static string Serialize(Serilog.Events.LogEvent logEvent, string renderedMessage)
        {
            var e = new SimplifiedLogEvent()
            {
                Timestamp = logEvent.Timestamp,
                Exception = logEvent.Exception,
                MessageTemplate = logEvent.MessageTemplate.Text,
                Level = logEvent.Level,
                RenderedMessage = renderedMessage,
                Properties = new Dictionary<string, object>()
            };

            foreach (var pair in logEvent.Properties)
            {
                e.Properties.Add(pair.Key, SimplifyPropertyFormatter.Simplify(pair.Value));
            }

            return Newtonsoft.Json.JsonConvert.SerializeObject(e);
        }

        /// <summary>
        /// The time at which the event occurred.
        /// </summary>
        public DateTimeOffset Timestamp { get; set; }

        /// <summary>
        /// The template that was used for the log message.
        /// </summary>
        public string MessageTemplate { get; set; }

        /// <summary>
        /// The level of the log.
        /// </summary>
        public LogEventLevel Level { get; set; }

        /// <summary>
        /// A string representation of the exception that was attached to the log (if any).
        /// </summary>
        public Exception Exception { get; set; }

        /// <summary>
        /// The rendered log message.
        /// </summary>
        public string RenderedMessage { get; set; }

        /// <summary>
        /// Properties associated with the event, including those presented in <see cref="LogEvent.MessageTemplate"/>.
        /// </summary>
        public IDictionary<string, object> Properties { get; set; }
    }


    public static class SimplifyPropertyFormatter
    {
        static readonly HashSet<Type> SpecialScalars = new HashSet<Type>
        {
            typeof(bool),
            typeof(byte), typeof(short), typeof(ushort), typeof(int), typeof(uint),
                typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal),
            typeof(byte[])
        };

        /// <summary>
        /// Simplify the object so as to make handling the serialized
        /// representation easier.
        /// </summary>
        /// <param name="value">The value to simplify (possibly null).</param>
        /// <returns>A simplified representation.</returns>
        public static object Simplify(LogEventPropertyValue value)
        {
            if (value is ScalarValue scalar)
                return SimplifyScalar(scalar.Value);

            if (value is DictionaryValue dict)
                return dict
                    .Elements
                    .ToDictionary(kv => SimplifyScalar(kv.Key), kv => Simplify(kv.Value));

            if (value is SequenceValue seq)
                return seq.Elements.Select(Simplify).ToArray();

            if (value is StructureValue str)
            {
                var props = str.Properties.ToDictionary(p => p.Name, p => Simplify(p.Value));
                if (str.TypeTag != null)
                    props["$typeTag"] = str.TypeTag;
                return props;
            }

            return null;
        }

        static object SimplifyScalar(object value)
        {
            if (value == null)
                return null;

            var valueType = value.GetType();
            if (SpecialScalars.Contains(valueType))
                return value;

            return value.ToString();
        }
    }
}
