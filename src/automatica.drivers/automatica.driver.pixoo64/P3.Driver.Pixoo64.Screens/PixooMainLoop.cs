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

        public Task Start()
        {
            _isRunning = true;
            _cancellationTokenSource = new CancellationTokenSource();
            // capture the token, a later Start() must not revive this loop
            var token = _cancellationTokenSource.Token;

            _logger.LogInformation($"{GetHashCode()} Loop started...");

            _ = Task.Run(function: async () =>
            {

                int i = 0;
                while (true)
                {
                    try
                    {
                        if (token.IsCancellationRequested || !_isRunning)
                        {
                            _logger.LogInformation(
                                $"Cancellation requested... {token.IsCancellationRequested} || {!_isRunning}");
                            _screens.Clear();
                            i = 0;
                            return;
                        }

                        if (_screens.Count > i)
                        {
                            var screen = _screens[i];
                            screen.Start();

                            do
                            {
                                if (token.IsCancellationRequested || !_isRunning)
                                {
                                    _logger.LogInformation(
                                        $"Cancellation requested... {token.IsCancellationRequested} || {!_isRunning}");
                                    _screens.Clear();
                                    i = 0;
                                    return;
                                }

                                await screen.Paint();
                                await Task.Delay(1000, token);
                            } while (screen.TimeForNextScreen > 0);
                        }
                        else
                        {
                            i = 0;
                            // no screens configured (yet), do not spin
                            await Task.Delay(1000, token);
                        }


                        i++;

                        if (i >= _screens.Count)
                        {
                            i = 0;
                        }
                    }
                    catch (OperationCanceledException) when (token.IsCancellationRequested)
                    {
                        _logger.LogInformation($"{GetHashCode()} Loop cancelled...");
                        return;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Exception occurred {ex}");
                        try
                        {
                            await Task.Delay(2000, token);
                        }
                        catch (OperationCanceledException)
                        {
                            return;
                        }
                    }
                }

            }).ConfigureAwait(false);


            _logger.LogInformation($"{GetHashCode()} Loop exited...");
            return Task.CompletedTask;
        }

        public Task Stop()
        {
            _screens.Clear();
            _logger.LogInformation($"{GetHashCode()} Stop loop...");
            _isRunning = false;
            _cancellationTokenSource.Cancel();
            return Task.CompletedTask;
        }
    }
}
