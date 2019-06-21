using System;
using System.Threading.Tasks;
using MQTTnet.Client;

namespace Automatica.Core.Plugin.Standalone.Factories
{
    public interface IRemoteFactory
    {
        Task SubmitFactoryData(Guid factoryGuid, IMqttClient client);
    }
}
