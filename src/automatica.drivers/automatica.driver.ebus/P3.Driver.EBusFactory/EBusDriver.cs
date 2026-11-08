using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using Microsoft.Extensions.Logging;
using P3.Driver.EBus;
using P3.Driver.EBusFactory.Wolf;

namespace P3.Driver.EBusFactory
{
    /// <summary>
    /// The bus: keeps the connection to the adapter (reconnects), forwards the telegrams to the devices and polls them
    /// </summary>
    internal sealed class EBusDriver : DriverNoneAttributeBase
    {
        internal static TimeSpan ReconnectDelay = TimeSpan.FromSeconds(10);

        private readonly List<WolfDevice> _devices = new();
        private readonly SemaphoreSlim _pollLock = new(1, 1);
        private readonly TaskCompletionSource _connected = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private CancellationTokenSource _cts;
        private Task _run = Task.CompletedTask;
        private Timer _timer;

        public EBusDriver(IDriverContext driverContext) : base(driverContext)
        {
        }

        internal Func<string, int, IEBusTransport> TransportFactory { get; set; } = (host, port) => new EBusTcpTransport(host, port);
        internal string Host { get; private set; }
        internal int Port { get; private set; }
        internal byte MasterAddress { get; private set; }
        internal TimeSpan PollInterval { get; set; }
        internal EBusConnection Connection { get; private set; }
        internal IReadOnlyList<WolfDevice> Devices => _devices;

        /// <summary>
        /// Completes when the connection is up and the first poll is done
        /// </summary>
        internal Task Connected => _connected.Task;

        public override Task<bool> Init(CancellationToken token = default)
        {
            Host = GetPropertyValueString(EBusFactory.IpKey);
            Port = GetPropertyValueInt(EBusFactory.PortKey);
            var master = GetPropertyValueInt(EBusFactory.MasterAddressKey);
            if (master is < 0 or > 255 || !EBusProtocol.IsMaster((byte)master))
            {
                DriverContext.Logger.LogError($"{FullName}: {master} is not an eBUS master address, using 255 (0xff)");
                master = 0xFF;
            }

            MasterAddress = (byte)master;
            PollInterval = TimeSpan.FromSeconds(Math.Max(10, GetPropertyValueInt(EBusFactory.PollIntervalKey)));
            return base.Init(token);
        }

        public override Task<bool> Start(CancellationToken token = default)
        {
            _cts = new CancellationTokenSource();
            if (string.IsNullOrWhiteSpace(Host))
            {
                DriverContext.Logger.LogError($"{FullName}: no IP address of the eBUS adapter configured");
            }
            else
            {
                var cancel = _cts.Token;
                _run = Task.Run(() => RunAsync(cancel));
                _timer = new Timer(TimerElapsed, null, PollInterval, PollInterval);
            }

            return base.Start(token);
        }

        public override async Task<bool> Stop(CancellationToken token = default)
        {
            _timer?.Dispose();
            _timer = null;
            if (_cts != null)
            {
                await _cts.CancelAsync();
                try
                {
                    await _run;
                }
                catch (OperationCanceledException)
                {
                }

                _cts.Dispose();
                _cts = null;
            }

            return await base.Stop(token);
        }

        private async Task RunAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                var connection = new EBusConnection(TransportFactory(Host, Port), MasterAddress, DriverContext.Logger);
                connection.TelegramReceived += OnTelegram;
                try
                {
                    await connection.StartAsync(token);
                    Connection = connection;
                    DriverContext.Logger.LogInformation($"{FullName}: connected to the eBUS adapter {Host}:{Port} as master 0x{MasterAddress:x2}");
                    await PollAsync(token);
                    _connected.TrySetResult();
                    await connection.Completion.WaitAsync(token);
                    DriverContext.Logger.LogWarning($"{FullName}: the eBUS connection was closed, reconnecting");
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception e)
                {
                    DriverContext.Logger.LogError($"{FullName}: eBUS adapter {Host}:{Port}: {e.Message}, reconnecting in {ReconnectDelay.TotalSeconds:0} s");
                }
                finally
                {
                    Connection = null;
                    connection.TelegramReceived -= OnTelegram;
                    await connection.DisposeAsync();
                }

                try
                {
                    await Task.Delay(ReconnectDelay, token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        private void OnTelegram(EBusTelegram telegram)
        {
            if (DriverContext.Logger.IsEnabled(LogLevel.Trace))
            {
                DriverContext.Logger.LogTrace($"eBUS {telegram}");
            }

            foreach (var device in _devices)
            {
                device.OnTelegram(telegram);
            }
        }

        private async void TimerElapsed(object state)
        {
            try
            {
                await PollAsync(_cts?.Token ?? CancellationToken.None);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception e)
            {
                DriverContext.Logger.LogError(e, $"{FullName}: eBUS poll failed");
            }
        }

        internal async Task PollAsync(CancellationToken token = default)
        {
            var connection = Connection;
            if (connection == null)
            {
                return;
            }

            // a poll that is still running is enough
            if (!await _pollLock.WaitAsync(0, token))
            {
                return;
            }

            try
            {
                foreach (var device in _devices)
                {
                    await device.PollAsync(connection, token);
                }
            }
            finally
            {
                _pollLock.Release();
            }
        }

        protected override async Task<bool> Read(IReadContext readContext, CancellationToken token = default)
        {
            await PollAsync(token);
            return true;
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            if (ctx.NodeInstance.This2NodeTemplateNavigation?.Key != WolfBwl1.Key)
            {
                return null;
            }

            var device = new WolfDevice(ctx, this);
            _devices.Add(device);
            return device;
        }
    }
}
