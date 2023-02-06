using P3.PixooSharp.Assets;

namespace P3.Driver.Pixoo64.Screens
{
    public abstract class BaseScreen
    {
        public string Title { get; set; }
        public IList<PixooSharp.Pixoo64> Pixoos { get; } 

        public int TimeForNextScreen { get; private set; }

        public int ScreenTime { get; set; } = 10;

        protected bool ShowFrame { get; set; } = true;
        protected bool ShowClock { get; set; } = true;
        protected bool ShowTicksUntilNextFrame { get; set; } = true;
        protected Rgb FrameColor { get; set; } = Palette.White;
        protected Rgb BackgroundColor { get; set; } = Palette.Black;

        protected Rgb ColorText { get; set; } = Palette.Green;

        public int DateTimeHourOffset = 0;

        protected BaseScreen(IList<PixooSharp.Pixoo64> pixoo)
        {
            Pixoos = pixoo;
            Title = "Base";
        }

        protected virtual void Init()
        {

        }

        public void Start()
        {
            Init();
            TimeForNextScreen = ScreenTime;
        }

        public async Task Paint()
        {
            foreach (var pixoo in Pixoos)
            {
                pixoo.Clear();
                await pixoo.SendClearTextAsync();

                pixoo.DrawFilledRectangle(0, 0, 63, 63, BackgroundColor);
                if (ShowFrame)
                {
                    pixoo.DrawLine(0, 0, 63, 0, FrameColor);
                    pixoo.DrawLine(63, 0, 63, 63, FrameColor);
                    pixoo.DrawLine(63, 63, 0, 63, FrameColor);
                    pixoo.DrawLine(0, 63, 0, 0, FrameColor);
                }

                if (ShowTicksUntilNextFrame && TimeForNextScreen.ToString().Length == 1)
                {
                    pixoo.DrawText(59, 2, Palette.White, TimeForNextScreen.ToString());
                }

                TimeForNextScreen--;
                await PaintInternal();


                if (ShowClock)
                {
                    pixoo.DrawText(43, 57, ColorText, $"{DateTime.Now.AddHours(DateTimeHourOffset):HH:mm}");
                }

                await pixoo.SendResetGif();
                await pixoo.SendBufferAsync(0);
            }
        }

        protected abstract Task PaintInternal();
    }
}
