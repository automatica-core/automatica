using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.TelegramMonitor;
using Automatica.Core.Driver;
using Microsoft.Extensions.Logging;
using P3.Driver.Knx.DriverFactory.ThreeLevel;
using Automatica.Core.Driver.Utility.Network;
using P3.Knx.Core.Driver;
using P3.Knx.Core.Driver.Tunneling;

namespace P3.Driver.Knx.DriverFactory
{
    public enum KnxLevel
    {
        ThreeLevel,
        TwoLevel
    }
    public class KnxDriver : DriverBase
    {
        private readonly bool _secureDriver;
        private readonly KnxLevel _level;
        private KnxConnection _tunneling;

        private KnxGatewayState _gwState;

        private Knx3Level _knxTree;
        private readonly object _lock = new object();


        private readonly Dictionary<string, List<Action<KnxDatagram>>> _callbackMap = new Dictionary<string, List<Action<KnxDatagram>>>();

        public KnxConnection Tunneling => _tunneling;

        public KnxDriver(IDriverContext driverContext, bool secureDriver, KnxLevel level=KnxLevel.ThreeLevel) : base(driverContext)
        {
            _secureDriver = secureDriver;
            _level = level;
            KnxHelper.Logger = driverContext.Logger;
        }

        protected override bool CreateTelegramMonitor()
        {
            return true;
        }

        public override bool Init()
        {
            var ipAddress = GetProperty("knx-ip").ValueString;
            var useNat = GetProperty("knx-use-nat").ValueBool;
            var port = GetPropertyValueInt("knx-port");

            try
            {
                var remoteIp = IPAddress.Parse(ipAddress);
                if (_secureDriver)
                {
                    throw new NotImplementedException();
                }
                else
                {
                    _tunneling = new KnxConnectionTunneling(remoteIp, port,
                        IPAddress.Parse(NetworkHelper.GetActiveIp()), NetworkHelper.GetFreeTcpPort());

                    if (useNat == null)
                    {
                        _tunneling.UseNat = false;
                    }
                    else
                    {
                        _tunneling.UseNat = useNat.Value;
                    }
                }
                
                _tunneling.OnDatagramReceived += KnxEventDelegate;
                _tunneling.OnConnected += KnxConnectedEvent;
                _tunneling.OnDisconnected+= KnxDisconnectedEvent;
            }
            catch (Exception e)
            {
                KnxHelper.Logger.LogError($"Could not init knx driver {e}");
                return false;
            }
            return true;
        }

        internal void Read(KnxGroupAddress ga)
        {
            _tunneling.Read(ga.GroupAddress);
        }

        public override Task<bool> Start()
        {
            _gwState?.SetGatewayState(false);
            StartConnection();
            return base.Start();
        }

        private void StartConnection()
        {
            _tunneling?.Start();
        }

        private void KnxDisconnectedEvent(object sender, EventArgs eventArgs)
        {
            lock (_lock)
            {
                KnxHelper.Logger.LogDebug($"GW  {Name} disconnected");
                _gwState?.SetGatewayState(false);

                _tunneling.Stop();
                Thread.Sleep(1000);

                _tunneling.Start();

            }
        }

        private void KnxConnectedEvent(object sender, EventArgs knxConnectedEventArgs)
        {
            KnxHelper.Logger.LogDebug($"GW {Name} connected");
            _gwState?.SetGatewayState(true);

            _knxTree?.ConnectionEstablished();
        }

        private void KnxEventDelegate(object sender, KnxDatgramEventArgs knxDatgramEventArgs)
        {
            KnxHelper.Logger.LogDebug($"Datagram on GA {knxDatgramEventArgs.Datagram.DestinationAddress}");

            TelegramMonitor.NotifyTelegram(TelegramDirection.Input, knxDatgramEventArgs.Datagram.SourceAddress, knxDatgramEventArgs.Datagram.DestinationAddress, knxDatgramEventArgs.Datagram.ToString(), Automatica.Core.Driver.Utility.Utils.ByteArrayToString(knxDatgramEventArgs.Datagram.Data));

            if (_callbackMap.ContainsKey(knxDatgramEventArgs.Datagram.DestinationAddress))
            {
                foreach (var ac in _callbackMap[knxDatgramEventArgs.Datagram.DestinationAddress])
                {
                    try
                    {
                        KnxHelper.Logger.LogDebug($"Datagram on GA {knxDatgramEventArgs.Datagram.DestinationAddress} - dispatch to {ac}");
                        ac.Invoke(knxDatgramEventArgs.Datagram);
                    }
                    catch(Exception e)
                    {
                        KnxHelper.Logger.LogError($"{e}");
                    }
                }
            }
            else
            {
                KnxHelper.Logger.LogWarning($"Datagram on GA - not callback registered");
            }
        }

        public override Task<bool> Stop()
        {
            if (_tunneling != null)
            {
                _tunneling.OnDatagramReceived -= KnxEventDelegate;
                _tunneling.OnConnected -= KnxConnectedEvent;
                _tunneling.OnDisconnected -= KnxDisconnectedEvent;
            }
            _callbackMap.Clear();
            _tunneling?.Stop();
            return base.Stop();
        }


        internal void AddGroupAddress(string groupAddress, Action<KnxDatagram> callback)
        {
            KnxHelper.Logger.LogDebug($"Register for value changes on GA {groupAddress}");
            if (!_callbackMap.ContainsKey(groupAddress))
            {
                _callbackMap.Add(groupAddress, new List<Action<KnxDatagram>>());
            }
            _callbackMap[groupAddress].Add(callback);
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            if (ctx.NodeInstance.This2NodeTemplateNavigation.Key == "knx-gw-state")
            {
                _gwState = new KnxGatewayState(ctx);
                return _gwState;
            }
            if (_level == KnxLevel.ThreeLevel)
            {
                _knxTree = new Knx3Level(ctx, this);
                return _knxTree;
            }

            return null;
        }
    }
}
