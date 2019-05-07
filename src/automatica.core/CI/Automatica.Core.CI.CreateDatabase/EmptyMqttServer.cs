using MQTTnet;
using MQTTnet.Server;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Automatica.Core.CI.CreateDatabase
{
    internal class EmptyMqttServer : IMqttServer
    {
        public IMqttServerOptions Options => throw new NotImplementedException();

        public event EventHandler Started;
        public event EventHandler Stopped;
        public event EventHandler<MqttClientConnectedEventArgs> ClientConnected;
        public event EventHandler<MqttClientDisconnectedEventArgs> ClientDisconnected;
        public event EventHandler<MqttClientSubscribedTopicEventArgs> ClientSubscribedTopic;
        public event EventHandler<MqttClientUnsubscribedTopicEventArgs> ClientUnsubscribedTopic;
        public event EventHandler<MqttApplicationMessageReceivedEventArgs> ApplicationMessageReceived;

        public Task ClearRetainedMessagesAsync()
        {
            return Task.CompletedTask;
        }

        public IList<IMqttClientSessionStatus> GetClientSessionsStatus()
        {
            return new List<IMqttClientSessionStatus>();
        }

        public Task<IList<IMqttClientSessionStatus>> GetClientSessionsStatusAsync()
        {
            return null;
        }

        public IList<MqttApplicationMessage> GetRetainedMessages()
        {

            return null;
        }

        public Task PublishAsync(MqttApplicationMessage applicationMessage)
        {

            return null;
        }

        public Task StartAsync(IMqttServerOptions options)
        {

            return null;
        }

        public Task StopAsync()
        {

            return null;
        }

        public Task SubscribeAsync(string clientId, IList<TopicFilter> topicFilters)
        {

            return null;
        }

        public Task UnsubscribeAsync(string clientId, IList<string> topicFilters)
        {

            return null;
        }
    }
}
