using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using P3.Driver.HueBridge.Data;
using P3.Driver.HueBridge.EventArgs;
using Automatica.Core.Driver.Utility.Network;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace P3.Driver.HueBridge
{
    public class HueDriver
    {
        public const int DefaultPort = 5005;
        public const int MaxUsers = 32;
        public static readonly TimeSpan DefaultLinkButtonWindow = TimeSpan.FromMinutes(3);

        public event EventHandler<HueSwitchLightEventArgs> SwitchLight;

        /// <summary>
        /// Raised when the whitelist changed; persist <see cref="GetUserNames"/>
        /// </summary>
        public event EventHandler WhiteListChanged;

        private IHost _host;
        private readonly object _linkLock = new object();
        private DateTime _linkButtonOpenUntil = DateTime.MinValue;

        internal static HueDiscoveryService HueDiscoveryService { get; private set; }
        internal static HueDriver Instance { get; private set; }

        public BridgeConfig BridgeConfig { get; }

        internal ConcurrentDictionary<string, HueUser> WhiteList { get; } = new ConcurrentDictionary<string, HueUser>();

        public Guid BridgeId { get; } = Guid.NewGuid();

        public IDictionary<int, HueLight> Lights { get; } = new ConcurrentDictionary<int, HueLight>();

        public static Microsoft.Extensions.Logging.ILogger Logger { get; private set; } = NullLogger.Instance;

        public IPAddress BindAddress { get; }
        public int Port { get; }

        internal Func<DateTime> UtcNow { get; set; } = () => DateTime.UtcNow;

        public HueDriver(Microsoft.Extensions.Logging.ILogger logger) : this(logger, IPAddress.Any, DefaultPort)
        {
        }

        public HueDriver(Microsoft.Extensions.Logging.ILogger logger, IPAddress bindAddress, int port)
        {
            Instance = this;
            BindAddress = bindAddress ?? IPAddress.Any;
            Port = port is > 0 and <= 65535 ? port : DefaultPort;
            BridgeConfig = new BridgeConfig();
            BridgeConfig.Gateway = NetworkHelper.GetActiveIp();
            BridgeConfig.Netmask = NetworkHelper.GetActiveIpNetmask();
            BridgeConfig.BridgeId = BridgeId.ToString();
            BridgeConfig.Mac = NetworkHelper.GetActiveMacAddress();
            BridgeConfig.LinkButtonProvider = () => IsLinkButtonPressed;
            Logger = logger ?? NullLogger.Instance;
        }

        /// <summary>
        /// Opens the "link button" window. Only while it is open, POST /api whitelists a new user.
        /// </summary>
        public void PressLinkButton(TimeSpan? duration = null)
        {
            DateTime until;
            lock (_linkLock)
            {
                _linkButtonOpenUntil = UtcNow().Add(duration ?? DefaultLinkButtonWindow);
                until = _linkButtonOpenUntil;
            }

            Logger.LogInformation($"Hue link button pressed, new users are accepted until {until:u}");
        }

        public void ReleaseLinkButton()
        {
            lock (_linkLock)
            {
                _linkButtonOpenUntil = DateTime.MinValue;
            }
        }

        public bool IsLinkButtonPressed
        {
            get
            {
                lock (_linkLock)
                {
                    return UtcNow() < _linkButtonOpenUntil;
                }
            }
        }

        public HueLight SetLightState(int light, HueSwitchLightData state, bool invoke)
        {
            if (!Lights.TryGetValue(light, out var hueLight))
            {
                return null;
            }

            if (state.On.HasValue)
            {
                var on = state.On.Value;
                var bri = state.Bri ?? 0;
                if (on && bri == 0)
                {
                    bri = 100;
                }

                if (invoke)
                {
                    SwitchLight?.Invoke(this, new HueSwitchLightEventArgs(hueLight, on, bri));
                }

                // HueLightState couples on and bri (setting one derives the other), so set "on" first.
                hueLight.State.On = on;
                if (on)
                {
                    hueLight.State.Bri = bri;
                }
            }
            else if (state.Bri.HasValue && hueLight.State.On)
            {
                // Brightness-only request (e.g. {"bri":127}): adjust brightness of a light that is on,
                // never switch it. Like a real bridge, brightness of a light that is off is ignored.
                hueLight.State.Bri = Math.Max(1, state.Bri.Value);
            }

            return hueLight;
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

        /// <summary>
        /// Loads persisted user names (e.g. from the driver settings)
        /// </summary>
        public void LoadUsers(IEnumerable<string> userNames)
        {
            if (userNames == null)
            {
                return;
            }

            foreach (var name in userNames.Where(IsValidUserName).Take(MaxUsers))
            {
                WhiteList.TryAdd(name, new HueUser { Id = name, Created = DateTime.Now, LastUsed = DateTime.Now });
            }
        }

        public IReadOnlyList<string> GetUserNames()
        {
            return WhiteList.Keys.ToList();
        }

        internal static bool IsValidUserName(string name)
        {
            return !string.IsNullOrWhiteSpace(name) && name.Length >= 10 && name.Length <= 64 &&
                   name.All(c => char.IsLetterOrDigit(c) || c == '-');
        }

        /// <summary>
        /// Creates a new whitelisted user if the link button window is open and the whitelist is not full.
        /// </summary>
        internal bool TryCreateUser(out string userName)
        {
            userName = null;

            if (!IsLinkButtonPressed)
            {
                Logger.LogWarning("Hue user creation rejected, link button not pressed");
                return false;
            }

            if (WhiteList.Count >= MaxUsers)
            {
                Logger.LogWarning($"Hue user creation rejected, the whitelist is full ({MaxUsers})");
                return false;
            }

            userName = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();
            if (!WhiteList.TryAdd(userName, new HueUser { Id = userName, Created = DateTime.Now, LastUsed = DateTime.Now }))
            {
                return false;
            }

            Logger.LogInformation("New Hue user whitelisted");
            WhiteListChanged?.Invoke(this, System.EventArgs.Empty);
            return true;
        }

        internal bool IsAuthorized(string userName)
        {
            if (string.IsNullOrEmpty(userName) || !WhiteList.TryGetValue(userName, out var user))
            {
                return false;
            }

            user.LastUsed = DateTime.Now;
            return true;
        }

        public void RemoveAllUsers()
        {
            WhiteList.Clear();
            WhiteListChanged?.Invoke(this, System.EventArgs.Empty);
        }

        public async Task Start()
        {
            _host = BuildWebHost();

            HueDiscoveryService = new HueDiscoveryService(Port, Logger);

            await HueDiscoveryService.StartAsync();
            await _host.RunAsync();
        }

        public async Task Stop()
        {
            HueDiscoveryService?.Stop();
            HueDiscoveryService = null;

            if (_host != null)
            {
                await _host.StopAsync();
                _host.Dispose();
                _host = null;
            }
        }

        public IHost BuildWebHost()
        {
            string host;
            if (BindAddress.Equals(IPAddress.Any))
            {
                host = "*";
            }
            else if (BindAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
            {
                host = $"[{BindAddress}]";
            }
            else
            {
                host = BindAddress.ToString();
            }

            // the web host must not touch the global (Serilog) logger of the core process
            return Host.CreateDefaultBuilder()
                .ConfigureLogging(logging => logging.ClearProviders())
                .ConfigureServices(services => services.AddSingleton(this))
                .ConfigureWebHostDefaults(webHost => webHost
                    .UseKestrel(options => options.Limits.MaxRequestBodySize = 16 * 1024)
                    .UseStartup<HueStartup>()
                    .UseUrls($"http://{host}:{Port}/"))
                .Build();
        }
    }
}
