using P3.Driver.Matter.Fabrics;
using P3.Driver.Matter.InteractionModel;
using P3.Driver.Matter.Tlv;

namespace P3.Driver.Matter.DataModel.Clusters
{
    public enum ColorMode : byte
    {
        HueSaturation = 0,
        Xy = 1,
        ColorTemperature = 2
    }

    /// <summary>
    /// Color Control (0x0300) with color temperature and, for extended color lights, hue/saturation and XY.
    /// Transitions are not simulated: every move/step command jumps to its target.
    /// </summary>
    public sealed class ColorControlCluster : ClusterServer
    {
        public const uint Id = 0x0300;
        public const uint AttributeCurrentHue = 0x0000;
        public const uint AttributeCurrentSaturation = 0x0001;
        public const uint AttributeCurrentX = 0x0003;
        public const uint AttributeCurrentY = 0x0004;
        public const uint AttributeColorTemperature = 0x0007;
        public const uint AttributeColorMode = 0x0008;
        public const uint AttributeEnhancedColorMode = 0x4001;

        public const ushort MinMireds = 153; // 6500 K
        public const ushort MaxMireds = 500; // 2000 K

        private const uint FeatureHueSaturation = 0x01;
        private const uint FeatureXy = 0x08;
        private const uint FeatureColorTemperature = 0x10;

        private readonly bool _color;
        private byte _hue;
        private byte _saturation;
        private ushort _x = 20495; // D65 white
        private ushort _y = 21561;
        private ushort _mireds = 370;
        private ColorMode _mode;
        private byte _options;
        private ushort? _startUpMireds;

        /// <param name="color">true for hue/saturation and XY besides color temperature (extended color light).</param>
        public ColorControlCluster(bool color) : base(Id, 6)
        {
            _color = color;
            _mode = color ? ColorMode.HueSaturation : ColorMode.ColorTemperature;
            FeatureMap = FeatureColorTemperature | (color ? FeatureHueSaturation | FeatureXy : 0);

            if (color)
            {
                Attribute(AttributeCurrentHue, (w, t, _) => w.PutUInt(t, _hue));
                Attribute(AttributeCurrentSaturation, (w, t, _) => w.PutUInt(t, _saturation));
                Attribute(AttributeCurrentX, (w, t, _) => w.PutUInt(t, _x));
                Attribute(AttributeCurrentY, (w, t, _) => w.PutUInt(t, _y));
            }

            Attribute(0x0002, (w, t, _) => w.PutUInt(t, 0)); // RemainingTime
            Attribute(AttributeColorTemperature, (w, t, _) => w.PutUInt(t, _mireds));
            Attribute(AttributeColorMode, (w, t, _) => w.PutUInt(t, (byte)_mode));
            Attribute(0x000F, (w, t, _) => w.PutUInt(t, _options), (v, _, _) => _options = (byte)(v.AsUInt() & 0x01));
            Attribute(0x0010, (w, t, _) => w.PutUInt(t, 0)); // NumberOfPrimaries
            Attribute(AttributeEnhancedColorMode, (w, t, _) => w.PutUInt(t, (byte)_mode));
            Attribute(0x400A, (w, t, _) => w.PutUInt(t, FeatureMap)); // ColorCapabilities
            Attribute(0x400B, (w, t, _) => w.PutUInt(t, MinMireds));
            Attribute(0x400C, (w, t, _) => w.PutUInt(t, MaxMireds));
            Attribute(0x400D, (w, t, _) => w.PutUInt(t, MinMireds));
            Attribute(0x4010, (w, t, _) => w.PutNullableUInt(t, _startUpMireds), (v, _, _) =>
            {
                _startUpMireds = v.IsNull ? null : (ushort)Math.Clamp(v.AsUInt(), MinMireds, MaxMireds);
                MarkChanged(0x4010);
            }, writePrivilege: AclPrivilege.Manage);

            if (color)
            {
                Command(0x00, (_, f) => SetHueSaturation((byte)Math.Min(f.GetUInt(0), 254), null)); // MoveToHue
                Command(0x01, (_, _) => InvokeResult.SuccessTask()); // MoveHue
                Command(0x02, (_, f) => SetHueSaturation(StepByte(_hue, f.GetUInt(0) == 1, f.GetUInt(1), true), null)); // StepHue
                Command(0x03, (_, f) => SetHueSaturation(null, (byte)Math.Min(f.GetUInt(0), 254))); // MoveToSaturation
                Command(0x04, (_, _) => InvokeResult.SuccessTask()); // MoveSaturation
                Command(0x05, (_, f) => SetHueSaturation(null, StepByte(_saturation, f.GetUInt(0) == 1, f.GetUInt(1), false))); // StepSaturation
                Command(0x06, (_, f) => SetHueSaturation((byte)Math.Min(f.GetUInt(0), 254), (byte)Math.Min(f.GetUInt(1), 254))); // MoveToHueAndSaturation
                Command(0x07, (_, f) => SetXy((ushort)Math.Min(f.GetUInt(0), 0xFEFF), (ushort)Math.Min(f.GetUInt(1), 0xFEFF))); // MoveToColor
                Command(0x08, (_, _) => InvokeResult.SuccessTask()); // MoveColor
                Command(0x09, (_, f) => SetXy((ushort)Math.Clamp(_x + f.GetInt(0), 0, 0xFEFF), (ushort)Math.Clamp(_y + f.GetInt(1), 0, 0xFEFF))); // StepColor
            }

            Command(0x0A, (_, f) => SetMireds((ushort)Math.Clamp(f.GetUInt(0), MinMireds, MaxMireds))); // MoveToColorTemperature
            Command(0x47, (_, _) => InvokeResult.SuccessTask()); // StopMoveStep
            Command(0x4B, (_, _) => InvokeResult.SuccessTask()); // MoveColorTemperature
            Command(0x4C, (_, f) =>
            {
                var up = f.GetUInt(0) == 1;
                var step = (long)f.GetUInt(1);
                var min = Math.Max(MinMireds, (ushort)(f.GetUIntOrNull(3) ?? MinMireds));
                var max = Math.Min(MaxMireds, f.GetUIntOrNull(4) is { } m and > 0 ? (ushort)m : MaxMireds);
                return SetMireds((ushort)Math.Clamp(_mireds + (up ? step : -step), min, max));
            }); // StepColorTemperature
        }

