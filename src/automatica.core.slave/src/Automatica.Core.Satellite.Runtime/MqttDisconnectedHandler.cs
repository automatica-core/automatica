using System.Threading.Tasks;
using MQTTnet.Client.Disconnecting;

namespace Automatica.Core.Satellite.Runtime
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
