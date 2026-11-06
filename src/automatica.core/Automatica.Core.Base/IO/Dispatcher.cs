using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using AsyncKeyedLock;
using Automatica.Core.Base.Calendar;
using Automatica.Core.Base.IO.Remanent;
using Automatica.Core.Base.Remote;
using Microsoft.Extensions.Logging;
using Timer = System.Timers.Timer;

[assembly: InternalsVisibleTo("Automatica.Core.Tests")]
[assembly: InternalsVisibleTo("Automatica.Core.UnitTests.Base")]

namespace Automatica.Core.Base.IO
{
    internal partial class Dispatcher : IDispatcher
    {
        private static readonly TimeSpan LoopLogInterval = TimeSpan.FromSeconds(10);

        private readonly IDataBroadcast _dataBroadcast;
        private readonly IRemoteSender _remoteSender;
        private readonly IRemanentHandler _remanentHandler;
        private readonly IRuleInstanceVisuNotify _ruleNotifier;
        private readonly ILogger _logger;
        private readonly object _lock = new object();

        private readonly AsyncKeyedLocker<Guid> _semaphore = new();

        protected readonly IDictionary<DispatchableType, IDictionary<Guid, DispatchValue>> NodeValues =
            new ConcurrentDictionary<DispatchableType, IDictionary<Guid, DispatchValue>>();

        protected readonly IDictionary<Guid, DispatchValue> Values = new ConcurrentDictionary<Guid, DispatchValue>();

        /// <summary>
        /// Type of the dispatchable that dispatched the latest value of an id (the value itself is in <see cref="Values"/>)
        /// </summary>
        private readonly ConcurrentDictionary<Guid, DispatchableType> _latestValueType = new();

        /// <summary>
        /// Copy-on-write arrays, only replaced under <see cref="_registrationLock"/>, read lock free
        /// </summary>
        private readonly ConcurrentDictionary<DispatchableType, ConcurrentDictionary<Guid, Subscription[]>> _registrationMap = new();
        private readonly object _registrationLock = new object();

        private readonly ConcurrentDictionary<Guid, long> _loopLogTimestamps = new();

        private readonly Timer _notifyTimer = new Timer();
        private int _notifyRunning;

        public Dispatcher(ILogger<Dispatcher> logger, IDataBroadcast dataBroadcast, IRemoteSender remoteSender, IRemanentHandler remanentHandler, IRuleInstanceVisuNotify ruleNotifier)
        {
            _dataBroadcast = dataBroadcast;
            _remoteSender = remoteSender;
            _remanentHandler = remanentHandler;
            _ruleNotifier = ruleNotifier;

            _logger = logger;

            _notifyTimer.Elapsed += _notifyTimer_Elapsed;
            _notifyTimer.Interval = 30000;
            _notifyTimer.Start();
        }


        /// <summary>
        /// Restores the remanent values. The values are only stored (GetValue/visualization), they are not dispatched to the
        /// registrations, otherwise a stale value would be written to the linked nodes/logics as if it was new.
        /// </summary>
        public async Task Init(CancellationToken token = default)
        {
            var values = await _remanentHandler.GetAllAsync(token);
            if (values == null)
            {
                return;
            }

            foreach (var keyPair in values)
            {
                if (keyPair.Value == null || Values.ContainsKey(keyPair.Key))
                {
                    continue; // never overwrite a live value
                }

                StoreValue(new RemanentDispatchable(keyPair.Key), keyPair.Value);
            }
        }

        public IDictionary<Guid, DispatchValue> GetValues()
        {
            lock (_lock)
            {
                return Values;
            }
        }

        public DispatchValue GetValue(Guid id)
        {

            lock (_lock)
            {
                if (Values.TryGetValue(id, out var value))
                {
                    return value;
                }
            }

            return null;
        }


        public virtual IDictionary<Guid, DispatchValue> GetValues(DispatchableType type)
        {
            lock (_lock)
            {
                if(NodeValues.TryGetValue(type, out var values))
                    return values;
            }
            return new Dictionary<Guid, DispatchValue>();
        }

