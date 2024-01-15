using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using P3.Driver.HomeKit.Hap.Model;

namespace P3.Driver.HomeKit.Hap.Controllers
{
    internal class CharacteristicsList<T>
    {
        public CharacteristicsList()
        {
            Characteristics = new List<T>();
        }

        [JsonProperty("characteristics")]
        public List<T> Characteristics { get; set; }
    }
    internal sealed class CharacteristicReturn<T> : CharacteristicsList<T>
    {

        [JsonIgnore] public string ContentType { get; set; } = "application/hap+json";
    }
    internal sealed class CharacteristicsController : BaseController
    {
        private readonly ILogger _logger;

        public CharacteristicsController(ILogger logger)
        {
            _logger = logger;
        }
        internal CharacteristicReturn<Characteristic> Put(byte[] v, HapSession session, HomeKitServer server)
        {
            var json = Encoding.UTF8.GetString(v);

            _logger.LogTrace($"Read {json}");

            var characteristics = JsonConvert.DeserializeObject<CharacteristicReturn<SentCharacteristic>>(json);
            var all = server.GetAccessories();
            var retCharactersitcs = new List<Characteristic>();


            foreach (var sendCharacteristic in characteristics.Characteristics)
            {
                var accessoryId = sendCharacteristic.AccessoryNumber;
                var characteristicId = sendCharacteristic.Id;

                var accessory = all.SingleOrDefault(a => a.Id == accessoryId);

                if (accessory == null)
                {
                    _logger.LogError($"Could not find accessory with id {accessoryId}");
                    continue;
                }

                Characteristic characteristic = null;
                foreach (var service in accessory.Services)
                {
                    var c = service.Characteristics.SingleOrDefault(a => a.Id == characteristicId);

                    if (c != null)
                    {
                        characteristic = c;
                        break;
                    }
                }

                if (characteristic == null)
                {
                    _logger.LogError($"Could not find characterstic with id {characteristicId}");
                    continue;
                }

                if (sendCharacteristic.EventBasedNotification.HasValue && sendCharacteristic.EventBasedNotification.Value)
                {
                    server.RegisterNotifications(characteristic, session);
                }
                else
                {
                    server.RegisterNotifications(characteristic, session);
                }

                if (sendCharacteristic.Value != null)
                {
                    characteristic.Value = sendCharacteristic.Value;

                    server.NotifyValueChanged(characteristic);
                }

                characteristic.AccessoryId = accessoryId;
                retCharactersitcs.Add(characteristic);
            }

            return new CharacteristicReturn<Characteristic>
            {
                Characteristics = new List<Characteristic>()
            };
       
        }

        internal CharacteristicReturn<CharacteristicBase> Get(string[] data, HomeKitServer server)
        {
            var all = server.GetAccessories();

            var retCharactersitcs = new List<CharacteristicBase>();
            foreach (var s in data)
            {
                var split = s.Split(".");
                var accessoryId = Convert.ToUInt64(split[0]);
                var characteristicId = Convert.ToInt32(split[1]);

                var accessory = all.SingleOrDefault(a => a.Id == accessoryId);

                if (accessory == null)
                {
                    _logger.LogError($"Could not find accessory with id {accessoryId}");
                    continue;
                }

                Characteristic characteristic = null;
                foreach (var service in accessory.Services)
                {
                    var c = service.Characteristics.SingleOrDefault(a => a.Id == characteristicId);

                    if (c != null)
                    {
                        characteristic = c;
                        break;
                    }
                }

                if (characteristic == null)
                {
                    _logger.LogError($"Could not find characterstic with id {characteristicId}");
                    continue;
                }

                var baseChar = new CharacteristicBase(characteristic.DefaultType)
                {
                    Id = characteristic.Id,
                    Value = characteristic.Value ?? CharacteristicBase.GetDefault(characteristic.DefaultType),
                    AccessoryId = accessoryId
                };
                
                retCharactersitcs.Add(baseChar);
            }

            return new CharacteristicReturn<CharacteristicBase>
            {
                Characteristics = retCharactersitcs
            };
        }
    }
}
