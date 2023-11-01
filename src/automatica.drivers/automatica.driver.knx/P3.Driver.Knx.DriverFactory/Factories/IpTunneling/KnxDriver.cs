using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.TelegramMonitor;
using Automatica.Core.Base.Tunneling;
using Automatica.Core.Driver;
using Microsoft.Extensions.Logging;
using P3.Driver.Knx.DriverFactory.ThreeLevel;
using Automatica.Core.EF.Exceptions;
using Knx.Falcon;
using Knx.Falcon.Sdk;
using Knx.Falcon.Configuration;
using Knx.Falcon.Logging;
using System.Security;
using Automatica.Core.Base.Cryptography;
using Docker.DotNet.Models;
using P3.Driver.Knx.DriverFactory.Logging;

namespace P3.Driver.Knx.DriverFactory.Factories.IpTunneling
{
    public enum KnxLevel
    {
        ThreeLevel,
        TwoLevel
    }
    public class KnxDriver : DriverNoneAttributeBase
    {
        private readonly bool _secureDriver;
        private readonly KnxLevel _level;
        private KnxBus _tunneling;

        private KnxGatewayState _gwState;

        private Knx3Level _knxTree;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);


        private readonly Dictionary<string, List<Action<GroupEventArgs>>> _callbackMap = new();
        private readonly Dictionary<string, KnxGroupAddress> _gaMap = new();

        private readonly Dictionary<string, GroupValue> _lastGaValues = new();

        private bool _tunnelingEnabled;
        private IPAddress _remoteIp;
        private int _remotePort;
        private bool _onlyUseTunnel;

        public KnxDriver(IDriverContext driverContext, bool secureDriver, KnxLevel level=KnxLevel.ThreeLevel) : base(driverContext)
        {
            _secureDriver = secureDriver;
            _level = level;
            Logger.Factory = new FalconLoggerFactory(DriverContext.LoggerFactory.CreateLogger("KNXFalcon"));
        }

        protected override bool CreateTelegramMonitor()
        {
            return true;
        }

        protected override bool CreateCustomLogger()
        {
            return true;
        }

