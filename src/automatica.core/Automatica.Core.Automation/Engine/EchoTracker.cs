using System.Collections.Concurrent;

namespace Automatica.Core.Automation.Engine
{
    /// <summary>
    /// Remembers what an automation wrote, so that the value coming back from the device does not trigger the same
    /// automation again.
    /// </summary>
    internal sealed class EchoTracker
    {
        internal static readonly TimeSpan Window = TimeSpan.FromSeconds(5);

        private readonly TimeProvider _time;
        private readonly ConcurrentDictionary<(Guid Automation, Guid Target), (object Value, DateTimeOffset At)> _writes = new();

        public EchoTracker(TimeProvider time)
        {
            _time = time;
        }

        public void Record(Guid automationId, Guid target, object value)
        {
            _writes[(automationId, target)] = (AutomationValues.ToPlain(value), _time.GetUtcNow());
        }

        public bool IsEcho(Guid automationId, Guid target, object value)
        {
            if (!_writes.TryGetValue((automationId, target), out var write))
            {
                return false;
            }

            if (_time.GetUtcNow() - write.At > Window)
            {
                _writes.TryRemove((automationId, target), out _);
                return false;
            }

            return AutomationValues.AreEqual(write.Value, value);
        }

        public void Forget(Guid automationId)
        {
            foreach (var key in _writes.Keys.Where(a => a.Automation == automationId).ToList())
            {
                _writes.TryRemove(key, out _);
            }
        }
    }
}
