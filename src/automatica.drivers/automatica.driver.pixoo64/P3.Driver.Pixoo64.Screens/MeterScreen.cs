using System.Globalization;
using P3.PixooSharp.Assets;

namespace P3.Driver.Pixoo64.Screens
{
    public class MeterScreen : BaseScreen
    {
        public double? Meter1 { get; set; }
        public double? Meter2 { get; set; }
        public double? Meter3 { get; set; }
        public double? Meter4 { get; set; }
        public double? Meter5 { get; set; }
        public double? Meter6 { get; set; }


        public string Meter1Name { get; set; }
        public string Meter2Name { get; set; }
        public string Meter3Name { get; set; }
        public string Meter4Name { get; set; }
        public string Meter5Name { get; set; }
        public string Meter6Name { get; set; }

        public MeterScreen(IList<PixooSharp.Pixoo64> pixoo) : base(pixoo)
        {
            Meter1Name = "M1";
            Meter2Name = "M2";
            Meter3Name = "M3";
            Meter4Name = "M4";
            Meter5Name = "M5";
            Meter6Name = "M6";

            Title = "Meter Info";
        }
        protected override async Task PaintInternal()
        {
            await Task.CompletedTask;

            foreach (var pixoo in Pixoos)
            {

                pixoo.DrawText(5, 5, Palette.Green, Title);

                int meterPos = 12;
                if (Meter1.HasValue)
                {
                    pixoo.DrawText(5, meterPos, Palette.White, $"{Meter1Name}");
                    pixoo.DrawText(30, meterPos, Meter1 < 0 ? Palette.Green : Palette.White,
                        Meter1.Value.ToString("0", CultureInfo.InvariantCulture));

                    meterPos += 8;
                }

                if (Meter2.HasValue)
                {
                    pixoo.DrawText(5, meterPos, Palette.White, $"{Meter2Name}");
                    pixoo.DrawText(30, meterPos, Meter2 < 0 ? Palette.Green : Palette.White,
                        Meter2.Value.ToString("0", CultureInfo.InvariantCulture));
                    meterPos += 8;
                }

                if (Meter3.HasValue)
                {
                    pixoo.DrawText(5, meterPos, Palette.White, $"{Meter3Name}");
                    pixoo.DrawText(30, meterPos, Meter3 < 0 ? Palette.Green : Palette.White,
                        Meter3.Value.ToString("0", CultureInfo.InvariantCulture));
                    meterPos += 8;
                }

                if (Meter4.HasValue)
                {
                    pixoo.DrawText(5, meterPos, Palette.White, $"{Meter4Name}");
                    pixoo.DrawText(30, meterPos, Meter4 < 0 ? Palette.Green : Palette.White,
                        Meter4.Value.ToString("0", CultureInfo.InvariantCulture));
                    meterPos += 8;
                }

                if (Meter5.HasValue)
                {
                    pixoo.DrawText(5, meterPos, Palette.White, $"{Meter5Name}");
                    pixoo.DrawText(30, meterPos, Meter5 < 0 ? Palette.Green : Palette.White,
                        Meter5.Value.ToString("0", CultureInfo.InvariantCulture));
                    meterPos += 8;
                }

                if (Meter6.HasValue)
                {
                    pixoo.DrawText(5, meterPos, Palette.White, $"{Meter6Name}");
                    pixoo.DrawText(30, meterPos, Meter5 < 0 ? Palette.Green : Palette.White,
                        Meter6.Value.ToString("0", CultureInfo.InvariantCulture));
                }
            }
        }


    }
}
