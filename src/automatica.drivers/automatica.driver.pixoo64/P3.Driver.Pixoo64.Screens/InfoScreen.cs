using Microsoft.Extensions.Logging;
using P3.PixooSharp.Assets;

namespace P3.Driver.Pixoo64.Screens
{
    public class InfoScreen : BaseScreen
    {
        public double? Outside { get; set; }
        public double? Inside { get; set; }
        public string? DoorLockState { get; set; }

        public InfoScreen(IList<PixooSharp.Pixoo64> pixoo, ILogger logger) : base(pixoo, logger)
        {
            Title = "Infos";
        }

        protected override void Init()
        {
            ShowClock = false;
            base.Init();
        }

        protected override async Task PaintInternal(PixooSharp.Pixoo64 pixoo)
        {
            await Task.CompletedTask;


            pixoo.DrawText(5, 5, Palette.Green, Title);

            if (Outside.HasValue)
                pixoo.DrawText(5, 12, Palette.White, $"Out: {Math.Round(Outside.Value, 2)}°C");
            if (Inside.HasValue)
                pixoo.DrawText(5, 22, Palette.White, $"In:  {Math.Round(Inside.Value, 2)}°C");
            if (DoorLockState != null)
                pixoo.DrawText(5, 52, Palette.White, $"Door:  {DoorLockState}");

            pixoo.DrawText(5, 32, Palette.White, $"{DateTime.Now.AddHours(DateTimeHourOffset):HH:mm}");
            pixoo.DrawText(5, 42, Palette.White, $"{DateTime.Now.AddHours(DateTimeHourOffset):dd.MM.yyyy}");

        }
    }
}
