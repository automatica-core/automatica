using P3.PixooSharp.Assets;

namespace P3.Driver.Pixoo64.Screens
{
    public class InfoScreen : BaseScreen
    {
       
        public double? Outside { get; set; }
        public double? Inside { get; set; }

        public InfoScreen(PixooSharp.Pixoo64 Pixoo) : base(Pixoo)
        {
            Title = "Infos";
        }

        protected override void Init()
        {
            ShowFrame = false;
            base.Init();
        }

        protected override async Task PaintInternal()
        {
            await Task.CompletedTask;


            Pixoo.DrawText(5, 5, Palette.Green, Title);

            if(Outside.HasValue) 
                Pixoo.DrawText(5, 12, Palette.White, $"Out: {Outside}°C");
            if(Inside.HasValue)
                Pixoo.DrawText(5, 22, Palette.White, $"In:  {Inside}°C");
            Pixoo.DrawText(5, 32, Palette.White, $"{DateTime.Now:dd.MM.yyyy}");
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
