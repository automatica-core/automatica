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
        
        private readonly AccessoryData _accessory = new AccessoryData();

        private readonly Dictionary<string, List<Characteristic>> _eventBasedNotifications =
            new Dictionary<string, List<Characteristic>>();

        public HomeKitServer(ILogger logger, int port, string name, string ltsk, string ltpk, string deviceId, string pairCode, string manufacturer, string bridgeName, int configVersion)
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

            _bonjour.AlreadyPaired = !string.IsNullOrEmpty(ltpk);

            _hapServer.PairingCompleted += HapServerOnPairingCompleted;

            var bridgeAccessory = new Accessory
            {
                Id = 1
            };
            bridgeAccessory.Services.Add(AccessoryFactory.CreateAccessoryInfo(bridgeAccessory, 1, bridgeName, manufacturer, ServerInfo.ServerUid.ToString()));
            _accessory.Accessories.Add(bridgeAccessory);

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
            return _accessory.Accessories;
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

        private void HapServerOnPairingCompleted(object sender, PairSetupCompleteEventArgs e)
        {
            PairingCompleted?.Invoke(sender, e);
            _bonjour.AlreadyPaired = true;
        }

        public async Task<bool> Start()
        {
            await _bonjour.Start();
            await _hapServer.Start();
           
            return true;
        }

        public int AddAccessory(Accessory accessory)
        {
            accessory.Id = _accessory.Accessories.Count + 1;

            _accessory.Accessories.Add(accessory);
            return accessory.Id;
        }

        public void SetCharacteristicValue(Characteristic characteristic, object value)
        {
            lock (_eventBasedNotifications)
            {
                foreach (var a in _accessory.Accessories)
                {
                    foreach (var s in a.Services)
                    {
                        foreach (var c in s.Characteristics)
                        {
                            if (c == characteristic)
                            {
                                c.Value = value;

                                foreach(var sessions in _eventBasedNotifications) {
                                    if (sessions.Value.Contains(characteristic))
                                    {
                                        _hapServer.SendNotification(characteristic, _hapServer.GetClientSession(sessions.Key));
                                    }
                                }

                            }
                        }
                    }
                }
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
