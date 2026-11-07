using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Automatica.Core.Base.Common;
using Microsoft.Extensions.Logging;
using P3.Driver.HomeKit.Bonjour;
using P3.Driver.HomeKit.Hap;
using P3.Driver.HomeKit.Hap.EventArgs;
using P3.Driver.HomeKit.Hap.Model;

[assembly:InternalsVisibleTo("P3.Driver.HomeKit.UnitTests")]

namespace P3.Driver.HomeKit
{
    public class HomeKitServer : IHomeKitServer
    {
        internal const string Libsodium = "libsodium";

        public string Manufacturer { get; }
        public string BridgeName { get; }

        private readonly BonjourService _bonjour;
        private readonly HapControllerServer _hapServer;
        public event EventHandler<PairSetupCompleteEventArgs> PairingCompleted;
        public event EventHandler<CharactersiticValueChangedEventArgs> ValueChanged;
        
        private readonly AccessoryContainer _accessoryContainer = new AccessoryContainer();

        private readonly Dictionary<string, List<Characteristic>> _eventBasedNotifications =
            new Dictionary<string, List<Characteristic>>();

        public AccessoryContainer AccessoryContainer => _accessoryContainer;

        public HomeKitServer(ILogger logger, int port, string name, string ltsk, string ltpk, string deviceId, string pairCode, string manufacturer, string bridgeName, int configVersion, string firmwareRevision)
        {
            if (!HomeKitSetup.IsSetupCodeValid(pairCode))
            {
                throw new ArgumentException($"{nameof(pairCode)} is not valid...");
            }

            if (configVersion <= 0)
            {
                configVersion = 1;
            }

            Manufacturer = manufacturer;
            BridgeName = bridgeName;
            var hapPort = port;
            _bonjour = new BonjourService(logger, Convert.ToUInt16(hapPort), name, deviceId, configVersion);
            _hapServer = new HapControllerServer(logger, this, hapPort, ltsk, ltpk, deviceId, pairCode);

            _bonjour.AlreadyPaired = _hapServer.PairingStore.IsPaired;

            _hapServer.PairingCompleted += HapServerOnPairingCompleted;

            var bridgeAccessory = new Accessory
            {
                Id = 1
            };
            bridgeAccessory.AccessoryInfo = AccessoryFactory.CreateAccessoryInfo(bridgeAccessory, 1, bridgeName,
                manufacturer, ServerInfo.ServerUid.ToString(), firmwareRevision);
            bridgeAccessory.Services.Add(bridgeAccessory.AccessoryInfo);
            bridgeAccessory.Services.Add(AccessoryFactory.CreateHapServiceInformation(bridgeAccessory, 8));
            _accessoryContainer.AddAccessory(bridgeAccessory);

        }


