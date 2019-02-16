using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Automatica.Core.Base.IO;
using Automatica.Core.Internals;
using Automatica.Push.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Timer = System.Timers.Timer;
using Automatica.Core.Base.Extensions;

namespace Automatica.Core.Runtime.IO
{
    public class Dispatcher : IDispatcher
    {
        private readonly IHubContext<DataHub> _dataHub;
        private readonly ILogger _logger;
        private readonly object _lock = new object();

        protected readonly IDictionary<DispatchableType, IDictionary<Guid, object>> NodeValues =
            new ConcurrentDictionary<DispatchableType, IDictionary<Guid, object>>();

        private readonly IDictionary<DispatchableType, IDictionary<Guid, IList<Action<IDispatchable, object>>>> _registrationMap = new ConcurrentDictionary<DispatchableType, IDictionary<Guid, IList<Action<IDispatchable, object>>>>();
        private readonly Timer _notifyTimer = new Timer();
        public Dispatcher(IHubContext<DataHub> dataHub)
        {
            _dataHub = dataHub;
            _logger = SystemLogger.DispatcherLogger;

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

        private void _notifyTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            lock (_lock)
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
                        _dataHub?.Clients.Group(self.ToString()).SendAsync("dispatchValue", node.Key, self, value);
                    }
                }
            }
        }

        public virtual async Task DispatchValue(IDispatchable self, object value)
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

            _logger.LogInformation($"Driver {self.Id}-{self.Name} dispatched value {value}");
            if (_registrationMap.ContainsKey(self.Type) && _registrationMap[self.Type].ContainsKey(self.Id))
            {
                var dispatch = _registrationMap[self.Type][self.Id];

                foreach (var dis in dispatch)
                {
                    try
                    {
                        var cts = new CancellationTokenSource();
                        cts.CancelAfter(TimeSpan.FromSeconds(10));
                        
                        await DispatchValueInternal(self, value, dis).WithCancellation(cts.Token);
                    }
                    catch (Exception e)
                    {
                        _logger.LogError($"Error while dispatching {self.Id}-{self.Name}. {e}");

                    }
                }
            }

            _dataHub?.Clients.Group("All").SendAsync("dispatchValue", self.Type, self.Id, value);
        }

        private Task DispatchValueInternal(IDispatchable self, object value, Action<IDispatchable, object> dis)
        {
            return Task.Run(() => dis(self, value));
        }

        public void RegisterDispatch(DispatchableType type, Guid id, Action<IDispatchable, object> callback)
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
        }

        public void ClearRegistrations()
        {
            _registrationMap.Clear();
        }
    }
}
