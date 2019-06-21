using System;
using Automatica.Core.Base.IO;
using Automatica.Core.Base.Serialization;

namespace Automatica.Core.Base.Mqtt
{
    internal class MqttDispatchable : IDispatchable
    {
        public DispatchableSource Source => DispatchableSource.Mqtt;

        public DispatchableType Type { get; internal set; }

        public string Name { get; internal set; }

        public Guid Id { get; internal set; }
    }

    public static class MqttDisptacherHelper
    {
        public static void MqttDispatch(this IDispatcher self, string topic, byte[] data)
        {
            var split = topic.Split("/");

            if (Enum.TryParse(split[1], out DispatchableType enu))
            {
                var id = new Guid(split[2]);


                var mqttDispatch = new MqttDispatchable()
                {
                    Id = id,
                    Type = enu,
                    Name = "MqttUnknown"
                };

                self.DispatchValue(mqttDispatch, BinarySerializer.Deserialize(data));
            }
        }
    }
}
