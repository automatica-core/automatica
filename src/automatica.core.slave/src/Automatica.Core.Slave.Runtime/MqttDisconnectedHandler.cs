using MQTTnet.Client.Disconnecting;
using System.Threading.Tasks;

namespace Automatica.Core.Slave.Runtime
{
    internal class MqttDisconnectedHandler : IMqttClientDisconnectedHandler
    {
        public MqttDisconnectedHandler(SlaveRuntime runtime)
        {
            Runtime = runtime;
        }

        public SlaveRuntime Runtime { get; }

        public async Task HandleDisconnectedAsync(MqttClientDisconnectedEventArgs eventArgs)
        {
            await Runtime.Restart();
        }
    }
}
