using Automatica.Core.Base.IO;
using Automatica.Core.Internals.Mqtt;
using Automatica.Core.Internals.Serialization;
using MQTTnet.Client;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Automatica.Core.Plugin.Dockerize.Dispatcher
{

    internal class MqttDispatcher : IDispatcher
    {
        private readonly IMqttClient _mqttClient;
        private readonly IList<string> _subscriptions = new List<string>();
        private readonly object _lock = new object();

        protected readonly IDictionary<DispatchableType, IDictionary<Guid, object>> NodeValues =
           new ConcurrentDictionary<DispatchableType, IDictionary<Guid, object>>();

        private readonly IDictionary<DispatchableType, IDictionary<Guid, IList<Action<IDispatchable, object>>>> _registrationMap = new ConcurrentDictionary<DispatchableType, IDictionary<Guid, IList<Action<IDispatchable, object>>>>();

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

        private Task DispatchValueInternal(IDispatchable self, object value, Action<IDispatchable, object> dis)
        {
            return Task.Run(() => dis(self, value));
        }

        public async Task DispatchValue(IDispatchable self, object value)
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
                        //_logger.LogError($"Error while dispatching {self.Id}-{self.Name}. {e}");

                    }
                }
            }

            if (self.Source != DispatchableSource.Mqtt)
            {
                var topic = $"{MqttTopicConstants.DISPATCHER_TOPIC}/{self.Type}/{self.Id}";
                await _mqttClient.PublishAsync(new MQTTnet.MqttApplicationMessage()
                {
                    Topic = topic,
                    QualityOfServiceLevel = MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce,
                    Payload = BinarySerializer.Serialize(value),
                    Retain = true
                });

            }
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

        public IDictionary<Guid, object> GetValues(DispatchableType type)
        {
            lock (_lock)
            {
                if (NodeValues.ContainsKey(type))
                    return NodeValues[type];
            }
            return new Dictionary<Guid, object>();
        }

        public async Task RegisterDispatch(DispatchableType type, Guid id, Action<IDispatchable, object> callback)
        {
            var topic = $"{MqttTopicConstants.DISPATCHER_TOPIC}/{type}/{id}";
            _subscriptions.Add(topic);

            await _mqttClient.SubscribeAsync(topic, MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce);

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
    }
}
