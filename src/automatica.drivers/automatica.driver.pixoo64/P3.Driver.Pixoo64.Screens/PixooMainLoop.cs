namespace P3.Driver.Pixoo64.Screens
{
    public class PixooMainLoop
    {
        private readonly PixooSharp.Pixoo64 _pixoo;
        private readonly List<BaseScreen> _screens = new List<BaseScreen>();

        private CancellationTokenSource _cancellationTokenSource = new();

        public PixooMainLoop(PixooSharp.Pixoo64 pixoo)
        {
            _pixoo = pixoo;
        }

        public void AddScreen(BaseScreen screen)
        {
            _screens.Add(screen);
        }

        public async Task Start()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            await Task.CompletedTask;

            var task = Task.Run(function: async () =>
            {
                int i = 0;
                while (true)
                {
                    var screen = _screens[i];
                    screen.Start();

                    do
                    {
                        await screen.Paint();
                        Thread.Sleep(1000);
                    } while (screen.TimeForNextScreen > 0);


                    i++;

                    if (i >= _screens.Count)
                    {
                        i = 0;
                    }
                }
            }, _cancellationTokenSource.Token).ConfigureAwait(false);
        }

        public async Task Stop()
        {
            await Task.CompletedTask;
            _cancellationTokenSource.Cancel();
        }
    }
}
