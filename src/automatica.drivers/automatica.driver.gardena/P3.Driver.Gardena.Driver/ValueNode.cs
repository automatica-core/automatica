using System;
using System.Globalization;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using Microsoft.Extensions.Logging;

namespace P3.Driver.Gardena.Driver
{
    /// <summary>
    /// A data point node: dispatches changed values, forwards writes to the owner
    /// </summary>
    internal sealed class ValueNode : DriverBase
    {
        private readonly Func<object, CancellationToken, Task> _write;
        private readonly Func<CancellationToken, Task<bool>> _read;
        private readonly object _lock = new();
        private bool _hasValue;

        public ValueNode(IDriverContext driverContext, string key, Func<object, CancellationToken, Task> write, Func<CancellationToken, Task<bool>> read) : base(driverContext)
        {
            Key = key;
            _write = write;
            _read = read;
        }

        public string Key { get; }

        /// <summary>
        /// Free per-node configuration (e.g. the key of a custom value)
        /// </summary>
        public string Parameter { get; set; }

        public object LastValue { get; private set; }

        public void Set(object value)
        {
            lock (_lock)
            {
                if (_hasValue && Equals(LastValue, value))
                {
                    return;
                }
                _hasValue = true;
                LastValue = value;
            }
            DispatchRead(value);
        }

        /// <summary>
        /// The write path without the dispatcher queue (tests)
        /// </summary>
        internal Task WriteDirectAsync(object value, CancellationToken token = default) => Write(value, null, token);

        protected override async Task Write(object value, IWriteContext writeContext, CancellationToken token = default)
        {
            if (_write == null)
            {
                return;
            }
            try
            {
                await _write(value, token);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                DriverContext.Logger.LogError($"{FullName}: writing {Key} failed: {ex.Message}");
            }
        }

        protected override Task<bool> Read(IReadContext readContext, CancellationToken token = default)
        {
            return _read != null ? _read(token) : Task.FromResult(true);
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx) => null;
    }

    internal static class Conv
    {
        public static bool ToBool(object value)
        {
            return value switch
            {
                bool b => b,
                null => false,
                string s => s.Equals("true", StringComparison.OrdinalIgnoreCase) || s == "1" || s.Equals("on", StringComparison.OrdinalIgnoreCase),
                IConvertible c => Convert.ToDouble(c, CultureInfo.InvariantCulture) != 0,
                _ => false
            };
        }

        public static double ToDouble(object value)
        {
            return value switch
            {
                null => 0,
                bool b => b ? 1 : 0,
                string s => double.Parse(s, NumberStyles.Float, CultureInfo.InvariantCulture),
                IConvertible c => Convert.ToDouble(c, CultureInfo.InvariantCulture),
                _ => 0
            };
        }

        public static string ToText(object value)
        {
            return value switch
            {
                null => "",
                string s => s,
                IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
                _ => value.ToString()
            };
        }

        public static JsonElement? Path(JsonElement element, params string[] path)
        {
            var current = element;
            foreach (var name in path)
            {
                if (current.ValueKind != JsonValueKind.Object || !current.TryGetProperty(name, out var next))
                {
                    return null;
                }
                current = next;
            }
            return current.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined ? null : current;
        }

        public static string Str(JsonElement element, params string[] path)
        {
            var v = Path(element, path);
            return v?.ValueKind switch
            {
                JsonValueKind.String => v.Value.GetString(),
                JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False => v.Value.GetRawText(),
                _ => null
            };
        }

        public static double? Num(JsonElement element, params string[] path)
        {
            var v = Path(element, path);
            if (v?.ValueKind == JsonValueKind.Number)
            {
                return v.Value.GetDouble();
            }
            if (v?.ValueKind == JsonValueKind.String && double.TryParse(v.Value.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var d))
            {
                return d;
            }
            return null;
        }

        public static int? Int(JsonElement element, params string[] path)
        {
            var d = Num(element, path);
            return d.HasValue ? (int)Math.Round(d.Value) : null;
        }

        public static bool? Bool(JsonElement element, params string[] path)
        {
            var v = Path(element, path);
            return v?.ValueKind switch
            {
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.String when bool.TryParse(v.Value.GetString(), out var b) => b,
                _ => null
            };
        }

        /// <summary>
        /// A JSON scalar as a .NET value (bool, int, double, string)
        /// </summary>
        public static object Scalar(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Number when element.TryGetInt32(out var i) => i,
                JsonValueKind.Number => element.GetDouble(),
                JsonValueKind.String => element.GetString(),
                JsonValueKind.Null or JsonValueKind.Undefined => null,
                _ => element.GetRawText()
            };
        }
    }
}
