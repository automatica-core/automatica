using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using P3.Driver.HueBridge.Data;
using P3.Driver.HueBridge.EventArgs;
using Automatica.Core.Driver.Utility.Network;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;

namespace P3.Driver.HueBridge
{
    public class HueDriver
    {
        public event EventHandler<HueSwitchLightEventArgs> SwitchLight;

        private IWebHost _host;

        internal static HueDiscoveryService HueDiscoveryService { get; private set; }
        internal static HueDriver Instance { get; private set; }

        public BridgeConfig BridgeConfig { get; }

        public Dictionary<string, HueUser> WhiteList { get; } = new Dictionary<string, HueUser>();

        public Guid BridgeId { get; } = Guid.NewGuid();

        public Dictionary<int, HueLight> Lights { get; } = new Dictionary<int, HueLight>();

        public static Microsoft.Extensions.Logging.ILogger Logger { get; private set; } = NullLogger.Instance;

        public HueDriver(Microsoft.Extensions.Logging.ILogger logger)
        {
            Instance = this;
            BridgeConfig = new BridgeConfig();
            BridgeConfig.Gateway = NetworkHelper.GetActiveIp();
            BridgeConfig.Netmask = NetworkHelper.GetActiveIpNetmask();
            BridgeConfig.BridgeId = BridgeId.ToString();
            BridgeConfig.Mac = NetworkHelper.GetActiveMacAddress();
            Logger = logger;
        }

        public HueLight SetLightState(int light, HueSwitchLightData state, bool invoke)
        {
            if (invoke)
            {
                SwitchLight?.Invoke(this, new HueSwitchLightEventArgs(Lights[light], state.On, state.Bri));
            }

            Lights[light].State.On = state.On;
            if (state.On && state.Bri == 0)
            {
                state.Bri = 100;
            }
            Lights[light].State.Bri = state.Bri;

            return Lights[light];
        }

        internal void SetAll(HueSwitchLightData data)
        {
            foreach(var light in Lights.Values)
            {
                SetLightState(light.Id, data, true);
            }
        }

        public int AddLight(HueLight light)
        {
            var nextId = Lights.Count + 1;
            light.Id = nextId;
            Lights.Add(nextId, light);
            return nextId;
        }

        public void AddUser(HueUser user)
        {
            if(!WhiteList.ContainsKey(user.Id)) { 
                WhiteList.Add(user.Id, user);
            }
        }

        public async Task Start()
        {
            _host = BuildWebHost(5005);

            HueDiscoveryService = new HueDiscoveryService(5005, Logger);

            await HueDiscoveryService.StartAsync();
            await _host.RunAsync();
        }

        public async Task Stop()
        {
            await _host.StopAsync();
        }

        public IWebHost BuildWebHost(int port)
        {
            var args = new string[0];

            var webHost = WebHost.CreateDefaultBuilder(args)
                .UseStartup<HueStartup>()
                .UseUrls($"http://*:{port}/");

            return webHost.Build();
        }

  
    }
}
