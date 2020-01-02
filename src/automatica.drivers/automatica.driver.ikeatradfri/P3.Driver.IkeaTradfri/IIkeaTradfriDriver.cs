using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using P3.Driver.IkeaTradfri.Models;

namespace P3.Driver.IkeaTradfri
{
    public interface IIkeaTradfriDriver
    {
        Task SwitchOn(long id);
        Task SwitchOff(long id);

        Task SetDimmer(long id, int dimmer);

        Task SetColor(long id, string color);

        List<TradfriDevice> LoadDevices();

        void Connect();

        void RegisterChange(Action<JToken> changeAction, TradfriDeviceType deviceType, long id);
    }
}
