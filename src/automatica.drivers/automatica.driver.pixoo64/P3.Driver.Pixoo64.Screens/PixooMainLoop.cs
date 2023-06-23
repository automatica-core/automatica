using Microsoft.Extensions.Logging;

namespace P3.Driver.Pixoo64.Screens
{
    public class PixooMainLoop
    {
        private readonly IList<PixooSharp.Pixoo64> _pixoo;
        private readonly ILogger _logger;
        private readonly List<BaseScreen> _screens = new List<BaseScreen>();

        private CancellationTokenSource _cancellationTokenSource = new();
        private bool _isRunning = false;

        public PixooMainLoop(IList<PixooSharp.Pixoo64> pixoo, ILogger logger)
        {
            _pixoo = pixoo;
            _logger = logger;
        }

        public void AddScreen(BaseScreen screen)
        {
            _screens.Add(screen);
            _logger.LogInformation($"{GetHashCode()} Add Screen {screen.GetType().Name}...");
        }

        public async Task Start()
        {
            _isRunning = true;
            _cancellationTokenSource = new CancellationTokenSource();
            await Task.CompletedTask;

            _logger.LogInformation($"{GetHashCode()} Loop started...");

            var task = Task.Run(function: async () =>
            {
                
                    int i = 0;
                    while (true)
                    {
                        try
                        {
                            if (_cancellationTokenSource.IsCancellationRequested || !_isRunning)
                            {
                                _logger.LogInformation(
                                    $"Cancellation requested... {_cancellationTokenSource.IsCancellationRequested} || {!_isRunning}");
                                break;
                            }

                            var screen = _screens[i];
                            screen.Start();

                            do
                            {
                                if (_cancellationTokenSource.IsCancellationRequested || !_isRunning)
                                {
                                    _logger.LogInformation(
                                        $"Cancellation requested... {_cancellationTokenSource.IsCancellationRequested} || {!_isRunning}");
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
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Exception occurred {ex}");
                            Thread.Sleep(2000);
                        }
                    }

            }, _cancellationTokenSource.Token).ConfigureAwait(false);


            _logger.LogInformation($"{GetHashCode()} Loop exited...");
        }

        public async Task Stop()
        {
            _screens.Clear();
            _logger.LogInformation($"{GetHashCode()} Stop loop...");
            _isRunning = false;
            await Task.CompletedTask;
            _cancellationTokenSource.Cancel();
        }
    }
}
