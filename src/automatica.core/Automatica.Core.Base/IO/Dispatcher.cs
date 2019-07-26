using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Timers;
using Automatica.Core.Base.Remote;
using Automatica.Core.Base.Serialization;
using Microsoft.Extensions.Logging;
using Timer = System.Timers.Timer;

namespace Automatica.Core.Base.IO
{
    public class Dispatcher : IDispatcher
    {
        private readonly IDataBroadcast _dataBroadcast;
        private readonly IRemoteSender _remoteSender;
        private readonly ILogger _logger;
        private readonly object _lock = new object();

        protected readonly IDictionary<DispatchableType, IDictionary<Guid, object>> NodeValues =
            new ConcurrentDictionary<DispatchableType, IDictionary<Guid, object>>();

        private readonly IDictionary<DispatchableType, IDictionary<Guid, IList<Action<IDispatchable, object>>>> _registrationMap = new ConcurrentDictionary<DispatchableType, IDictionary<Guid, IList<Action<IDispatchable, object>>>>();
        private readonly Timer _notifyTimer = new Timer();

        private readonly IDictionary<Guid, IDictionary<Action<IDispatchable, object>, int>> _hopCounts = new ConcurrentDictionary<Guid, IDictionary<Action<IDispatchable, object>, int>>();

        public Dispatcher(ILogger<Dispatcher> logger, IDataBroadcast dataBroadcast, IRemoteSender remoteSender)
        {
            _dataBroadcast = dataBroadcast;
            _remoteSender = remoteSender;

            _logger = logger;

            _notifyTimer.Elapsed += _notifyTimer_Elapsed;
            _notifyTimer.Interval = 1000;
            _notifyTimer.Start();
        }

        public virtual IDictionary<Guid, object> GetValues(DispatchableType type)
        {
            lock (_lock)
            {
                if(NodeValues.ContainsKey(type))
                    return NodeValues[type];
            }
            return new Dictionary<Guid, object>();
        }

        public object GetValue(DispatchableType type, Guid id)
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
            foreach (var node in NodeValues)
            {
                if (node.Key == DispatchableType.RuleInstance)
                {
                    continue;
                }

                foreach (var rnode in node.Value)
                {
                    var self = rnode.Key;
                    var value = rnode.Value;
                    await _dataBroadcast.DispatchValue(node.Key, self, value);
                }
            }
        }

        private void StoreValue(IDispatchable self, object value)
        {
            lock (_lock)
            {
                if (!NodeValues.ContainsKey(self.Type))
                {
                    NodeValues.Add(self.Type, new ConcurrentDictionary<Guid, object>());
                }

                if (!NodeValues[self.Type].ContainsKey(self.Id))
                {
                    NodeValues[self.Type].Add(self.Id, value);
                }
                else
                {
                    if (NodeValues[self.Type][self.Id] == value) // value did not change
                    {
                        return;
                    }
                    NodeValues[self.Type][self.Id] = value;
                }

            }
        }

        private async Task Dispatch(IDispatchable self, object value, Action<IDispatchable, object, Action<IDispatchable, object>> dispatchAction)
        {
            StoreValue(self, value);

            if (!_hopCounts.ContainsKey(self.Id))
            {
                _hopCounts.Add(self.Id, new ConcurrentDictionary<Action<IDispatchable, object>, int>());
            }

            _logger.LogInformation($"Driver {self.Id}-{self.Name} dispatched value {value}");
            if (_registrationMap.ContainsKey(self.Type) && _registrationMap[self.Type].ContainsKey(self.Id))
            {
                var dispatch = _registrationMap[self.Type][self.Id];

                foreach (var dis in dispatch)
                {

                    if (!_hopCounts[self.Id].ContainsKey(dis))
                    {
                        _hopCounts[self.Id].Add(dis, 0);
                    }

                    try
                    {
                        if (_hopCounts[self.Id][dis] > 10)
                        {
                            _hopCounts.Clear();
                            throw new DispatchLoopDetectedException(self);
                        }

                        IncrementHopCount(self, dis);
                        dispatchAction.Invoke(self, value, dis);

                        ResetHopCount(self, dis);
                    }
                    catch (DispatchLoopDetectedException dlde)
                    {
                        _logger.LogError(dlde, $"Detected a dispatch loop while dispatching {dlde.Dispatchable.Id}-{dlde.Dispatchable.Name}");
                        throw;
                    }
                    catch (Exception e)
                    {
                        _logger.LogError($"Error while dispatching {self.Id}-{self.Name}. {e}");

                    }
                }
            }
            else if (self.Source != DispatchableSource.Remote)
            {
                _logger.LogInformation($"Dispatch value via mqtt dispatcher");

                await _remoteSender.DispatchValue(self, value);
            }

            await _dataBroadcast.DispatchValue(self.Type, self.Id, value);
        }

        private void ResetHopCount(IDispatchable self, Action<IDispatchable, object> action)
        {
            if (!_hopCounts.ContainsKey(self.Id))
            {
                return;
            }

            if (!_hopCounts[self.Id].ContainsKey(action))
            {
                return;
            }


            _hopCounts[self.Id][action] = 0;
        }

        private void IncrementHopCount(IDispatchable self, Action<IDispatchable, object> action)
        {
            if (!_hopCounts.ContainsKey(self.Id))
            {
                return;
            }

            if (!_hopCounts[self.Id].ContainsKey(action))
            {
                return;
            }


            _hopCounts[self.Id][action]++;
        }

        public virtual async Task DispatchValue(IDispatchable self, object value)
        {
            await Dispatch(self, value, async (a, b, c) => { await DispatchValueInternal(self, value, c); });
        }


        protected virtual Task DispatchValueInternal(IDispatchable self, object value, Action<IDispatchable, object> dis)
        {
            return Task.Run(() => dis(self, value));
        }

        public virtual Task RegisterDispatch(DispatchableType type, Guid id, Action<IDispatchable, object> callback)
        {
            if (!_registrationMap.ContainsKey(type))
            {
                _registrationMap.Add(type, new ConcurrentDictionary<Guid, IList<Action<IDispatchable, object>>>());
            }
            if (!_registrationMap[type].ContainsKey(id))
            {
                _registrationMap[type].Add(id, new List<Action<IDispatchable, object>>());
            }

            _registrationMap[type][id].Add(callback);
            return Task.CompletedTask;
        }

        public virtual Task ClearRegistrations()
        {
            _registrationMap.Clear();
            _hopCounts.Clear();
            return Task.CompletedTask;
        }
    }
}