        public DispatchValue GetValue(DispatchableType type, Guid id)
        {

            lock (_lock)
            {
                if (NodeValues.ContainsKey(type) && NodeValues[type].ContainsKey(id))
                {
                    return NodeValues[type][id];
                }
            }

            return null;
        }

        private async void _notifyTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            // async void (timer event) - nothing may escape, and never run twice at the same time
            if (Interlocked.Exchange(ref _notifyRunning, 1) == 1)
            {
                return;
            }

            try
            {
                // only the latest value per id, a node has values for several types (e.g. Visualization and NodeInstance)
                // and re-sending all of them could show an outdated value on the clients
                foreach (var (id, type) in _latestValueType)
                {
                    if (type == DispatchableType.RuleInstance)
                    {
                        continue;
                    }

                    if (Values.TryGetValue(id, out var value))
                    {
                        await _dataBroadcast.DispatchValue(type, id, value);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error re-broadcasting values");
            }
            finally
            {
                Volatile.Write(ref _notifyRunning, 0);
            }
        }

        private void StoreValue(IDispatchable self, DispatchValue value)
        {
            lock (_lock)
            {
                if (!NodeValues.ContainsKey(self.Type))
                {
                    NodeValues.Add(self.Type, new ConcurrentDictionary<Guid, DispatchValue>());

                }

                var dispatchable = value;

                if (!NodeValues[self.Type].ContainsKey(self.Id))
                {
                    NodeValues[self.Type].Add(self.Id, dispatchable);

                    Values[self.Id] = dispatchable;
                    _latestValueType[self.Id] = self.Type;
                }
                else
                {
                    if (NodeValues[self.Type][self.Id] == value) // value did not change
                    {
                        return;
                    }
                    NodeValues[self.Type][self.Id] = dispatchable;
                    Values[self.Id] = dispatchable;
                    _latestValueType[self.Id] = self.Type;
                }
            }
        }

        private async Task Dispatch(IDispatchable self, DispatchValue value)
        {
            // has to be calculated before the first await, it depends on the async context of the caller
            var chain = DispatchChain.Next();

            using var releaser = await _semaphore.LockAsync(self.Id).ConfigureAwait(false);

            StoreValue(self, value);

            if (self.Type == DispatchableType.RuleInstance)
            {
                try
                {
                    await _ruleNotifier.NotifyValueChanged(self, value);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, $"Error notifying rule value change of {self.Id}-{self.Name}");
                }
            }

            LogValueDispatched(_logger, self.Id, self.Name, value.Value, value.ValueSource);

            if (self.IsRemanent && self is not RemanentDispatchable)
            {
                try
                {
                    await _remanentHandler.SaveValueAsync(self.Id, value);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, $"Error saving remanent value of {self.Id}-{self.Name}");
                }
            }

            if (chain.Depth > DispatchChain.MaxDepth)
            {
                LogDispatchLoop(self, chain);
            }
            else if (TryGetSubscriptions(self.Type, self.Id, out var subscriptions))
            {
                Enqueue(self, value, subscriptions, chain);
            }
            else if (self.Type == DispatchableType.Visualization)
            {
                foreach (var all in _registrationMap)
                {
                    if (all.Value.TryGetValue(self.Id, out var dispatch) && dispatch.Length > 0)
                    {
                        Enqueue(self, value, dispatch, chain);
                    }
                }
            }

            // satellites get every value, independent of local registrations (links, trendings)
            if (self.Type != DispatchableType.Visualization && self.Source != DispatchableSource.Remote)
            {
                try
                {
                    await _remoteSender.DispatchValue(self, value);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, $"Error sending value of {self.Id}-{self.Name} to remote clients");
                }
            }

