using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.Base.Remote;
using Automatica.Core.Base.Serialization;
using Docker.DotNet.Models;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using MQTTnet.Client;
using Newtonsoft.Json;

namespace Automatica.Core.Plugin.Standalone.Dispatcher
{
    internal class MqttDispatcher : IDispatcher
    {
        private readonly IMqttClient _mqttClient;
        private readonly IList<string> _subscriptions = new List<string>();
        private readonly object _lock = new object();

        private readonly Semaphore _semaphore = new Semaphore(1, 1);

        protected readonly IDictionary<DispatchableType, IDictionary<Guid, DispatchValue>> NodeValues =
           new ConcurrentDictionary<DispatchableType, IDictionary<Guid, DispatchValue>>();

        private readonly IDictionary<DispatchableType, IDictionary<Guid, IList<Action<IDispatchable, DispatchValue>>>> _registrationMap = new ConcurrentDictionary<DispatchableType, IDictionary<Guid, IList<Action<IDispatchable, DispatchValue>>>>();

        public MqttDispatcher(IMqttClient mqttClient)
        {
            _mqttClient = mqttClient;
        }
        public async Task ClearRegistrations()
        {
            await _mqttClient.UnsubscribeAsync(_subscriptions.ToArray());
            _subscriptions.Clear();
            _registrationMap.Clear();
        }

        public Task ClearValues()
        {
            lock (_lock)
            {
                NodeValues.Clear();
            }

            return Task.CompletedTask;
        }

        private Task DispatchValueInternal(IDispatchable self, DispatchValue value, Action<IDispatchable, DispatchValue> dis)
        {
            return Task.Run(() => dis(self, value));
        }

        private void StoreValue(IDispatchable self, DispatchValue value)
        {
            lock (_lock)
            {
                if (!NodeValues.ContainsKey(self.Type))
                {
                    NodeValues.Add(self.Type, new ConcurrentDictionary<Guid, DispatchValue>());
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

        public Task Init(CancellationToken token = new CancellationToken())
        {
            return Task.CompletedTask;
        }

        public Task DispatchValue(IDispatchable self, object value, DispatchValueSource valueSource)
        {
            return DispatchValue(self, new DispatchValue(self.Id, self.Type, value, DateTime.Now, valueSource));
        }

        public async Task DispatchValue(IDispatchable self, DispatchValue value)
        {

            if (_registrationMap.ContainsKey(self.Type) && _registrationMap[self.Type].ContainsKey(self.Id))
            {
                var dispatch = _registrationMap[self.Type][self.Id];

                foreach (var dis in dispatch)
                {
                    try
                    {

                        await DispatchValueInternal(self, value, dis);
                    }
                    catch (Exception e)
                    {
                        //.LogError($"Error while dispatching {self.Id}-{self.Name}. {e}");

                    }
                }
            }

            lock (_lock)
            {
                if (NodeValues.ContainsKey(self.Type) && NodeValues[self.Type].ContainsKey(self.Id) &&
                    NodeValues[self.Type][self.Id] == value)
                {
                    return;
                }

                StoreValue(self, value);
            }

            if (self.Source != DispatchableSource.Remote)
            {
                var remoteDispatchValue = new RemoteDispatchValue()
                {
                    Source = _mqttClient.Options.ClientId,
                    Value = value
                };

                var topic = $"{RemoteTopicConstants.DISPATCHER_TOPIC}/{self.Type}/{self.Id}";
                await _mqttClient.PublishAsync(new MQTTnet.MqttApplicationMessage()
                {
                    Topic = topic,
                    QualityOfServiceLevel = MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce,
                    Payload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(remoteDispatchValue)),
                    Retain = true
                });

            }
        }

        public Task UnRegisterDispatch(DispatchableType type, Guid id)
        {
            return Task.CompletedTask;
        }


        public DispatchValue GetValue(Guid id)
        {
            return null;
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

        public IDictionary<Guid, DispatchValue> GetValues()
        {
            return null;

        }

        public IDictionary<Guid, DispatchValue> GetValues(DispatchableType type)
        {
            lock (_lock)
            {
                if (NodeValues.ContainsKey(type))
                    return NodeValues[type];
            }
            return new Dictionary<Guid, DispatchValue>();
        }

        public async Task RegisterDispatch(DispatchableType type, Guid id, Action<IDispatchable, DispatchValue> callback)
        {

            _semaphore.WaitOne();

            try
            {
                var topic = $"{RemoteTopicConstants.DISPATCHER_TOPIC}/{type}/{id}";
                _subscriptions.Add(topic);

                await _mqttClient.SubscribeAsync(topic, MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce);

                if (!_registrationMap.ContainsKey(type))
                {
                    _registrationMap.Add(type, new ConcurrentDictionary<Guid, IList<Action<IDispatchable, DispatchValue>>>());
                }

                if (!_registrationMap[type].ContainsKey(id))
                {
                    _registrationMap[type].Add(id, new List<Action<IDispatchable, DispatchValue>>());
                }

                _registrationMap[type][id].Add(callback);
            }
            finally
            {
                _semaphore.Release(1);
            }
        }
    }
}
