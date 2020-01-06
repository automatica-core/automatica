using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tomidix.NetStandard.Tradfri;
using Tomidix.NetStandard.Tradfri.Models;
using Zeroconf;

namespace P3.Driver.IkeaTradfri
{
    public class IkeaTradfriDriver : IIkeaTradfriDriver
    {
        private readonly string _gatewayIp;
        private readonly string _name;
        private readonly string _appKey;

        
        private TradfriController _tradfri;

        public IkeaTradfriDriver(string gatewayIp, string name, string appKey)
        {
            _gatewayIp = gatewayIp;
            _name = name;
            _appKey = appKey;
        }

        public Task Connect()
        {
            _tradfri = new TradfriController(_name, _gatewayIp);
            _tradfri.ConnectAppKey(_appKey, _name);

            return Task.CompletedTask;
        }

        public Task Disconnect()
        {
            return Task.CompletedTask;
        }

        public async Task RegisterChange(Action<TradfriDevice> changeAction, long id)
        {
            var device = await _tradfri.DeviceController.GetTradfriDevice(id);
            _tradfri.DeviceController.ObserveDevice(device, changeAction); 
        }

        public static async Task<IEnumerable<Tuple<string, string>>> Discover()
        {
            var ret = new List<Tuple<string, string>>();
            var responses = await ZeroconfResolver.ResolveAsync("_coap._udp.local.");
            foreach (var resp in responses)
            {
                ret.Add(new Tuple<string, string>(resp.DisplayName, resp.IPAddress));
            }

            return ret;
        }

        public static TradfriAuth GeneratePsk(string gatewayIp, string appName, string secret)
        {
            try
            {
                var controller = new TradfriController(appName, gatewayIp);
                return controller.GenerateAppSecret(secret, appName);
            }
            catch (Exception e)
            {
                throw;
            }
        }
        
        public async Task<List<TradfriDevice>> LoadDevices()
        {
            return await _tradfri.GatewayController.GetDeviceObjects();
        }

        public Task<TradfriDevice> GetDevice(long id)
        {
            return _tradfri.DeviceController.GetTradfriDevice(id);
        }

        public async Task<bool> SwitchOn(long id)
        {
            await _tradfri.DeviceController.SetOutlet(id, true);

            return true;
        }
        public async Task<bool> SwitchOff(long id)
        {
            await _tradfri.DeviceController.SetOutlet(id, false);

            return true;
        }

        public async Task<bool> SetColor(long id, string color)
        {
            await _tradfri.DeviceController.SetColor(id, color);

            return true;
        }
        public async Task<bool> SetDimmer(long id, int dimmer)
        {
            await _tradfri.DeviceController.SetDimmer(id, dimmer);

            return true;
        }
    }
}
