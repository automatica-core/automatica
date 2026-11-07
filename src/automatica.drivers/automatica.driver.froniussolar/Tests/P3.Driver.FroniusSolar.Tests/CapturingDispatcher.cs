using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;

namespace P3.Driver.FroniusSolar.Tests
{
    /// <summary>
    /// Records every dispatched value per dispatchable id
    /// </summary>
    internal sealed class CapturingDispatcher : IDispatcher
    {
        private readonly ConcurrentDictionary<Guid, ConcurrentQueue<object>> _values = new();

        public IReadOnlyList<object> ValuesOf(Guid id) => _values.TryGetValue(id, out var q) ? q.ToList() : new List<object>();

        public object? LastValueOf(Guid id) => ValuesOf(id).LastOrDefault();

        public async Task<object?> WaitForValue(Guid id, int count = 1, int timeoutMs = 5000)
        {
            var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
            while (DateTime.UtcNow < deadline)
            {
                var values = ValuesOf(id);
                if (values.Count >= count)
                {
                    return values[count - 1];
                }
                await Task.Delay(10);
            }
            return null;
        }

        public Task Init(CancellationToken token = default) => Task.CompletedTask;

        public Task DispatchValue(IDispatchable self, object value, DispatchValueSource valueSource = DispatchValueSource.Read)
        {
            _values.GetOrAdd(self.Id, _ => new ConcurrentQueue<object>()).Enqueue(value);
            return Task.CompletedTask;
        }

        public Task DispatchValue(IDispatchable self, DispatchValue value) => DispatchValue(self, value.Value, value.ValueSource);

        public Task UnRegisterDispatch(DispatchableType type, Guid id) => Task.CompletedTask;

        public Task RegisterDispatch(DispatchableType type, Guid id, Action<IDispatchable, DispatchValue> callback) => Task.CompletedTask;

        public IDictionary<Guid, DispatchValue> GetValues() => new Dictionary<Guid, DispatchValue>();

        public IDictionary<Guid, DispatchValue> GetValues(DispatchableType type) => new Dictionary<Guid, DispatchValue>();

        public DispatchValue GetValue(Guid id) => null!;

        public DispatchValue GetValue(DispatchableType type, Guid id) => null!;

        public Task ClearRegistrations() => Task.CompletedTask;

        public Task ClearValues() => Task.CompletedTask;
    }
}
