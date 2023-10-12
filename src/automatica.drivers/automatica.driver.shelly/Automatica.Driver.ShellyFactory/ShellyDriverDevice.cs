using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using Automatica.Driver.Shelly.Clients;
using Automatica.Driver.Shelly.Options;
using Automatica.Driver.ShellyFactory.Types.Meter;
using Automatica.Driver.ShellyFactory.Types.Relay;
using Automatica.Driver.ShellyFactory.Types.Roller;
using Microsoft.Extensions.Logging;
using Timer = System.Timers.Timer;
using Automatica.Driver.ShellyFactory.Discovery;
using Automatica.Core.Base.TelegramMonitor;
using Automatica.Driver.ShellyFactory.Types;

namespace Automatica.Driver.ShellyFactory
{
    internal class ShellyDriverDevice : DriverBase
    {
        private readonly ITelegramMonitorInstance _telegramMonitorInstance;
        private Timer _timer;
        public string ShellyId { get; }
        public int PollingInterval { get; }
        public int PollFailCount { get; private set; }

        private string _ipAddress;
        public ShellyDevice Device { get; private set; }

        public ShellyClient Client { get; set; }

        protected readonly List<ShellyContainerNode> ContainerNodes = new();

        internal ShellyDriverDevice(IDriverContext driverContext, ITelegramMonitorInstance telegramMonitorInstance) : base(driverContext)
        {
            _telegramMonitorInstance = telegramMonitorInstance;
            ShellyId = GetPropertyValueString(ShellyFactory.DeviceIdPropertyKey);
            PollingInterval = GetPropertyValueInt("polling-interval");
        }

        public override async Task<bool> Start(CancellationToken token = new CancellationToken())
        {
            var shellyDiscovery = (Parent as ShellyDriver).DiscoveredShellys.FirstOrDefault(a => a.Id == ShellyId);

            if (shellyDiscovery == null)
            {
                DriverContext.Logger.LogError($"Could not find any shelly device with id {ShellyId} in the local network");
                return false;
            }

            _ipAddress = shellyDiscovery.IpAddress;
            Device = shellyDiscovery;

            Client = new ShellyClient(_telegramMonitorInstance, new HttpClient { BaseAddress = new Uri($"http://{_ipAddress}") },
                new ShellyOptions
                {
                    UserName = GetPropertyValueString("shelly-username"),
                    Password = GetPropertyValueString("shelly-password")
                });

            if (!await Poll(token))
            {
                return false;
            }


            _timer = new Timer(PollingInterval);


            var ret = await base.Start(token);

            if (!ret)
            {
                return ret;
            }

            if (!await Poll(token))
            {
                return false;
            }

            _timer.Elapsed += _timer_Elapsed;
            _timer.Start();
            return true;
        }

        private async void _timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            _timer.Elapsed -= _timer_Elapsed;
            try
            {
                var ret = await Poll();
                if (!ret)
                {
                    PollFailCount++;
                }
                else
                {
                    PollFailCount = 0;
                }

                if (PollFailCount >= 10)
                {
                    DriverContext.Logger.LogError($"Stop polling, after 10 tries, we have some errors to check...");
                    _timer.Stop();
                }
            }
            catch (Exception ex)
            {
                DriverContext.Logger.LogError(ex, "Error polling shelly device");
            }
            finally
            {
                _timer.Elapsed += _timer_Elapsed;
            }
        }

        public override Task<bool> Stop(CancellationToken token = new CancellationToken())
        {
            if (_timer != null)
            {
                _timer.Elapsed -= _timer_Elapsed;
                _timer.Stop();
                _timer.Dispose();
            }

            return base.Stop(token);
        }

        private async Task<bool> Poll(CancellationToken token = default)
        {
            try
            {
                var data = await Client.GetStatus(token);

                if (data.IsSuccess)
                {
                    foreach (var containerNode in ContainerNodes)
                    {
                        containerNode.ReadData(data.Value);
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                DriverContext.Logger.LogError(ex, "Something strange happened....");
            }

            return false;
        }

        protected override Task Write(object value, IWriteContext writeContext, CancellationToken token = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        protected override Task<bool> Read(IReadContext readContext, CancellationToken token = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            var type = ctx.NodeInstance.This2NodeTemplateNavigation.Key;

            ShellyContainerNode node;
            switch (type)
            {
                case "shelly-relays":
                    node = new RelayContainerNode(ctx, this);
                    break;
                case "shelly-meters":
                    node = new MeterContainerNode(ctx, this);
                    break;
                case "shelly-rollers":
                    node = new RollerContainerNode(ctx, this);
                    break;
                default:
                    return null;
            }

            ContainerNodes.Add(node);
            return node;
        }
    }
}
