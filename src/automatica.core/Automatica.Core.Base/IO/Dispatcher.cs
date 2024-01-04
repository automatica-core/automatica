using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Automatica.Core.Base.IO.Remanent;
using Automatica.Core.Base.Remote;
using Automatica.Core.EF.Models;
using Microsoft.Extensions.Logging;
using Timer = System.Timers.Timer;

[assembly: InternalsVisibleTo("Automatica.Core.Tests")]
[assembly: InternalsVisibleTo("Automatica.Core.UnitTests.Base")]

namespace Automatica.Core.Base.IO
{
    internal class Dispatcher : IDispatcher
    {
        private readonly IDataBroadcast _dataBroadcast;
        private readonly IRemoteSender _remoteSender;
        private readonly IRemanentHandler _remanentHandler;
        private readonly IRuleInstanceVisuNotify _ruleNotifier;
        private readonly ILogger _logger;
        private readonly object _lock = new object();
        

        protected readonly IDictionary<DispatchableType, IDictionary<Guid, DispatchValue>> NodeValues =
            new ConcurrentDictionary<DispatchableType, IDictionary<Guid, DispatchValue>>();

        protected readonly IDictionary<Guid, DispatchValue> Values = new ConcurrentDictionary<Guid, DispatchValue>();

        private readonly IDictionary<DispatchableType, IDictionary<Guid, IList<Action<IDispatchable, DispatchValue>>>> _registrationMap = new ConcurrentDictionary<DispatchableType, IDictionary<Guid, IList<Action<IDispatchable, DispatchValue>>>>();

        private readonly IDictionary<Guid, IList<Action<IDispatchable, DispatchValue>>> _universalRegistrationMap =
            new ConcurrentDictionary<Guid, IList<Action<IDispatchable, DispatchValue>>>();


        private readonly Timer _notifyTimer = new Timer();

        private readonly IDictionary<Guid, IDictionary<Action<IDispatchable, DispatchValue>, int>> _hopCounts = new ConcurrentDictionary<Guid, IDictionary<Action<IDispatchable, DispatchValue>, int>>();

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


        public async Task Init(CancellationToken token = default)
        {
            var values = await _remanentHandler.GetAllAsync(token);
            foreach (var keyPair in values)
            {
                await DispatchValue(new RemanentDispatchable(keyPair.Key), keyPair.Value);
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
                if (Values.ContainsKey(id))
                {
                    return Values[id];
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

                    if (!Values.ContainsKey(self.Id))
                    {
                        Values.Add(self.Id, dispatchable);
                    }
                    else
                    {
                        Values[self.Id] = dispatchable;
                    }
                    
                }
                else
                {
                    if (NodeValues[self.Type][self.Id] == value) // value did not change
                    {
                        return;
                    }
                    NodeValues[self.Type][self.Id] = dispatchable;
                    Values[self.Id] = dispatchable;
                }
            }
        }

        private async Task Dispatch(IDispatchable self, DispatchValue value, Action<IDispatchable, DispatchValue, Action<IDispatchable, DispatchValue>> dispatchAction)
        {
            StoreValue(self, value);

            if (self.Type == DispatchableType.RuleInstance)
            {
                await _ruleNotifier.NotifyValueChanged(self, value);
            }

            if (!_hopCounts.ContainsKey(self.Id))
            {
                _hopCounts.Add(self.Id, new ConcurrentDictionary<Action<IDispatchable, DispatchValue>, int>());
            }

            _logger.LogInformation($"Driver {self.Id}-{self.Name} dispatched value {value.Value} from a {value.ValueSource} operation");

            if (self.IsRemanent && self is not RemanentDispatchable)
            {
                await _remanentHandler.SaveValueAsync(self.Id, value);
            }

            if (_universalRegistrationMap.TryGetValue(self.Id, out var dispatch1))
            {
                await ExecuteDispatch(self, value, dispatch1, dispatchAction);
            }

            if (_registrationMap.ContainsKey(self.Type) && _registrationMap[self.Type].ContainsKey(self.Id))
            {
                var dispatch = _registrationMap[self.Type][self.Id];
                await ExecuteDispatch(self, value, dispatch, dispatchAction);
            }
            else if (self.Type == DispatchableType.Visualization)
            {
                foreach (var all in _registrationMap)
                {
                    if (all.Value.TryGetValue(self.Id, out var dispatch))
                    {
                        await ExecuteDispatch(self, value, dispatch, dispatchAction);
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

        private Task ExecuteDispatch(IDispatchable self, DispatchValue value, IList<Action<IDispatchable, DispatchValue>> dispatch, Action<IDispatchable, DispatchValue, Action<IDispatchable, DispatchValue>> dispatchAction)
        {
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

            return Task.CompletedTask;
        }

        private void ResetHopCount(IDispatchable self, Action<IDispatchable, DispatchValue> action)
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

        private void IncrementHopCount(IDispatchable self, Action<IDispatchable, DispatchValue> action)
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

        public Task DispatchValue(IDispatchable self, object value, DispatchValueSource valueSource)
        {
            var dispatchable = new DispatchValue(self.Id, self.Type, value, DateTime.Now, valueSource);
            return DispatchValue(self, dispatchable);
        }

        public virtual async Task DispatchValue(IDispatchable self, DispatchValue value)
        {
            async void DispatchAction(IDispatchable a, DispatchValue b, Action<IDispatchable, DispatchValue> c)
            {
                await DispatchValueInternal(self, value, c);
            }

            await Dispatch(self, value, DispatchAction);
        }


        public Task UnRegisterDispatch(DispatchableType type, Guid id)
        {
            if (_registrationMap.ContainsKey(type) && _registrationMap[type].ContainsKey(id))
            {
                _registrationMap[type].Remove(id);
            }

            return Task.CompletedTask;
        }
        public Task UnRegisterDispatch(Guid id, Action<IDispatchable, DispatchValue> callback)
        {
            if (_universalRegistrationMap.ContainsKey(id))
            {
                _universalRegistrationMap.Remove(id);
            }

            return Task.CompletedTask;
        }

        protected virtual Task DispatchValueInternal(IDispatchable self, DispatchValue value, Action<IDispatchable, DispatchValue> dis)
        {
            return Task.Run(() =>
            {
                dis(self, value);
            });
        }

        public virtual Task RegisterDispatch(DispatchableType type, Guid id, Action<IDispatchable, DispatchValue> callback)
        {
            if (!_registrationMap.ContainsKey(type))
            {
                _registrationMap.Add(type, new ConcurrentDictionary<Guid, IList<Action<IDispatchable, DispatchValue>>>());
            }
            if (!_registrationMap[type].ContainsKey(id))
            {
                _registrationMap[type].Add(id, new List<Action<IDispatchable, DispatchValue>>());
            }

            _registrationMap[type][id].Add(callback);
            return Task.CompletedTask;
        }

        public Task RegisterDispatch(Guid id, Action<IDispatchable, DispatchValue> callback)
        {
            if (!_universalRegistrationMap.ContainsKey(id))
            {
                _universalRegistrationMap.Add(id, new List<Action<IDispatchable, DispatchValue>>());
            }

            _universalRegistrationMap[id].Add(callback);
            return Task.CompletedTask;
        }


        public virtual Task ClearRegistrations()
        {
            _registrationMap.Clear();
            _universalRegistrationMap.Clear();
            _hopCounts.Clear();
            return Task.CompletedTask;
        }

        public Task ClearValues()
        {
            lock (_lock)
            {
                NodeValues.Clear();
            }

            return Task.CompletedTask;
        }
    }
}
