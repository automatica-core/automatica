using Automatica.Core.Automation.Definition;
using Automatica.Core.Control.Base;
using Microsoft.Extensions.Logging;

namespace Automatica.Core.Automation.Engine
{
    /// <summary>
    /// Calls <c>changed</c> whenever something a condition depends on may have changed: a datapoint value, a control state,
    /// or (for time and sun conditions) every <see cref="PollInterval"/>. Calls are serialized.
    /// </summary>
    internal sealed class ConditionWatcher : IAsyncDisposable
    {
        internal static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(15);

        private readonly IAutomationEnvironment _environment;
        private readonly ConditionDefinition _condition;
        private readonly Func<Task> _changed;
        private readonly ILogger _logger;
        private readonly SemaphoreSlim _lock = new(1, 1);
        private readonly List<IDisposable> _subscriptions = new();
        private readonly List<(IControlValueCallback Control, Guid Id)> _controlCallbacks = new();
        private ITimer _poll;
        private volatile bool _disposed;

        public ConditionWatcher(IAutomationEnvironment environment, ConditionDefinition condition, Func<Task> changed, ILogger logger)
        {
            _environment = environment;
            _condition = condition;
            _changed = changed;
            _logger = logger;
        }

        public async Task StartAsync()
        {
            var nodes = new HashSet<Guid>();
            var controls = new HashSet<Guid>();
            var timeBased = false;
            ConditionEvaluator.CollectNodes(_condition, nodes, controls, ref timeBased);

            foreach (var node in nodes)
            {
                _subscriptions.Add(await _environment.SubscribeAsync(node, _ => NotifyAsync()));
            }

            foreach (var controlId in controls)
            {
                if (_environment.GetControl(controlId) is IControlValueCallback callback)
                {
                    var id = callback.RegisterValueCallback(() => _ = NotifyAsync());
                    _controlCallbacks.Add((callback, id));
                }
            }

            if (timeBased)
            {
                _poll = _environment.Time.CreateTimer(_ => _ = NotifyAsync(), null, PollInterval, PollInterval);
            }
        }

        public async Task NotifyAsync()
        {
            if (_disposed)
            {
                return;
            }

            await _lock.WaitAsync().ConfigureAwait(false);
            try
            {
                if (!_disposed)
                {
                    await _changed().ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error evaluating automation condition");
            }
            finally
            {
                _lock.Release();
            }
        }

        public async ValueTask DisposeAsync()
        {
            _disposed = true;
            if (_poll != null)
            {
                await _poll.DisposeAsync();
            }

            foreach (var subscription in _subscriptions)
            {
                subscription.Dispose();
            }
            _subscriptions.Clear();

            foreach (var (control, id) in _controlCallbacks)
            {
                control.UnregisterValueCallback(id);
            }
            _controlCallbacks.Clear();
        }
    }
}
