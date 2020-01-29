using MQTTnet;
using MQTTnet.Client.Publishing;
using MQTTnet.Client.Receiving;
using MQTTnet.Server;
using MQTTnet.Server.Status;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Automatica.Core.CI.CreateDatabase
{
    internal class EmptyMqttServer : IMqttServer
    {
        public IMqttServerStartedHandler StartedHandler { get; set; }
        public IMqttServerStoppedHandler StoppedHandler { get; set; }
        public IMqttServerClientConnectedHandler ClientConnectedHandler { get; set; }
        public IMqttServerClientDisconnectedHandler ClientDisconnectedHandler { get; set; }
        public IMqttServerClientSubscribedTopicHandler ClientSubscribedTopicHandler { get; set; }
        public IMqttServerClientUnsubscribedTopicHandler ClientUnsubscribedTopicHandler { get; set; }

        public IMqttServerOptions Options => null;

        public IMqttApplicationMessageReceivedHandler ApplicationMessageReceivedHandler { get; set; }

        public Task ClearRetainedApplicationMessagesAsync()
        {
            throw new NotImplementedException();
        }

        public Task<IList<IMqttClientStatus>> GetClientStatusAsync()
        {
            throw new NotImplementedException();
        }

        public Task<IList<MqttApplicationMessage>> GetRetainedApplicationMessagesAsync()
        {
            throw new NotImplementedException();
        }

        public Task<IList<IMqttSessionStatus>> GetSessionStatusAsync()
        {
            throw new NotImplementedException();
        }

        public Task<MqttClientPublishResult> PublishAsync(MqttApplicationMessage applicationMessage, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task StartAsync(IMqttServerOptions options)
        {
            throw new NotImplementedException();
        }

        public Task StopAsync()
        {
            throw new NotImplementedException();
        }

        public Task SubscribeAsync(string clientId, ICollection<TopicFilter> topicFilters)
        {
            throw new NotImplementedException();
        }

        public Task UnsubscribeAsync(string clientId, ICollection<string> topicFilters)
        {
            throw new NotImplementedException();
        }
    }
}