            try
            {
                await _dataBroadcast.DispatchValue(self.Type, self.Id, value);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error broadcasting value of {self.Id}-{self.Name}");
            }
        }

        private void LogDispatchLoop(IDispatchable self, DispatchChain chain)
        {
            var now = Stopwatch.GetTimestamp();
            var last = _loopLogTimestamps.GetOrAdd(self.Id, 0);

            if (last != 0 && Stopwatch.GetElapsedTime(last, now) < LoopLogInterval)
            {
                return;
            }

            _loopLogTimestamps[self.Id] = now;
            var exception = new DispatchLoopDetectedException(self);
            _logger.LogError(exception,
                $"Detected a dispatch loop while dispatching {self.Id}-{self.Name} ({chain.Depth} hops within {DispatchChain.Window.TotalSeconds}s), the value is not forwarded to its links");
        }

        private bool TryGetSubscriptions(DispatchableType type, Guid id, out Subscription[] subscriptions)
        {
            subscriptions = null;
            return _registrationMap.TryGetValue(type, out var byId) && byId.TryGetValue(id, out subscriptions) && subscriptions.Length > 0;
        }

        private static void Enqueue(IDispatchable self, DispatchValue value, Subscription[] subscriptions, DispatchChain chain)
        {
            foreach (var subscription in subscriptions)
            {
                subscription.Enqueue(self, value, chain);
            }
        }

        private async Task InvokeSubscription(Subscription subscription, IDispatchable self, DispatchValue value)
        {
            try
            {
                if (subscription.Callback != null)
                {
                    await subscription.Callback(self, value).ConfigureAwait(false);
                }
                else
                {
                    await DispatchValueInternal(self, value, subscription.LegacyCallback).ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error while dispatching {self.Id}-{self.Name}");
            }
        }

        public Task DispatchValue(IDispatchable self, object value, DispatchValueSource valueSource)
        {
            var dispatchable = new DispatchValue(self.Id, self.Type, value, DateTimeHelper.ProviderInstance.GetLocalNow().DateTime, valueSource);
            return DispatchValue(self, dispatchable);
        }

        public virtual Task DispatchValue(IDispatchable self, DispatchValue value)
        {
            return Dispatch(self, value);
        }


        public Task UnRegisterDispatch(DispatchableType type, Guid id)
        {
            Subscription[] removed = null;
            lock (_registrationLock)
            {
                if (_registrationMap.TryGetValue(type, out var byId))
                {
                    byId.TryRemove(id, out removed);
                }
            }

            if (removed != null)
            {
                foreach (var subscription in removed)
                {
                    subscription.MarkDisposed();
                }
            }

            return Task.CompletedTask;
        }

        public Task UnRegisterDispatch(DispatchableType type, Guid id, Action<IDispatchable, DispatchValue> callback)
        {
            if (callback == null)
            {
                return Task.CompletedTask;
            }

            Subscription[] matching;
            lock (_registrationLock)
            {
                if (!_registrationMap.TryGetValue(type, out var byId) || !byId.TryGetValue(id, out var existing))
                {
                    return Task.CompletedTask;
                }

                matching = existing.Where(a => a.LegacyCallback != null && a.LegacyCallback.Equals(callback)).ToArray();
            }

            foreach (var subscription in matching)
            {
                subscription.Dispose();
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Invokes a callback registered with <see cref="RegisterDispatch"/>. Runs on the delivery task of the registration,
        /// the next value of the registration is delivered after the returned task completed.
        /// </summary>
        protected virtual Task DispatchValueInternal(IDispatchable self, DispatchValue value, Action<IDispatchable, DispatchValue> dis)
        {
            dis(self, value);
            return Task.CompletedTask;
        }

        public virtual Task RegisterDispatch(DispatchableType type, Guid id, Action<IDispatchable, DispatchValue> callback)
        {
            AddSubscription(new Subscription(this, type, id, null, callback));
            return Task.CompletedTask;
        }

        public Task<IDisposable> Subscribe(DispatchableType type, Guid id, Func<IDispatchable, DispatchValue, Task> callback)
        {
            var subscription = new Subscription(this, type, id, callback, null);
            AddSubscription(subscription);
            return Task.FromResult<IDisposable>(subscription);
        }

        private void AddSubscription(Subscription subscription)
        {
            lock (_registrationLock)
            {
                var byId = _registrationMap.GetOrAdd(subscription.Type, _ => new ConcurrentDictionary<Guid, Subscription[]>());
                byId.TryGetValue(subscription.Id, out var existing);

                var updated = existing == null ? new[] { subscription } : existing.Append(subscription).ToArray();
                byId[subscription.Id] = updated;
            }
        }

        private void RemoveSubscription(Subscription subscription)
        {
            lock (_registrationLock)
            {
                if (!_registrationMap.TryGetValue(subscription.Type, out var byId) || !byId.TryGetValue(subscription.Id, out var existing))
                {
                    return;
                }

                var updated = existing.Where(a => !ReferenceEquals(a, subscription)).ToArray();
                if (updated.Length == 0)
                {
                    byId.TryRemove(subscription.Id, out _);
                }
                else if (updated.Length != existing.Length)
                {
                    byId[subscription.Id] = updated;
                }
            }
        }

        public virtual Task ClearRegistrations()
        {
            List<Subscription> removed;
            lock (_registrationLock)
            {
                removed = _registrationMap.Values.SelectMany(a => a.Values).SelectMany(a => a).ToList();
                _registrationMap.Clear();
            }

            foreach (var subscription in removed)
            {
                subscription.MarkDisposed();
            }
            return Task.CompletedTask;
        }

        public Task ClearValues()
        {
            lock (_lock)
            {
                NodeValues.Clear();
                _latestValueType.Clear();
            }

            return Task.CompletedTask;
        }

        [LoggerMessage(Level = LogLevel.Debug, Message = "Driver {Id}-{Name} dispatched value {Value} from a {ValueSource} operation")]
        private static partial void LogValueDispatched(ILogger logger, Guid id, string name, object value, DispatchValueSource valueSource);

        /// <summary>
        /// One registration. Values are delivered one after another in dispatch order (per source the order is guaranteed by the
        /// per source lock in <see cref="Dispatch"/>), different registrations are delivered in parallel.
        /// </summary>
        private sealed class Subscription : IDisposable
        {
            private readonly Dispatcher _owner;
            private readonly ConcurrentQueue<(IDispatchable Source, DispatchValue Value, DispatchChain Chain)> _queue = new();
            private int _delivering;
            private volatile bool _disposed;

            internal DispatchableType Type { get; }
            internal Guid Id { get; }
            internal Func<IDispatchable, DispatchValue, Task> Callback { get; }
            internal Action<IDispatchable, DispatchValue> LegacyCallback { get; }

            internal Subscription(Dispatcher owner, DispatchableType type, Guid id, Func<IDispatchable, DispatchValue, Task> callback, Action<IDispatchable, DispatchValue> legacyCallback)
            {
                _owner = owner;
                Type = type;
                Id = id;
                Callback = callback;
                LegacyCallback = legacyCallback;
            }

            internal void Enqueue(IDispatchable source, DispatchValue value, DispatchChain chain)
            {
                if (_disposed)
                {
                    return;
                }

                _queue.Enqueue((source, value, chain));

                if (Interlocked.CompareExchange(ref _delivering, 1, 0) == 0)
                {
                    _ = Task.Run(DeliverAsync);
                }
            }

            private async Task DeliverAsync()
            {
                while (true)
                {
                    while (_queue.TryDequeue(out var item))
                    {
                        if (_disposed)
                        {
                            _queue.Clear();
                            break;
                        }

                        // values dispatched by the callback continue the chain of this value (loop detection)
                        DispatchChain.Current = item.Chain;
                        await _owner.InvokeSubscription(this, item.Source, item.Value).ConfigureAwait(false);
                    }

                    Volatile.Write(ref _delivering, 0);

                    // something was enqueued after the last TryDequeue but before the reset, continue if nobody else took over
                    if (_queue.IsEmpty || Interlocked.CompareExchange(ref _delivering, 1, 0) != 0)
                    {
                        return;
                    }
                }
            }

            internal void MarkDisposed()
            {
                _disposed = true;
            }

            public void Dispose()
            {
                if (_disposed)
                {
                    return;
                }

                MarkDisposed();
                _owner.RemoveSubscription(this);
            }
        }
    }
}