        private static SecureString PasswordToSecureString(string password)
        {
            var secureString = new SecureString();

            foreach (char c in password)
                secureString.AppendChar(c);
            return secureString;

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
                await ConstructTunnelingConnection();

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

        private async Task ConstructTunnelingConnection()
        {
            DriverContext.Logger.LogInformation($"Construct KNX driver...");
            await _semaphore.WaitAsync();
            try
            {
                var ipAddress = GetProperty("knx-ip").ValueString;
                var useNat = GetProperty("knx-use-nat").ValueBool;
                var port = GetPropertyValueInt("knx-port");

                var remoteIp = IPAddress.Parse(ipAddress);
                _remoteIp = remoteIp;
                _remotePort = port;
                var useNatValue = false;
                useNatValue = useNat != null && useNat.Value;
                var ip = new IpTunnelingConnectorParameters(ipAddress, ipPort: port, useNat: useNatValue)
                {
                    AutoReconnect = true
                };

                if (_secureDriver)
                {
                    var authPw = GetPropertyValueString("knx-auth-pw");
                    var userPw = GetPropertyValueString("knx-user-pw");
                    var userId = GetPropertyValueInt("knx-user-id");
                    var iaAddress = GetPropertyValueString("knx-ia-address");

                    ip.IndividualAddress = IndividualAddress.Parse(iaAddress);
                    ip.DeviceAuthenticationCodeHash =
                        IpUnicastConnectorParameters.GetDeviceAuthenticationCodeHash(PasswordToSecureString(authPw));
                    ip.UserPasswordHash =
                        IpUnicastConnectorParameters.GetUserPasswordHash(PasswordToSecureString(userPw));
                    ip.UserId = (byte)userId;
                }

                _tunneling = new KnxBus(ip);
                _tunneling.ConnectionStateChanged += _tunneling_ConnectionStateChanged;
                _tunneling.GroupMessageReceived += _tunneling_GroupMessageReceived;
            }
            finally
            {
                _semaphore.Release();
            }
            DriverContext.Logger.LogInformation($"Construct KNX driver...done");
        }

        private async void _tunneling_GroupMessageReceived(object sender, GroupEventArgs e)
        {
            DriverContext.Logger.LogDebug($"Datagram on GA {e.DestinationAddress} {e.EventType}");

            if (e.Value is { Value: not null })
            {
                await TelegramMonitor.NotifyTelegram(TelegramDirection.Input, e.SourceAddress, e.DestinationAddress,
                    e.Value.Value.ToHex(true), Automatica.Core.Driver.Utility.Utils.ByteArrayToString(e.Value.Value.AsSpan()));
            }

            if (e.EventType == GroupEventType.ValueRead)
            {
                var ga = e.DestinationAddress.ToString()!;
                DriverContext.Logger.LogDebug($"Datagram on GA {e.EventType} {e.DestinationAddress}");
                if (_gaMap.TryGetValue(ga, out var groupAddress) && _lastGaValues.TryGetValue(ga, out var gaValue))
                {
                    DriverContext.Logger.LogDebug($"Answer read request on GA {e.DestinationAddress}");

                    await _tunneling.RespondGroupValueAsync(GroupAddress.Parse(ga), gaValue);
                }
            }
            else
            {
                if (_callbackMap.TryGetValue(e.DestinationAddress, out var value))
                {
                    foreach (var ac in value)
                    {
                        try
                        {

                            DriverContext.Logger.LogDebug($"Datagram on GA {e.DestinationAddress}  {e.Value.Value.ToHex(false)} - dispatch to {ac}");
                            ac.Invoke(e);

                        }
                        catch (Exception ex)
                        {
                            DriverContext.Logger.LogError($"{e.DestinationAddress}: {ex}");
                        }
                    }
                }
                else
                {
                    DriverContext.Logger.LogInformation(
                        $"Datagram on GA {e.DestinationAddress} - no callback registered");
                }
            }

        }

        private async void _tunneling_ConnectionStateChanged(object sender, EventArgs e)
        {
            DriverContext.Logger.LogError($"Connection state changed to {_tunneling.ConnectionState}");
             if (_tunneling != null)
            {
                var state = _tunneling.ConnectionState == BusConnectionState.Connected;
                _gwState?.SetGatewayState(state);

                if (!state)
                {
                    DriverContext.Logger.LogDebug($"GW {Name} disconnected, try to reconnect");

                    await DisposeConnection();
                    await ConstructTunnelingConnection();
                    await StartConnection();

                    DriverContext.Logger.LogDebug($"State is now {_tunneling.ConnectionState}");
                }
            }
        }

        private async Task InitRemoteConnect(CancellationToken token = default)
        {
            try
            {
                var remoteFeatureEnabled =
                    DriverContext.LicenseContract.IsFeatureLicensed("knx-interface-remote-connection");
                if (remoteFeatureEnabled && _tunnelingEnabled && await DriverContext.TunnelingProvider.IsAvailableAsync(default))
                {
                    string tunnel;
                    if (_secureDriver)
                    {
                        tunnel = await DriverContext.TunnelingProvider.CreateTunnelAsync(TunnelingProtocol.Tcp, "knx", $"{_remoteIp}", _remotePort,
                            token);
                    }
                    else
                    {
                        tunnel = await DriverContext.TunnelingProvider.CreateTunnelAsync(TunnelingProtocol.Udp, "knx", $"{_remoteIp}", _remotePort,
                            token);
                    }

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
            await StartConnection(token);

            return await base.Start(token);
        }

        private async Task StartConnection(CancellationToken token = default)
        {
            DriverContext.Logger.LogInformation($"Start KNX connection...");
            _gwState?.SetGatewayState(false);
            await _semaphore.WaitAsync(token);
            try
            {
                await _tunneling.ConnectAsync(token);
            }
            catch (Exception e)
            {
                DriverContext.Logger.LogError(e, $"Error connecting to KNX Interface {e}");
            }
            finally
            {
                _semaphore.Release();
            }
            DriverContext.Logger.LogInformation($"Start KNX connection...done");
        }

        private async Task DisposeConnection()
        {
            await _semaphore.WaitAsync();
            try
            {
                DriverContext.Logger.LogInformation($"Dispose KNX driver...");
                _tunneling.ConnectionStateChanged -= _tunneling_ConnectionStateChanged;
                _tunneling.GroupMessageReceived -= _tunneling_GroupMessageReceived;
                await _tunneling.DisposeAsync();
                DriverContext.Logger.LogInformation($"Dispose KNX driver...done");
            }
            catch (Exception e)
            {
                DriverContext.Logger.LogError(e, "Error disposing connection properly....");
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public override async Task<bool> Stop(CancellationToken token = default)
        {
            DriverContext.Logger.LogInformation($"Stopping KNX driver...");
            if (_tunneling != null)
            {
                if (!_onlyUseTunnel)
                {
                    await DisposeConnection();
                }

                _callbackMap.Clear();
                _tunneling = null;
            }

            return await base.Stop(token);
        }

        internal void AddGroupAddress(string groupAddress, Action<GroupEventArgs> callback)
        {
            DriverContext.Logger.LogDebug($"Register for value changes on GA {groupAddress}");
            if (!_callbackMap.ContainsKey(groupAddress))
            {
                _callbackMap.Add(groupAddress, new List<Action<GroupEventArgs>>());
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

        public void AddAddressNotifier(string address, KnxGroupAddress ga, Action<object> callback)
        {
            AddGroupAddress(address, callback);

            if (_gaMap.ContainsKey(address))
            {
                DriverContext.Logger.LogWarning($"Double mapping detected {address} is used multiple times!");
                return;
            }

            _gaMap.Add(address, ga);
        }

        public async Task<bool> Read(string address)
        {
            DriverContext.Logger.LogDebug($"Read datagram on GA {address}");

            await _semaphore.WaitAsync();
            try
            {
                if (_tunneling.ConnectionState != BusConnectionState.Connected)
                {
                    DriverContext.Logger.LogError($"Cannot read from KNX interface, not connected");
                    return false;
                }
                return await _tunneling.RequestGroupValueAsync(GroupAddress.Parse(address));
            }
            finally
            {
                _semaphore.Release(1);
            }
        }

        public async Task<bool> Write(KnxGroupAddress source, string address, GroupValue groupValue, CancellationToken token)
        {
            DriverContext.Logger.LogDebug($"Write datagram on GA {address} {groupValue.Value.ToHex(false)}");

            await _semaphore.WaitAsync(token);
            try
            {
                if (_tunneling.ConnectionState != BusConnectionState.Connected)
                {
                    DriverContext.Logger.LogError($"Cannot write to KNX interface, not connected");
                    return false;
                }

                _lastGaValues[address] = groupValue;
                return await _tunneling.WriteGroupValueAsync(GroupAddress.Parse(address), groupValue,
                    MessagePriority.High, token);
            }
            catch (Exception e)
            {
                DriverContext.Logger.LogError(e, $"Error writing to KNX interface {e}");
                throw;
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}