        /// <summary>Raised when a controller sets a color: RGB as #RRGGBB.</summary>
        public Func<string, Task> ColorRequested { get; set; }

        /// <summary>Raised when a controller sets a color temperature in Kelvin.</summary>
        public Func<int, Task> ColorTemperatureRequested { get; set; }

        public ColorMode Mode => _mode;
        public int ColorTemperatureKelvin => MiredsToKelvin(_mireds);
        public string Rgb => _mode == ColorMode.Xy ? ColorConversion.XyToRgb(_x / 65536.0, _y / 65536.0) : ColorConversion.HsToRgb(_hue * 360.0 / 254, _saturation / 254.0);

        private static byte StepByte(byte value, bool up, ulong step, bool wrap)
        {
            var v = (long)value + (up ? (long)step : -(long)step);
            if (wrap)
            {
                return (byte)(((v % 255) + 255) % 255);
            }

            return (byte)Math.Clamp(v, 0, 254);
        }

        private void SetMode(ColorMode mode)
        {
            if (mode != _mode)
            {
                _mode = mode;
                MarkChanged(AttributeColorMode);
                MarkChanged(AttributeEnhancedColorMode);
            }
        }

        private async Task<InvokeResult> SetHueSaturation(byte? hue, byte? saturation)
        {
            ApplyHueSaturation(hue ?? _hue, saturation ?? _saturation);
            SetMode(ColorMode.HueSaturation);
            if (ColorRequested != null)
            {
                await ColorRequested(Rgb);
            }

            return InvokeResult.Success();
        }

        private async Task<InvokeResult> SetXy(ushort x, ushort y)
        {
            ApplyXy(x, y);
            SetMode(ColorMode.Xy);
            if (ColorRequested != null)
            {
                await ColorRequested(Rgb);
            }

            return InvokeResult.Success();
        }

        private async Task<InvokeResult> SetMireds(ushort mireds)
        {
            ApplyMireds(mireds);
            SetMode(ColorMode.ColorTemperature);
            if (ColorTemperatureRequested != null)
            {
                await ColorTemperatureRequested(MiredsToKelvin(mireds));
            }

            return InvokeResult.Success();
        }

        private void ApplyHueSaturation(byte hue, byte saturation)
        {
            if (hue != _hue)
            {
                _hue = hue;
                MarkChanged(AttributeCurrentHue);
            }

            if (saturation != _saturation)
            {
                _saturation = saturation;
                MarkChanged(AttributeCurrentSaturation);
            }
        }

        private void ApplyXy(ushort x, ushort y)
        {
            if (x != _x)
            {
                _x = x;
                MarkChanged(AttributeCurrentX);
            }

            if (y != _y)
            {
                _y = y;
                MarkChanged(AttributeCurrentY);
            }
        }

        private void ApplyMireds(ushort mireds)
        {
            if (mireds != _mireds)
            {
                _mireds = mireds;
                MarkChanged(AttributeColorTemperature);
            }
        }

        public static int MiredsToKelvin(ushort mireds) => (int)Math.Round(1_000_000.0 / Math.Max((ushort)1, mireds));

        public static ushort KelvinToMireds(int kelvin) => (ushort)Math.Clamp(Math.Round(1_000_000.0 / Math.Max(1, kelvin)), MinMireds, MaxMireds);

