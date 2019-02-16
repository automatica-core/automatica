﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Automatica.Core.Base.Common;
using Microsoft.Extensions.Logging;
using P3.Driver.HomeKit.Bonjour;
using P3.Driver.HomeKit.Hap;
using P3.Driver.HomeKit.Hap.EventArgs;
using P3.Driver.HomeKit.Hap.Model;

namespace P3.Driver.HomeKit
{
    public class HomeKitServer : IHomeKitServer
    {
        public string Manufacturer { get; }
        public string BridgeName { get; }

        private readonly BonjourService _bonjour;
        private readonly HapControllerServer _hapServer;
        public event EventHandler<PairSetupCompleteEventArgs> PairingCompleted;
        public event EventHandler<CharactersiticValueChangedEventArgs> ValueChanged;
        

        private readonly AccessoryData _accessory = new AccessoryData();

        private readonly Dictionary<Characteristic, List<HapSession>> _eventBasedNotifications = new Dictionary<Characteristic, List<HapSession>>();

        public HomeKitServer(ILogger logger, int port, string name, string ltsk, string ltpk, string deviceId, string pairCode, string manufacturer, string bridgeName)
        {
            Manufacturer = manufacturer;
            BridgeName = bridgeName;
            var hapPort = port;
            _bonjour = new BonjourService(logger, hapPort, name);
            _hapServer = new HapControllerServer(logger, this, hapPort, ltsk, ltpk, deviceId, pairCode);

            _hapServer.PairingCompleted += HapServerOnPairingCompleted;

            var bridgeAccessory = new Accessory
            {
                Id = 1
            };
            bridgeAccessory.Services.Add(AccessoryFactory.CreateAccessoryInfo(bridgeAccessory, 1, bridgeName, manufacturer, ServerInfo.ServerUid.ToString()));
            _accessory.Accessories.Add(bridgeAccessory);

        }

        public void SetConfigVersion(int version)
        {
            HapControllerServer.ConfigVersion = version;
        }

        public static void Init()
        {
            //extract libsodium dll

            var myPath = new FileInfo(Assembly.GetExecutingAssembly().Location);
            var corePath = new FileInfo(Assembly.GetExecutingAssembly().Location);

            var rid = ServerInfo.Rid.Replace("-", "_");
            var embeddedResource = Assembly.GetExecutingAssembly().GetManifestResourceNames()
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
                    stream.CopyTo(file);
                }
            }

            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(embeddedResource))
            {
                using (var file = new FileStream(Path.Combine(corePath.DirectoryName, fileName), FileMode.Create,
                    FileAccess.Write))
                {
                    stream.CopyTo(file);
                }
            }
        }

        internal List<Accessory> GetAccessories()
        {
            return _accessory.Accessories;
        }

        internal void RegisterNotifications(Characteristic characteristic, HapSession session)
        {
            lock (_eventBasedNotifications)
            {
                if (!_eventBasedNotifications.ContainsKey(characteristic))
                {
                    _eventBasedNotifications.Add(characteristic, new List<HapSession>());
                }

                if (!_eventBasedNotifications[characteristic].Contains(session))
                {
                    _eventBasedNotifications[characteristic].Add(session);
                }
            }
        }

        private void HapServerOnPairingCompleted(object sender, PairSetupCompleteEventArgs e)
        {
            PairingCompleted?.Invoke(sender, e);
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

                                if (_eventBasedNotifications.ContainsKey(characteristic))
                                {
                                    _hapServer.SendNotificiation(characteristic,
                                        _eventBasedNotifications[characteristic]);
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
