using System;
using System.Globalization;

namespace P3.Logic.Lightning.Common
{
    public enum CircuitType
    {
        Unused = 0,
        /// <summary>on/off, output true/false</summary>
        Switch = 1,
        /// <summary>output 0..100 %</summary>
        Dimmer = 2,
        /// <summary>output "#RRGGBB", the brightness is applied to the color</summary>
        Rgb = 3,
        /// <summary>output 0..100 % plus the color temperature in kelvin on the second output</summary>
        TunableWhite = 4
    }

    /// <summary>
    /// The state of one light: brightness 0..100, the color at full brightness (RGB) and the color temperature (tunable white)
    /// </summary>
    public sealed record LightValue(double Level, string Color, int Kelvin)
    {
        public const int MinKelvin = 2700;
        public const int MaxKelvin = 6500;
        public const int DefaultKelvin = 4000;
        public const string White = "#FFFFFF";

        public static LightValue Off { get; } = new(0, White, DefaultKelvin);
        public static LightValue FullOn { get; } = new(100, White, DefaultKelvin);

        public bool IsOn => Level > 0;

        public LightValue WithLevel(double level) => this with { Level = Math.Round(Math.Clamp(level, 0, 100), 1) };

        /// <summary>
        /// The output value for the circuit type
        /// </summary>
        public object ToOutput(CircuitType type)
        {
            return type switch
            {
                CircuitType.Switch => IsOn,
                CircuitType.Dimmer => (int)Math.Round(Level),
                CircuitType.TunableWhite => (int)Math.Round(Level),
                CircuitType.Rgb => Scale(Color, Level),
                _ => null
            };
        }

        /// <summary>
        /// Parses a value for one circuit: "on"/"off", true/false, a level "60", "#RRGGBB" (brightness = brightest channel)
        /// or "level@kelvin" ("80@3000"). Parts that are not given are taken from <paramref name="current"/>.
        /// </summary>
        public static LightValue Parse(object value, LightValue current)
        {
            current ??= Off;
            switch (value)
            {
                case null:
                    return null;
                case bool b:
                    return current.WithLevel(b ? (current.IsOn ? current.Level : 100) : 0);
                case string s:
                    return ParseText(s.Trim(), current);
                default:
                    var d = LightingValues.ToDouble(value, double.NaN);
                    return double.IsNaN(d) ? null : current.WithLevel(d);
            }
        }

        private static LightValue ParseText(string s, LightValue current)
        {
            if (s.Length == 0)
            {
                return null;
            }

            if (s.Equals("on", StringComparison.OrdinalIgnoreCase) || s.Equals("true", StringComparison.OrdinalIgnoreCase))
            {
                return current.WithLevel(current.IsOn ? current.Level : 100);
            }
            if (s.Equals("off", StringComparison.OrdinalIgnoreCase) || s.Equals("false", StringComparison.OrdinalIgnoreCase))
            {
                return current.WithLevel(0);
            }

            if (s.StartsWith('#'))
            {
                if (!TryParseColor(s, out var r, out var g, out var b))
                {
                    return null;
                }
                var max = Math.Max(r, Math.Max(g, b));
                if (max == 0)
                {
                    return current.WithLevel(0);
                }
                var color = Format(r * 255.0 / max, g * 255.0 / max, b * 255.0 / max);
                return new LightValue(Math.Round(max * 100.0 / 255, 1), color, current.Kelvin);
            }

            var at = s.IndexOf('@');
            if (at > 0)
            {
                var levelOk = double.TryParse(s[..at], NumberStyles.Float, CultureInfo.InvariantCulture, out var level);
                var kelvinOk = double.TryParse(s[(at + 1)..], NumberStyles.Float, CultureInfo.InvariantCulture, out var kelvin);
                if (!levelOk || !kelvinOk)
                {
                    return null;
                }
                return current.WithLevel(level) with { Kelvin = ClampKelvin(kelvin) };
            }

            return double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var l) ? current.WithLevel(l) : null;
        }

        public static int ClampKelvin(double kelvin) => (int)Math.Round(Math.Clamp(kelvin, MinKelvin, MaxKelvin));

        /// <summary>
        /// The text form used in mood definitions and the visualization ("60", "on", "#FF8800", "80@3000")
        /// </summary>
        public string ToText(CircuitType type)
        {
            return type switch
            {
                CircuitType.Switch => IsOn ? "on" : "off",
                CircuitType.Rgb => Scale(Color, Level),
                CircuitType.TunableWhite => string.Create(CultureInfo.InvariantCulture, $"{Level:0.#}@{Kelvin}"),
                _ => Level.ToString("0.#", CultureInfo.InvariantCulture)
            };
        }

        public static bool TryParseColor(string hex, out int r, out int g, out int b)
        {
            r = g = b = 0;
            if (string.IsNullOrEmpty(hex))
            {
                return false;
            }
            var s = hex.TrimStart('#');
            if (s.Length != 6 && s.Length != 8)
            {
                return false;
            }
            return int.TryParse(s.AsSpan(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out r)
                   && int.TryParse(s.AsSpan(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out g)
                   && int.TryParse(s.AsSpan(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out b);
        }

        public static string Format(double r, double g, double b)
        {
            static int C(double v) => (int)Math.Round(Math.Clamp(v, 0, 255), MidpointRounding.AwayFromZero);
            return $"#{C(r):X2}{C(g):X2}{C(b):X2}";
        }

        /// <summary>
        /// The color with the brightness applied
        /// </summary>
        public static string Scale(string color, double level)
        {
            if (!TryParseColor(color, out var r, out var g, out var b))
            {
                r = g = b = 255;
            }
            var f = Math.Clamp(level, 0, 100) / 100.0;
            return Format(r * f, g * f, b * f);
        }
    }
}
