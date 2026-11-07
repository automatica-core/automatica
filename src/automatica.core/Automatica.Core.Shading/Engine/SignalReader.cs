using System.Globalization;
using System.Text.Json;
using Automatica.Core.Shading.Definition;

namespace Automatica.Core.Shading.Engine
{
    internal sealed record SignalReading(double Value, string Source);

    /// <summary>
    /// Resolves a <see cref="Signal"/>: the first source with a fresh value wins.
    /// </summary>
    internal sealed class SignalReader
    {
        private readonly IShadingEnvironment _environment;

        public SignalReader(IShadingEnvironment environment)
        {
            _environment = environment;
        }

        /// <param name="calculate">Value of a <see cref="CalculatedSource"/> (gets the source for its cloud cover), null when the signal cannot be calculated</param>
        /// <param name="convert">Brightness of an <see cref="IrradianceSource"/> or <see cref="PvSource"/> from its raw value (already converted with the factor), null for other signals</param>
        public SignalReading Read(Signal signal, DateTimeOffset now, Func<CalculatedSource, SignalReading> calculate = null,
            Func<SignalSource, double, SignalReading> convert = null)
        {
            if (signal?.Sources == null)
            {
                return null;
            }

            foreach (var source in signal.Sources)
            {
                switch (source)
                {
                    case DatapointSource datapoint:
                    {
                        var value = ReadDatapoint(datapoint.Node, datapoint.Factor, datapoint.Offset, datapoint.StaleAfterMinutes, now);
                        if (value.HasValue)
                        {
                            return new SignalReading(value.Value, $"datapoint:{datapoint.Node}");
                        }
                        break;
                    }
                    case IrradianceSource irradiance when convert != null:
                    {
                        var value = ReadDatapoint(irradiance.Node, irradiance.Factor, irradiance.Offset, irradiance.StaleAfterMinutes, now);
                        var converted = value.HasValue ? convert(irradiance, value.Value) : null;
                        if (converted != null)
                        {
                            return converted;
                        }
                        break;
                    }
                    case PvSource pv when convert != null:
                    {
                        var value = ReadDatapoint(pv.Node, pv.Factor, 0, pv.StaleAfterMinutes, now);
                        var converted = value.HasValue ? convert(pv, value.Value) : null;
                        if (converted != null)
                        {
                            return converted;
                        }
                        break;
                    }
                    case CalculatedSource calculated when calculate != null:
                    {
                        var value = calculate(calculated);
                        if (value != null)
                        {
                            return value;
                        }
                        break;
                    }
                }
            }

            return null;
        }

        private double? ReadDatapoint(Guid node, double factor, double offset, int staleAfterMinutes, DateTimeOffset now)
        {
            if (node == Guid.Empty)
            {
                return null;
            }

            var value = _environment.GetValue(node);
            if (value?.Value == null)
            {
                return null;
            }

            if (staleAfterMinutes > 0 && value.Timestamp.HasValue &&
                now - value.Timestamp.Value > TimeSpan.FromMinutes(staleAfterMinutes))
            {
                return null;
            }

            var number = ToDouble(value.Value);
            if (!number.HasValue || double.IsNaN(number.Value) || double.IsInfinity(number.Value))
            {
                return null;
            }

            return number.Value * factor + offset;
        }

        internal static double? ToDouble(object value)
        {
            switch (value)
            {
                case null:
                    return null;
                case bool b:
                    return b ? 1 : 0;
                case double d:
                    return d;
                case float f:
                    return f;
                case decimal m:
                    return (double)m;
                case IConvertible convertible when value is not string:
                    try
                    {
                        return convertible.ToDouble(CultureInfo.InvariantCulture);
                    }
                    catch (Exception e) when (e is FormatException or InvalidCastException or OverflowException)
                    {
                        return null;
                    }
                case string s:
                    if (bool.TryParse(s, out var parsedBool))
                    {
                        return parsedBool ? 1 : 0;
                    }
                    return double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) ? parsed : null;
                case JsonElement element:
                    return element.ValueKind switch
                    {
                        JsonValueKind.Number => element.GetDouble(),
                        JsonValueKind.True => 1,
                        JsonValueKind.False => 0,
                        JsonValueKind.String => ToDouble(element.GetString()),
                        _ => null
                    };
                default:
                    return null;
            }
        }
    }
}