        /// <summary>Updates the color from the device (#RRGGBB or RRGGBB).</summary>
        public void SetRgb(string rgb)
        {
            if (!_color || !ColorConversion.TryParseRgb(rgb, out var r, out var g, out var b))
            {
                return;
            }

            var (hue, saturation, _) = ColorConversion.RgbToHsv(r, g, b);
            var (x, y) = ColorConversion.RgbToXy(r, g, b);
            ApplyHueSaturation((byte)Math.Clamp(Math.Round(hue * 254 / 360), 0, 254), (byte)Math.Clamp(Math.Round(saturation * 254), 0, 254));
            ApplyXy((ushort)Math.Clamp(Math.Round(x * 65536), 0, 0xFEFF), (ushort)Math.Clamp(Math.Round(y * 65536), 0, 0xFEFF));
            SetMode(ColorMode.HueSaturation);
        }

        /// <summary>Updates the color temperature from the device (Kelvin).</summary>
        public void SetColorTemperature(int kelvin)
        {
            ApplyMireds(KelvinToMireds(kelvin));
            SetMode(ColorMode.ColorTemperature);
        }
    }

    public static class ColorConversion
    {
        public static bool TryParseRgb(string value, out double r, out double g, out double b)
        {
            r = g = b = 0;
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            var s = value.Trim().TrimStart('#');
            if (s.Length != 6 || !int.TryParse(s, System.Globalization.NumberStyles.HexNumber, null, out var rgb))
            {
                return false;
            }

            r = ((rgb >> 16) & 0xFF) / 255.0;
            g = ((rgb >> 8) & 0xFF) / 255.0;
            b = (rgb & 0xFF) / 255.0;
            return true;
        }

        public static string ToHex(double r, double g, double b)
        {
            static int C(double v) => (int)Math.Clamp(Math.Round(v * 255), 0, 255);
            return $"#{C(r):X2}{C(g):X2}{C(b):X2}";
        }

        /// <summary>Hue in degrees, saturation and value 0..1.</summary>
        public static (double Hue, double Saturation, double Value) RgbToHsv(double r, double g, double b)
        {
            var max = Math.Max(r, Math.Max(g, b));
            var min = Math.Min(r, Math.Min(g, b));
            var delta = max - min;
            double hue = 0;
            if (delta > 0)
            {
                if (max == r)
                {
                    hue = 60 * (((g - b) / delta) % 6);
                }
                else if (max == g)
                {
                    hue = 60 * ((b - r) / delta + 2);
                }
                else
                {
                    hue = 60 * ((r - g) / delta + 4);
                }
            }

            if (hue < 0)
            {
                hue += 360;
            }

            return (hue, max == 0 ? 0 : delta / max, max);
        }

        public static string HsToRgb(double hue, double saturation)
        {
            hue = ((hue % 360) + 360) % 360;
            var c = saturation;
            var x = c * (1 - Math.Abs(hue / 60 % 2 - 1));
            var m = 1 - c;
            var (r, g, b) = hue switch
            {
                < 60 => (c, x, 0.0),
                < 120 => (x, c, 0.0),
                < 180 => (0.0, c, x),
                < 240 => (0.0, x, c),
                < 300 => (x, 0.0, c),
                _ => (c, 0.0, x)
            };
            return ToHex(r + m, g + m, b + m);
        }

        private static double ToLinear(double c) => c <= 0.04045 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);

        private static double FromLinear(double c) => c <= 0.0031308 ? 12.92 * c : 1.055 * Math.Pow(c, 1 / 2.4) - 0.055;

        /// <summary>sRGB (0..1) to CIE 1931 xy.</summary>
        public static (double X, double Y) RgbToXy(double r, double g, double b)
        {
            r = ToLinear(r);
            g = ToLinear(g);
            b = ToLinear(b);
            var x = r * 0.4124 + g * 0.3576 + b * 0.1805;
            var y = r * 0.2126 + g * 0.7152 + b * 0.0722;
            var z = r * 0.0193 + g * 0.1192 + b * 0.9505;
            var sum = x + y + z;
            return sum <= 0 ? (0.3127, 0.3290) : (x / sum, y / sum);
        }

        /// <summary>CIE 1931 xy to sRGB at full brightness.</summary>
        public static string XyToRgb(double x, double y)
        {
            if (y <= 0)
            {
                return "#FFFFFF";
            }

            var bigY = 1.0;
            var bigX = bigY / y * x;
            var bigZ = bigY / y * (1 - x - y);
            var r = bigX * 3.2406 - bigY * 1.5372 - bigZ * 0.4986;
            var g = -bigX * 0.9689 + bigY * 1.8758 + bigZ * 0.0415;
            var b = bigX * 0.0557 - bigY * 0.2040 + bigZ * 1.0570;
            r = Math.Max(0, r);
            g = Math.Max(0, g);
            b = Math.Max(0, b);
            var max = Math.Max(r, Math.Max(g, b));
            if (max > 1)
            {
                r /= max;
                g /= max;
                b /= max;
            }

            return ToHex(FromLinear(r), FromLinear(g), FromLinear(b));
        }
    }
}
