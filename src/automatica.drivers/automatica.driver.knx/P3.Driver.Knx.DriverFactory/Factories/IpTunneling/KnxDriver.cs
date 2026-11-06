using System;
using System.Collections.Concurrent;
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
using Knx.Falcon.Logging;
using Automatica.Core.Base.Cryptography;
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

        internal KnxGatewayState GatewayState { get; private set; }

        private Knx3Level _knxTree;

        private readonly Dictionary<string, List<Action<object>>> _callbackMap = new();
        private readonly ConcurrentDictionary<string, KnxGroupAddress> _gaMap = new();

        private readonly ConcurrentDictionary<string, GroupValue> _lastGaValues = new();

        private bool _tunnelingEnabled;
        private IPAddress _remoteIp;
        private int _remotePort;
        private bool _onlyUseTunnel;
        private KnxConnection _connection;

        private SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        private static readonly TimeSpan ReconnectInitialDelay = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan ReconnectMaxDelay = TimeSpan.FromSeconds(60);
        private CancellationTokenSource _stopCts = new CancellationTokenSource();
        private int _reconnecting;

        internal ITelegramMonitorInstance InternalTelegramMonitorInstance => TelegramMonitor;

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

      

        public override async Task<bool> Init(CancellationToken token = default)
        {
            var ipAddress = GetProperty("knx-ip").ValueString;

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
                _connection = CreateConnection();

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

        private KnxConnection CreateConnection()
        {
            DriverContext.Logger.LogInformation($"Create connection...");
            var ipAddress = GetProperty("knx-ip").ValueString;
            var useNat = GetProperty("knx-use-nat").ValueBool;
            var port = GetPropertyValueInt("knx-port");

            KnxSecureSettings secureConfig = null;
            if (_secureDriver)
            {
                var authPw = GetPropertyValueString("knx-auth-pw");
                var userPw = GetPropertyValueString("knx-user-pw");
                var userId = GetPropertyValueInt("knx-user-id");
                var iaAddress = GetPropertyValueString("knx-ia-address");
                secureConfig = new KnxSecureSettings(authPw, userPw, userId, iaAddress);
            }
            var connection = new KnxConnection(this, DriverContext.Logger, ipAddress, useNat ?? false, port, _secureDriver, secureConfig);
            return connection;
        }

        internal GroupValue? GetGroupValue(GroupAddress destination)
        {
            var ga = destination.ToString()!;
            if (_gaMap.TryGetValue(ga, out _) && _lastGaValues.TryGetValue(ga, out var gaValue))
            {
                return gaValue;
            }

            return null;
        }

        internal void GroupValueReceived(GroupEventArgs e)
        {
            GroupValueReceived(e.DestinationAddress, e.Value);
        }

        internal void GroupValueReceived(string address, GroupValue value)
        {
            DispatchToNotifiers(address, value, value.Value);
        }

        /// <summary>
        /// Passes a raw datapoint payload (e.g. from the BAOS interface) to the group addresses, they know the size of their datapoint type.
        /// </summary>
        internal void RawValueReceived(string address, byte[] data)
        {
            DispatchToNotifiers(address, data, data);
        }

        private void DispatchToNotifiers(string address, object payload, byte[] data)
        {
            if (_callbackMap.TryGetValue(address, out var value))
            {
                foreach (var ac in value)
                {
                    try
                    {
                        DriverContext.Logger.LogDebug($"Datagram on GA {address}  {data.ToHex(false)} - dispatch to {ac}");
                        ac.Invoke(payload);

                    }
                    catch (Exception ex)
                    {
                        DriverContext.Logger.LogError($"{address}: {ex}");
                    }
                }
            }
            else
            {
                DriverContext.Logger.LogInformation(
                    $"Datagram on GA {address} - no callback registered");
            }
        }

        internal Task ConnectionStateChanged(BusConnectionState state)
        {
            if (state is not (BusConnectionState.Broken or BusConnectionState.Closed))
            {
                return Task.CompletedTask;
            }

            if (_stopCts.IsCancellationRequested)
            {
                return Task.CompletedTask;
            }

            if (Interlocked.CompareExchange(ref _reconnecting, 1, 0) != 0)
            {
                DriverContext.Logger.LogDebug($"Reconnect already in progress...");
                return Task.CompletedTask;
            }

            var token = _stopCts.Token;
            _ = Task.Run(() => ReconnectLoop(token), CancellationToken.None);
            return Task.CompletedTask;
        }

        private async Task ReconnectLoop(CancellationToken token)
        {
            try
            {
                DriverContext.Logger.LogInformation($"Try to create a new connection...");

                KnxConnection oldConnection;
                await _semaphore.WaitAsync(token);
                try
                {
                    oldConnection = _connection;
                    _connection = null;
                }
                finally
                {
                    _semaphore.Release();
                }

                await DisposeConnectionSafe(oldConnection);

                var delay = ReconnectInitialDelay;
                while (!token.IsCancellationRequested)
                {
                    await Task.Delay(delay, token);

                    KnxConnection newConnection = null;
                    try
                    {
                        newConnection = CreateConnection();
                        await StartConnection(newConnection, token);
                    }
                    catch (OperationCanceledException) when (token.IsCancellationRequested)
                    {
                        await DisposeConnectionSafe(newConnection);
                        return;
                    }
                    catch (Exception e)
                    {
                        DriverContext.Logger.LogWarning($"Reconnect to KNX interface failed, retry in {delay.TotalSeconds}s: {e.Message}");
                        await DisposeConnectionSafe(newConnection);

                        delay = TimeSpan.FromTicks(Math.Min(delay.Ticks * 2, ReconnectMaxDelay.Ticks));
                        continue;
                    }

                    await _semaphore.WaitAsync(CancellationToken.None);
                    try
                    {
                        if (token.IsCancellationRequested)
                        {
                            await DisposeConnectionSafe(newConnection);
                            return;
                        }

                        _connection = newConnection;
                    }
                    finally
                    {
                        _semaphore.Release();
                    }

                    DriverContext.Logger.LogInformation($"Reconnected to KNX interface...");
                    return;
                }
            }
            catch (OperationCanceledException)
            {
                // driver stopped
            }
            catch (Exception e)
            {
                DriverContext.Logger.LogError(e, $"Error in KNX reconnect loop {e}");
            }
            finally
            {
                Interlocked.Exchange(ref _reconnecting, 0);
            }
        }

        private async Task DisposeConnectionSafe(KnxConnection connection)
        {
            if (connection == null)
            {
                return;
            }

            try
            {
                await connection.DisposeConnection();
                DriverContext.Logger.LogInformation($"Closed previous connection...");
            }
            catch (Exception e)
            {
                DriverContext.Logger.LogError(e, $"Error disposing connection: {e}");
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
                    if (_secureDriver)
                    {
                        var tunnel = await DriverContext.TunnelingProvider.CreateTunnelAsync(TunnelingProtocol.TcpAndUdp, "knx", $"{_remoteIp}", _remotePort,
                            token);
                        DriverContext.Logger.LogInformation($"Tunnel created {tunnel}");
                    }
                    else
                    {
                        var tunnel = await DriverContext.TunnelingProvider.CreateTunnelAsync(TunnelingProtocol.Udp, "knx", $"{_remoteIp}", _remotePort,
                            token);
                        DriverContext.Logger.LogInformation($"Tunnel created {tunnel}");
                    }

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

            if (_stopCts.IsCancellationRequested)
            {
                _stopCts = new CancellationTokenSource();
            }

            await StartConnection(_connection, token);

            return await base.Start(token);
        }

        private async Task StartConnection(KnxConnection connection, CancellationToken token = default)
        {
            try
            {
                DriverContext.Logger.LogInformation($"Start KNX connection...");
                await connection.StartConnection(token);
                DriverContext.Logger.LogInformation($"Start KNX connection...done");
            }
            catch (Exception e)
            {
                DriverContext.Logger.LogError(e, $"Error connecting to KNX Interface {e}");
                throw;
            }
        }

     
        public override async Task<bool> Stop(CancellationToken token = default)
        {
            DriverContext.Logger.LogInformation($"Stopping KNX driver...");
            await _stopCts.CancelAsync();

            await _semaphore.WaitAsync(CancellationToken.None);
            try
            {
                if (_connection != null)
                {
                    if (!_onlyUseTunnel)
                    {
                        await _connection.DisposeConnection();
                    }

                    _callbackMap.Clear();
                    _connection = null;
                }
            }
            finally
            {
                _semaphore.Release();
            }

            return await base.Stop(token);
        }

        /// <summary>
        /// For interfaces without the KNXnet/IP connection (BAOS): runs the node handling of <see cref="DriverNoneAttributeBase"/> only.
        /// </summary>
        protected Task<bool> InitWithoutConnection(CancellationToken token) => base.Init(token);

        protected Task<bool> StartWithoutConnection(CancellationToken token) => base.Start(token);

        protected Task<bool> StopWithoutConnection(CancellationToken token)
        {
            _callbackMap.Clear();
            return base.Stop(token);
        }

        internal void AddGroupAddress(string groupAddress, Action<object> callback)
        {
            DriverContext.Logger.LogDebug($"Register for value changes on GA {groupAddress}");
            if (!_callbackMap.ContainsKey(groupAddress))
            {
                _callbackMap.Add(groupAddress, new List<Action<object>>());
            }
            _callbackMap[groupAddress].Add(callback);
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            if (ctx.NodeInstance.This2NodeTemplateNavigation.Key == "knx-gw-state")
            {
                GatewayState = new KnxGatewayState(ctx);
                return GatewayState;
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

            if (!_gaMap.TryAdd(address, ga))
            {
                DriverContext.Logger.LogWarning($"Double mapping detected {address} is used multiple times!");
            }
        }

        public virtual Task<bool> Read(string address)
        {
            var connection = _connection;
            if (connection == null)
            {
                return Task.FromResult(false);
            }
            return connection.Read(address);
        }

        public virtual async Task<bool> Write(KnxGroupAddress source, string address, GroupValue groupValue,
            CancellationToken token)
        {
            var connection = _connection;
            if (connection == null)
            {
                return false;
            }
            var ret = await connection.Write(source, address,groupValue, token);
            if (ret)
            {
                _lastGaValues[address] = groupValue;
            }

            return ret;
        }

    }
}
