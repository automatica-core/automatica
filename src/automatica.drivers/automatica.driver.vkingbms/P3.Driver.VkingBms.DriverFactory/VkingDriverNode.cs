using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using Microsoft.Extensions.Logging;
using P3.Driver.VkingBms.Driver;
using P3.Driver.VkingBms.Driver.Exception;
using P3.Driver.VkingBms.DriverFactory.Nodes;
using Timer = System.Threading.Timer;

namespace P3.Driver.VkingBms.DriverFactory
{
    internal class VkingDriverNode : DriverBase
    {
        private VkingDriver _driver;
        private readonly List<VkingBatteryPackNode> _packs = new();
        private Timer _timer;
        private string _port;
        private int _opCancelledCounter = 0;

        private readonly SemaphoreSlim _semaphore = new(1);

        public VkingDriverNode(IDriverContext driverContext) : base(driverContext)
        {
            
        }

        public override bool Init()
        {
            _port = GetProperty("vking-pack-port").ValueString;
            _driver = new VkingDriver(_port, TelegramMonitor, DriverContext.Logger);

            return base.Init();
        }

        public override async Task<bool> Start()
        {
            _driver.Open();
            _timer = new Timer(ReadPacks, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2));
            return await base.Start();
        }

        public override Task<bool> Stop()
        {
            _timer?.Dispose();
            _driver?.Close();
            return base.Stop(); 
        }

        private void ReOpen()
        {
            _driver.Close();
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

        private async void ReadPacks(object state)
        {

            try
            {
                DriverContext.Logger.LogInformation("Start read...");
                var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(10));
                await _semaphore.WaitAsync(cancellationTokenSource.Token);
                if (!_driver.IsOpen)
                {
                    ReOpen();
                }

                foreach (var pack in _packs)
                {
                    try
                    {
                        var version = await _driver.ReadVersionInfo(pack.PackId, cancellationTokenSource.Token);
                        var analogData = await _driver.ReadAnalogValues(pack.PackId, cancellationTokenSource.Token);

                        pack.Read(analogData, version);
                        _opCancelledCounter = 0;
                    }
                    catch (DataReadException ex)
                    {
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
                DriverContext.Logger.LogInformation("Done read...");
                _semaphore.Release();
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
