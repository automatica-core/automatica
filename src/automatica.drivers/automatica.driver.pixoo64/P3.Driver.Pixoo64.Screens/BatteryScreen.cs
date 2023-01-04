using System.Globalization;
using P3.PixooSharp.Assets;

namespace P3.Driver.Pixoo64.Screens
{
    public class BatteryScreen : BaseScreen
    {
        public double V1 { get; set; }
        public double V2 { get; set; }
        public double V3 { get; set; }
        public double V4 { get; set; }
        public double A1 { get; set; }
        public double A2 { get; set; }
        public double A3 { get; set; }
        public double A4 { get; set; }
        public int Soc1 { get; set; }
        public int Soc2 { get; set; }
        public int Soc3 { get; set; }
        public int Soc4 { get; set; }

        public BatteryScreen(PixooSharp.Pixoo64 Pixoo) : base(Pixoo)
        {
        }

        protected override async Task PaintInternal()
        {
            await Task.CompletedTask;
            Pixoo.DrawText(5, 5, Palette.Green, "Battery Info");

            Pixoo.DrawText(5, 12, Palette.White, "V");
            Pixoo.DrawText(10, 12, Palette.White, V1.ToString(CultureInfo.InvariantCulture));
            Pixoo.DrawText(30, 12, Palette.White, V2.ToString(CultureInfo.InvariantCulture));
            Pixoo.DrawText(5, 20, Palette.White, "V");
            Pixoo.DrawText(10, 20, Palette.White, V3.ToString(CultureInfo.InvariantCulture));
            Pixoo.DrawText(30, 20, Palette.White, V4.ToString(CultureInfo.InvariantCulture));

            Pixoo.DrawText(5, 28, Palette.White, "A");
            Pixoo.DrawText(10, 28, GetAmpereColor(A1), A1.ToString(CultureInfo.InvariantCulture));
            Pixoo.DrawText(30, 28, GetAmpereColor(A2), A2.ToString(CultureInfo.InvariantCulture));
            Pixoo.DrawText(5, 36, Palette.White, "A");
            Pixoo.DrawText(10, 36, GetAmpereColor(A3), A3.ToString(CultureInfo.InvariantCulture));
            Pixoo.DrawText(30, 36, GetAmpereColor(A4), A4.ToString(CultureInfo.InvariantCulture));

            Pixoo.DrawText(5, 44, Palette.White, "%");
            Pixoo.DrawText(10, 44, GetSocColor(Soc1), Soc1.ToString(CultureInfo.InvariantCulture));
            Pixoo.DrawText(30, 44, GetSocColor(Soc2), Soc2.ToString(CultureInfo.InvariantCulture));
            Pixoo.DrawText(5, 52, Palette.White, "%");
            Pixoo.DrawText(10, 52, GetSocColor(Soc3), Soc3.ToString(CultureInfo.InvariantCulture));
            Pixoo.DrawText(30, 52, GetSocColor(Soc4), Soc4.ToString(CultureInfo.InvariantCulture));

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
