using MQTTnet.Client;
using System;
using System.Threading.Tasks;

namespace Automatica.Core.Plugin.Dockerize.Factories
{
    public interface IRemoteFactory
    {
        Task SubmitFactoryData(Guid factoryGuid, IMqttClient client);
    }
}