        [DllImport(Libsodium, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int sodium_library_version_major();

        [DllImport(Libsodium, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int sodium_library_version_minor();

        public static void Init(ILogger logger)
        {
            //extract libsodium dll

            var myPath = new FileInfo(Assembly.GetExecutingAssembly().Location);
            var corePath = new FileInfo(Assembly.GetExecutingAssembly().Location);

            var rid = ServerInfo.Rid.Replace("-", "_");
            var resources = Assembly.GetExecutingAssembly().GetManifestResourceNames();
            var embeddedResource = resources
                .SingleOrDefault(a => a.Contains(rid));

            if (string.IsNullOrEmpty(embeddedResource))
            {
                throw new NotSupportedException($"libsodium seems not avaialable for {rid}");
            }

            var fileName = embeddedResource.Replace($"P3.Driver.HomeKit.Sodium.{rid}.native.", "");

            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(embeddedResource))
            {
                using (var file = new FileStream(Path.Combine(myPath.DirectoryName, fileName), FileMode.Create, FileAccess.Write))
                {
                    stream?.CopyTo(file);
                }
            }

            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(embeddedResource))
            {
                using (var file = new FileStream(Path.Combine(corePath.DirectoryName, fileName), FileMode.Create,
                    FileAccess.Write))
                {
                    stream?.CopyTo(file);
                }
            }

            logger.LogInformation($"Loading sodium version {sodium_library_version_major()}.{sodium_library_version_minor()}");
        }

        internal List<Accessory> GetAccessories()
        {
            return _accessoryContainer.Accessories;
        }

        internal void RegisterNotifications(Characteristic characteristic, HapSession session)
        {
            lock (_eventBasedNotifications)
            {
                if (!_eventBasedNotifications.ContainsKey(session.ClientUsername))
                {
                    _eventBasedNotifications.Add(session.ClientUsername, new List<Characteristic>());
                }

                if (!_eventBasedNotifications[session.ClientUsername].Contains(characteristic))
                {
                    _eventBasedNotifications[session.ClientUsername].Add(characteristic);
                }
            }
        }

        internal void UnregisterNotifications(Characteristic characteristic, HapSession session)
        {
            lock (_eventBasedNotifications)
            {
                if (_eventBasedNotifications.TryGetValue(session.ClientUsername, out var characteristics))
                {
                    characteristics.Remove(characteristic);
                }
            }
        }

        private void HapServerOnPairingCompleted(object sender, PairSetupCompleteEventArgs e)
        {
            _bonjour.AlreadyPaired = _hapServer.PairingStore.IsPaired;
            PairingCompleted?.Invoke(sender, e);
        }

        /// <summary>
        /// The paired state announced over mDNS ("sf" TXT record), follows <see cref="HapPairingStore.IsPaired"/>.
        /// </summary>
        internal bool AdvertisedAsPaired => _bonjour.AlreadyPaired;

        internal HapPairingStore PairingStore => _hapServer.PairingStore;

        public async Task<bool> Start()
        {
            await _bonjour.Start();
            await _hapServer.Start();
           
            return true;
        }

        public bool IsPaired => _hapServer.PairingStore.IsPaired;

        public static string GenerateSetupCode()
        {
            return HomeKitSetup.GenerateSetupCode();
        }

        /// <summary>
        /// Removes all controller pairings so the bridge can be paired again. The store raises <see cref="PairingCompleted"/>
        /// with the cleared state (persisted by the owner) and the bridge is advertised as pairable again.
        /// </summary>
        public void ClearPairings()
        {
            _hapServer.ClearPairings();
        }

        public ulong AddAccessory(Accessory accessory)
        {
            _accessoryContainer.AddAccessory(accessory);
            return accessory.Id;
        }

        public void SetCharacteristicValue(Characteristic characteristic, object value)
        {
            var found = false;
            foreach (var a in _accessoryContainer.Accessories)
            {
                foreach (var s in a.Services)
                {
                    foreach (var c in s.Characteristics)
                    {
                        if (c == characteristic)
                        {
                            c.Value = value;
                            found = true;
                        }
                    }
                }
            }

            if (!found)
            {
                return;
            }

            // snapshot the subscribers under the lock and send outside of it: sending takes the session lock, which a
            // connection holds while it handles a request that (un)registers notifications => lock order inversion
            List<string> subscribers;
            lock (_eventBasedNotifications)
            {
                subscribers = _eventBasedNotifications.Where(a => a.Value.Contains(characteristic)).Select(a => a.Key).ToList();
            }

            foreach (var clientUsername in subscribers)
            {
                _hapServer.SendNotification(characteristic, _hapServer.GetClientSession(clientUsername));
            }
        }

        public async Task<bool> Stop()
        {
            _hapServer.PairingCompleted -= HapServerOnPairingCompleted;

            await _bonjour.Stop();
            await _hapServer.Stop();

            return true;
        }

        internal void NotifyValueChanged(Characteristic characteristic)
        {
            ValueChanged?.Invoke(this, new CharactersiticValueChangedEventArgs(characteristic, characteristic.Value));
        }

    }
}
