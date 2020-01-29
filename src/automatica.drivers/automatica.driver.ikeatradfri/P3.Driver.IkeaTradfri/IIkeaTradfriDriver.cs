using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tomidix.NetStandard.Tradfri.Models;

namespace P3.Driver.IkeaTradfri
{
    public interface IIkeaTradfriDriver
    {
        Task<bool> SwitchOn(long id);
        Task<bool> SwitchOff(long id);

        Task<bool> SetDimmer(long id, int dimmer);

        Task<bool> SetColor(long id, string color);

        Task<List<TradfriDevice>> LoadDevices();

        Task Connect();
        Task Disconnect();
        Task RegisterChange(Action<TradfriDevice> changeAction, long id);
        Task<TradfriDevice> GetDevice(long containerDeviceId);
    }
}
