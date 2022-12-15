using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Automatica.Core.Driver;
using P3.Driver.VkingBms.Driver;
using P3.Driver.VkingBms.DriverFactory.Nodes;
using Timer = System.Threading.Timer;

namespace P3.Driver.VkingBms.DriverFactory
{
    internal class VkingDriverNode : DriverBase
    {
        private VkingDriver _driver;
        private List<VkingBatteryPackNode> _packs = new List<VkingBatteryPackNode>();
        private Timer _timer;
        private string _port;

        private SemaphoreSlim _semaphore = new SemaphoreSlim(1);

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
            await _semaphore.WaitAsync();
            try
            {
                if (!_driver.IsOpen)
                {
                    ReOpen();
                }

                foreach (var pack in _packs)
                {
                    var version = await _driver.ReadVersionInfo(pack.PackId);
                    var analogData = await _driver.ReadAnalogValues(pack.PackId);

                    pack.Read(analogData, version);
                }
            }
            catch (Exception)
            {
                ReOpen();
            }
            finally
            {
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
