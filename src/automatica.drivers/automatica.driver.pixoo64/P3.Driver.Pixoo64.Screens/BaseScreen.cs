using P3.PixooSharp.Assets;

namespace P3.Driver.Pixoo64.Screens
{
    public abstract class BaseScreen
    {
        public PixooSharp.Pixoo64 Pixoo { get; }

        public int TimeForNextScreen { get; private set; }

        public int ScreenTime { get; set; } = 10;

        protected bool ShowFrame { get; set; } = true;
        protected bool ShowClock { get; set; } = true;
        protected bool ShowTicksUntilNextFrame { get; set; } = true;
        protected Rgb FrameColor { get; set; } = Palette.White;
        protected Rgb BackgroundColor { get; set; } = Palette.Black;

        protected Rgb ColorText { get; set; } = Palette.White;

        protected BaseScreen(PixooSharp.Pixoo64 pixoo)
        {
            Pixoo = pixoo;
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
            Pixoo.Clear();
            await Pixoo.SendClearTextAsync();

            Pixoo.DrawFilledRectangle(0, 0, 63, 63, BackgroundColor);
            if (ShowFrame)
            {
                Pixoo.DrawLine(0, 0, 63, 0, FrameColor);
                Pixoo.DrawLine(63, 0, 63, 63, FrameColor);
                Pixoo.DrawLine(63, 63, 0, 63, FrameColor);
                Pixoo.DrawLine(0, 63, 0, 0, FrameColor);
            }

            if (ShowTicksUntilNextFrame && TimeForNextScreen.ToString().Length == 1)
            {
                Pixoo.DrawText(59, 2, ColorText, TimeForNextScreen.ToString());
            }
            
            TimeForNextScreen--;
            await PaintInternal();


            if (ShowClock)
            {
                Pixoo.DrawText(43, 57, ColorText, $"{DateTime.Now:HH:mm}");
            }

            await Pixoo.SendResetGif();
            await Pixoo.SendBufferAsync(0);
        }

        protected abstract Task PaintInternal();
    }
}
