using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;

namespace P3.Logic.Operations.Tests.Energy
{
    /// <summary>
    /// In-memory <see cref="IDispatcher"/> that records every dispatched value synchronously, in call order.
    /// The shared dispatcher of the test base delivers values asynchronously and keeps only the last one.
    /// </summary>
    internal sealed class RecordingDispatcher : IDispatcher
    {
        private readonly object _lock = new();
        private readonly List<(Guid Id, object Value)> _dispatched = new();
        private readonly Dictionary<Guid, DispatchValue> _values = new();

        public IReadOnlyList<object> ValuesOf(Guid id)
        {
            lock (_lock)
            {
                return _dispatched.Where(a => a.Id == id).Select(a => a.Value).ToArray();
            }
        }

        public object LastOf(Guid id)
        {
            var values = ValuesOf(id);
            return values.Count == 0 ? null : values[^1];
        }

        public Task Init(CancellationToken token = default)
        {
            return Task.CompletedTask;
        }

        public Task DispatchValue(IDispatchable self, object value, DispatchValueSource valueSource = DispatchValueSource.Read)
        {
            // fixed timestamp - the tests must not depend on the clock
            return DispatchValue(self, new DispatchValue(self.Id, self.Type, value, DateTime.UnixEpoch, valueSource));
        }

        public Task DispatchValue(IDispatchable self, DispatchValue value)
        {
            lock (_lock)
            {
                _dispatched.Add((self.Id, value.Value));
                _values[self.Id] = value;
            }
            return Task.CompletedTask;
        }

        public Task UnRegisterDispatch(DispatchableType type, Guid id)
        {
            return Task.CompletedTask;
        }

        public Task RegisterDispatch(DispatchableType type, Guid id, Action<IDispatchable, DispatchValue> callback)
        {
            return Task.CompletedTask;
        }

        public IDictionary<Guid, DispatchValue> GetValues()
        {
            lock (_lock)
            {
                return new Dictionary<Guid, DispatchValue>(_values);
            }
        }

        public IDictionary<Guid, DispatchValue> GetValues(DispatchableType type)
        {
            return GetValues();
        }

        public DispatchValue GetValue(Guid id)
        {
            lock (_lock)
            {
                return _values.TryGetValue(id, out var value) ? value : null;
            }
        }

        public DispatchValue GetValue(DispatchableType type, Guid id)
        {
            return GetValue(id);
        }

        public Task ClearRegistrations()
        {
            return Task.CompletedTask;
        }

        public Task ClearValues()
        {
            lock (_lock)
            {
                _dispatched.Clear();
                _values.Clear();
            }
            return Task.CompletedTask;
        }
    }

    internal static class Poll
    {
        public static async Task UntilAsync(Func<bool> condition, int timeoutMs = 5000)
        {
            var stopwatch = Stopwatch.StartNew();
            while (!condition())
            {
                if (stopwatch.ElapsedMilliseconds > timeoutMs)
                {
                    throw new TimeoutException($"Condition not met within {timeoutMs}ms");
                }

                await Task.Delay(5);
            }
        }
    }
}
