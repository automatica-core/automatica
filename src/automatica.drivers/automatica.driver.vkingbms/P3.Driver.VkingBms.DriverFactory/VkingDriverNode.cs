using System;
using System.Collections.Generic;
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

        public VkingDriverNode(IDriverContext driverContext) : base(driverContext)
        {
            
        }

        public override bool Init()
        {
            var port = GetProperty("vking-pack-port").ValueString;
            _driver = new VkingDriver(port, TelegramMonitor, DriverContext.Logger);

            return base.Init();
        }

        public override async Task<bool> Start()
        {
            _driver.Open();
            _timer = new Timer(ReadPacks, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
            return await base.Start();
        }

        private async void ReadPacks(object state)
        {
            foreach (var pack in _packs)
            {
                await pack.Read();
            }
        }

        protected override bool CreateCustomLogger()
        {
            return true;
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            var ret = new VkingBatteryPackNode(ctx, _driver);
            _packs.Add(ret);
            return ret;
        }
    }
}
