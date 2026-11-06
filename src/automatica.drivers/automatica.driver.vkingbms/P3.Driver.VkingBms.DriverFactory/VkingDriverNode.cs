using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using Microsoft.Extensions.Logging;
using P3.Driver.VkingBms.Driver;
using P3.Driver.VkingBms.Driver.Exception;
using P3.Driver.VkingBms.DriverFactory.Nodes;
using Timer = System.Threading.Timer;

[assembly: InternalsVisibleTo("P3.Driver.VkingBms.Tests")]

namespace P3.Driver.VkingBms.DriverFactory
{
    internal class VkingDriverNode : DriverNoneAttributeBase
    {
        private VkingDriver _driver;
        private readonly List<VkingBatteryPackNode> _packs = new();
        private Timer _timer;
        private string _port;
        private int _opCancelledCounter = 0;

        private readonly SemaphoreSlim _semaphore = new(1);
        private readonly object _openLock = new();
        private volatile bool _stopped;

        public VkingDriverNode(IDriverContext driverContext) : base(driverContext)
        {
            
        }

        public override Task<bool> Init(CancellationToken token = default)
        {
            _port = GetProperty("vking-pack-port").ValueString;
            _driver = new VkingDriver(_port, TelegramMonitor, DriverContext.Logger);

            return base.Init(token);
        }

        public override async Task<bool> Start(CancellationToken token = default)
        {
            _stopped = false;
            try
            {
                _driver.Open();
            }
            catch (Exception e)
            {
                // the poll timer reopens the port, a missing port at startup must not stop polling forever
                DriverContext.Logger.LogError(e, $"Could not open port {_port}, retry on next poll...");
            }

            _timer?.Dispose(); // Start may be retried, never keep a second timer running
            _timer = new Timer(ReadPacks, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2));
            return await base.Start(token);
        }

        public override Task<bool> Stop(CancellationToken token = default)
        {
            lock (_openLock)
            {
                _stopped = true;
                _timer?.Dispose();
                try
                {
                    _driver?.Close();
                }
                catch (Exception e)
                {
                    DriverContext.Logger.LogError(e, $"Could not close port {_port}...{e}");
                }
            }
            return base.Stop(token);
        }

        private void ReOpen()
        {
            lock (_openLock)
            {
                // a read that was in flight during Stop must not reopen the port
                if (_stopped)
                {
                    return;
                }

                try
                {
                    _driver.Close();
                }
                catch
                {
                    //ignore, we create a new instance anyway
                }

                _driver = new VkingDriver(_port, TelegramMonitor, DriverContext.Logger);
                try
                {
                    _driver.Open();
                }
                catch
                {
                    //ignore, timer will recall it anyway
                }
            }
        }

        private async void ReadPacks(object state)
        {
            if (_stopped)
            {
                return;
            }

            var acquired = false;
            try
            {
                // skip this tick if the previous read is still running, ticks must not pile up
                acquired = await _semaphore.WaitAsync(TimeSpan.Zero);
                if (!acquired)
                {
                    return;
                }

                DriverContext.Logger.LogInformation("Start read...");
                using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(10));
                if (!_driver.IsOpen)
                {
                    ReOpen();
                }

                foreach (var pack in _packs)
                {
                    if (_stopped)
                    {
                        return;
                    }

                    try
                    {
                        var version = await _driver.ReadVersionInfo(pack.PackId, cancellationTokenSource.Token);
                        var analogData = await _driver.ReadAnalogValues(pack.PackId, cancellationTokenSource.Token);

                        pack.Read(analogData, version);
                        _opCancelledCounter = 0;
                    }
                    catch (DataReadException ex)
                    {
                        // includes InvalidCrcException, a broken frame of one pack must not abort the other packs
                        DriverContext.Logger.LogError(ex, $"Error reading pack {pack.PackId}...{ex}");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                DriverContext.Logger.LogError("Operation cancelled...");
                _opCancelledCounter++;

                if (_opCancelledCounter >= 10)
                {
                    _opCancelledCounter = 0;
                    ReOpen();
                }
            }
            catch (Exception ex)
            {
                DriverContext.Logger.LogError(ex, $"Error reading...{ex}");
                ReOpen();
            }
            finally
            {
                if (acquired)
                {
                    DriverContext.Logger.LogInformation("Done read...");
                    _semaphore.Release();
                }
            }
        }

        protected override bool CreateCustomLogger()
        {
            return true;
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            var ret = new VkingBatteryPackNode(ctx);
            _packs.Add(ret);
            return ret;
        }
    }
}
