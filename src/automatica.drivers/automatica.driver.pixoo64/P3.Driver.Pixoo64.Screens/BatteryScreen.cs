using System.Globalization;
using Microsoft.Extensions.Logging;
using P3.PixooSharp.Assets;

namespace P3.Driver.Pixoo64.Screens
{
    public class BatteryScreen : BaseScreen
    {

        public double? V1 { get; set; }
        public double? V2 { get; set; }
        public double? V3 { get; set; }
        public double? V4 { get; set; }
        public double? A1 { get; set; }
        public double? A2 { get; set; }
        public double? A3 { get; set; }
        public double? A4 { get; set; }
        public int? Soc1 { get; set; }
        public int? Soc2 { get; set; }
        public int? Soc3 { get; set; }
        public int? Soc4 { get; set; }

        public BatteryScreen(IList<PixooSharp.Pixoo64> pixoo, ILogger logger) : base(pixoo, logger)
        {
            Title = "Battery Info";
        }

        protected override async Task PaintInternal(PixooSharp.Pixoo64 pixoo)
        {
            await Task.CompletedTask;

            pixoo.DrawText(5, 5, Palette.Green, Title);

            if (V1.HasValue || V2.HasValue)
                pixoo.DrawText(5, 12, Palette.White, "V");
            if (V1.HasValue)
                pixoo.DrawText(10, 12, Palette.White, V1.Value.ToString("0.00", CultureInfo.InvariantCulture));
            if (V2.HasValue)
                pixoo.DrawText(35, 12, Palette.White, V2.Value.ToString("0.00", CultureInfo.InvariantCulture));
            if (V3.HasValue || V4.HasValue)
                pixoo.DrawText(5, 20, Palette.White, "V");
            if (V3.HasValue)
                pixoo.DrawText(10, 20, Palette.White, V3.Value.ToString("0.00", CultureInfo.InvariantCulture));
            if (V4.HasValue)
                pixoo.DrawText(35, 20, Palette.White, V4.Value.ToString("0.00", CultureInfo.InvariantCulture));

            if (A1.HasValue || A2.HasValue)
                pixoo.DrawText(5, 28, Palette.White, "A");
            if (A1.HasValue)
                pixoo.DrawText(10, 28, GetAmpereColor(A1.Value),
                    A1.Value.ToString("0.00", CultureInfo.InvariantCulture));
            if (A2.HasValue)
                pixoo.DrawText(35, 28, GetAmpereColor(A2.Value),
                    A2.Value.ToString("0.00", CultureInfo.InvariantCulture));
            if (A3.HasValue || A4.HasValue)
                pixoo.DrawText(5, 36, Palette.White, "A");
            if (A3.HasValue)
                pixoo.DrawText(10, 36, GetAmpereColor(A3.Value),
                    A3.Value.ToString("0.00", CultureInfo.InvariantCulture));
            if (A4.HasValue)
                pixoo.DrawText(35, 36, GetAmpereColor(A4.Value),
                    A4.Value.ToString("0.00", CultureInfo.InvariantCulture));

            if (Soc1.HasValue || Soc2.HasValue)
                pixoo.DrawText(5, 44, Palette.White, "%");
            if (Soc1.HasValue)
                pixoo.DrawText(10, 44, GetSocColor(Soc1.Value), Soc1.Value.ToString(CultureInfo.InvariantCulture));
            if (Soc2.HasValue)
                pixoo.DrawText(35, 44, GetSocColor(Soc2.Value), Soc2.Value.ToString(CultureInfo.InvariantCulture));

            if (Soc3.HasValue || Soc4.HasValue)
                pixoo.DrawText(5, 52, Palette.White, "%");
            if (Soc3.HasValue)
                pixoo.DrawText(10, 52, GetSocColor(Soc3.Value), Soc3.Value.ToString(CultureInfo.InvariantCulture));
            if (Soc4.HasValue)
                pixoo.DrawText(35, 52, GetSocColor(Soc4.Value), Soc4.Value.ToString(CultureInfo.InvariantCulture));

        }

        private static Rgb GetAmpereColor(double value)
        {
            return value > 0 ? Palette.Green : (value == 0 ? Palette.White : Palette.Red);
        }
        private static Rgb GetSocColor(double value)
        {
            switch (value)
            {
                case var n when (n >= 80):
                    return Palette.Green;

                case var n when (n < 80 && n >= 40):
                    return Palette.Yellow;

                case var n when (n < 40):
                    return Palette.Red;
            }

            return Palette.White;
        }
    }
}
