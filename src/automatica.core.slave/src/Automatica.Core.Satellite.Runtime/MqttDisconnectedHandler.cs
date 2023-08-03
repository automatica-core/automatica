using MQTTnet.Client.Disconnecting;
using System.Threading.Tasks;

namespace Automatica.Core.Slave.Runtime
{
    internal class MqttDisconnectedHandler : IMqttClientDisconnectedHandler
    {
        public MqttDisconnectedHandler(SatelliteRuntime runtime)
        {
            Runtime = runtime;
        }

        public SatelliteRuntime Runtime { get; }

        public async Task HandleDisconnectedAsync(MqttClientDisconnectedEventArgs eventArgs)
        {
            await Runtime.Restart();
        }
    }
}
