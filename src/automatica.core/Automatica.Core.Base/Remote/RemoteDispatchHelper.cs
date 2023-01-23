using System;
using Automatica.Core.Base.IO;
using Newtonsoft.Json;

namespace Automatica.Core.Base.Remote
{
    internal class RemoteDispatchable : IDispatchable
    {
        public DispatchableSource Source => DispatchableSource.Remote;

        public DispatchableType Type { get; internal set; }

        public string Name { get; internal set; }

        public Guid Id { get; internal set; }
    }

    public static class RemoteDispatchHelper
    {
        public static void MqttDispatch(this IDispatcher self, string topic, string data)
        {
            var split = topic.Split("/");

            if (Enum.TryParse(split[1], out DispatchableType enu))
            {
                var id = new Guid(split[2]);


                var remoteDispatch = new RemoteDispatchable
                {
                    Id = id,
                    Type = enu,
                    Name = "RemoteUnknown"
                };

                var remoteDispatchValue =
                    JsonConvert.DeserializeObject<RemoteDispatchValue>(data);

                self.DispatchValue(remoteDispatch, remoteDispatchValue.Value);
            }
        }
    }
}
