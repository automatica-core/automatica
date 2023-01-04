using Microsoft.Extensions.Logging;

namespace P3.Driver.Pixoo64.Screens
{
    public class PixooMainLoop
    {
        private readonly PixooSharp.Pixoo64 _pixoo;
        private readonly ILogger _logger;
        private readonly List<BaseScreen> _screens = new List<BaseScreen>();

        private CancellationTokenSource _cancellationTokenSource = new();

        public PixooMainLoop(PixooSharp.Pixoo64 pixoo, ILogger logger)
        {
            _pixoo = pixoo;
            _logger = logger;
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
                try
                {
                    int i = 0;
                    while (true)
                    {
                        if (_cancellationTokenSource.IsCancellationRequested)
                        {
                            break;
                        }

                        var screen = _screens[i];
                        screen.Start();

                        do
                        {
                            if (_cancellationTokenSource.IsCancellationRequested)
                            {
                                break;
                            }

                            await screen.Paint();
                            Thread.Sleep(1000);
                        } while (screen.TimeForNextScreen > 0);


                        i++;

                        if (i >= _screens.Count)
                        {
                            i = 0;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Exception occured {ex}");
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
