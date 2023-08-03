using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.Cryptography;
using Automatica.Core.Base.TelegramMonitor;
using Automatica.Core.Base.Tunneling;
using Automatica.Core.Driver;
using Microsoft.Extensions.Logging;
using P3.Driver.Knx.DriverFactory.ThreeLevel;
using Automatica.Core.Driver.Utility.Network;
using P3.Knx.Core.Driver;
using P3.Knx.Core.Driver.Tunneling;
using P3.Knx.Core.Abstractions;
using Automatica.Core.EF.Exceptions;

namespace P3.Driver.Knx.DriverFactory.Factories.IpTunneling
{
    public enum KnxLevel
    {
        ThreeLevel,
        TwoLevel
    }
    public class KnxDriver : DriverBase, IKnxDriver, IKnxEvents
    {
        private readonly bool _secureDriver;
        private readonly KnxLevel _level;
        private KnxConnection _tunneling;

        private KnxGatewayState _gwState;

        private Knx3Level _knxTree;
        private readonly object _lock = new object();


        private readonly Dictionary<string, List<Action<KnxDatagram>>> _callbackMap = new Dictionary<string, List<Action<KnxDatagram>>>();
        private bool _tunnelingEnabled;
        private IPAddress _remoteIp;
        private int _remotePort;
        private bool _onlyUseTunnel;

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

        protected override bool CreateCustomLogger()
        {
            return true;
        }

        public override async Task<bool> Init(CancellationToken token = default)
        {
            var ipAddress = GetProperty("knx-ip").ValueString;
            var useNat = GetProperty("knx-use-nat").ValueBool;
            var port = GetPropertyValueInt("knx-port");

            if (String.IsNullOrEmpty(ipAddress))
            {
                DriverContext.Logger.LogError($"IP Address cannot be empty!");
                return false;
            }


            var useTunnel = GetProperty("knx-use-tunnel").ValueBool;

            try
            {
                var onlyUseTunnelProp = GetProperty("knx-only-use-tunnel");

                if (onlyUseTunnelProp != null && onlyUseTunnelProp.ValueBool.HasValue)
                    _onlyUseTunnel = onlyUseTunnelProp.ValueBool!.Value;

            }
            catch (PropertyNotFoundException)
            {
                //ignore, default value is false anyway
            }

            try
            {
                var remoteIp = IPAddress.Parse(ipAddress);
                if (_secureDriver)
                {
                    throw new NotImplementedException();
                }
                else
                {
                    _remoteIp = remoteIp;
                    _remotePort = port;

                    _tunneling = new KnxConnectionTunneling(this, remoteIp, port,
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

                if (useTunnel.HasValue && useTunnel.Value)
                {
                    DriverContext.Logger.LogInformation($"Using tunneling mode...");
                    _tunnelingEnabled = true;
                }
                else
                {
                    DriverContext.Logger.LogInformation($"Using routing mode...");
                }
            }
            catch (Exception e)
            {
                DriverContext.Logger.LogError($"Could not init knx driver {e}");
                return false;
            }

            await InitRemoteConnect(token);

            DriverContext.Logger.LogInformation($"Init done...");

            return await base.Init(token);
        }

        private async Task InitRemoteConnect(CancellationToken token = default)
        {
            try
            {
                var remoteFeatureEnabled =
                    DriverContext.LicenseContract.IsFeatureLicensed("knx-interface-remote-connection");
                if (remoteFeatureEnabled && _tunnelingEnabled && await DriverContext.TunnelingProvider.IsAvailableAsync(default))
                {
                    var tunnel = await DriverContext.TunnelingProvider.CreateTunnelAsync(TunnelingProtocol.Udp, "knx", $"{_remoteIp}", _remotePort,
                        token);

                    DriverContext.Logger.LogInformation($"Tunnel created {tunnel}");
                }
                else
                {
                    DriverContext.Logger.LogInformation($"Tunnel is disabled...");
                }
            }

            catch (Exception e)
            {
                DriverContext.Logger.LogError($"Could not start tunnel {e}");
            }

        }

        public override async Task<bool> Start(CancellationToken token = default)
        {
            if (_onlyUseTunnel)
            {
                return true;
            }
            _gwState?.SetGatewayState(false);
            StartConnection();

            return await base.Start(token);
        }

        private void StartConnection()
        {
            _tunneling?.Start();
        }

        public override Task<bool> Stop(CancellationToken token = default)
        {
            lock (_lock)
            {
                DriverContext.Logger.LogInformation($"Stopping KNX driver...");
                if (_tunneling != null)
                {
                    _tunneling.Stop();
                    _callbackMap.Clear();
                    _tunneling = null;
                }
            }
            return base.Stop(token);
        }

        internal void AddGroupAddress(string groupAddress, Action<KnxDatagram> callback)
        {
            DriverContext.Logger.LogDebug($"Register for value changes on GA {groupAddress}");
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

        public void AddAddressNotifier(string address, Action<object> callback)
        {
            AddGroupAddress(address, callback);
        }

        public Task<bool> Read(string address)
        {
            _tunneling.Read(address);
            return Task.FromResult(true);
        }

        public Task<bool> Write(string address, ReadOnlyMemory<byte> data)
        {
            DriverContext.Logger.LogDebug($"Write datagram on GA {address}");

            _tunneling.Write(address, data.ToArray());

            return Task.FromResult(true);
        }

        public Task Connected()
        {
            DriverContext.Logger.LogDebug($"GW {Name} connected");
            _gwState?.SetGatewayState(true);

            _knxTree?.ConnectionEstablished();

            return Task.CompletedTask;
        }

        public Task Disconnected()
        {
            lock (_lock)
            {
                DriverContext.Logger.LogDebug($"GW  {Name} disconnected");
                _gwState?.SetGatewayState(false);

                _tunneling.Stop();
                Thread.Sleep(1000);

                _tunneling.Start();

            }

            return Task.CompletedTask;
        }

        public Task OnDatagram(KnxDatagram datagram)
        {
            DriverContext.Logger.LogDebug($"Datagram on GA {datagram.DestinationAddress}");

            TelegramMonitor.NotifyTelegram(TelegramDirection.Input, datagram.SourceAddress, datagram.DestinationAddress, datagram.ToString(), Automatica.Core.Driver.Utility.Utils.ByteArrayToString(datagram.Data.AsSpan()));

            if (_callbackMap.ContainsKey(datagram.DestinationAddress))
            {
                foreach (var ac in _callbackMap[datagram.DestinationAddress])
                {
                    try
                    {
                        DriverContext.Logger.LogDebug($"Datagram on GA {datagram.DestinationAddress} - dispatch to {ac}");
                        ac.Invoke(datagram);
                    }
                    catch (Exception e)
                    {
                        DriverContext.Logger.LogError($"{datagram.DestinationAddress} ({datagram.Datagram.ToHex(true)}): {e}");
                    }
                }
            }
            else
            {
                DriverContext.Logger.LogInformation($"Datagram on GA {datagram.DestinationAddress} - not callback registered");
            }

            return Task.CompletedTask;
        }
    }
}
