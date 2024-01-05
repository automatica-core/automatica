using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using Automatica.Driver.ShellyFactory.Types.Meter;
using Automatica.Driver.ShellyFactory.Types.Relay;
using Automatica.Driver.ShellyFactory.Types.Roller;
using Microsoft.Extensions.Logging;
using Timer = System.Timers.Timer;
using Automatica.Driver.ShellyFactory.Discovery;
using Automatica.Core.Base.TelegramMonitor;
using Automatica.Core.EF.Models;
using Automatica.Driver.ShellyFactory.Types;
using Automatica.Driver.Shelly.Gen1.Clients;
using Automatica.Driver.Shelly.Gen1.Options;
using Automatica.Driver.Shelly.Common;
using Automatica.Driver.Shelly.Gen2;
using Automatica.Driver.Shelly.Gen2.Models;

namespace Automatica.Driver.ShellyFactory
{
    internal class ShellyDriverDevice : DriverBase
    {
        private readonly ITelegramMonitorInstance _telegramMonitorInstance;
        private Timer _timer;
        public string ShellyId { get; }
        public ShellyGeneration Generation { get; set; }
        public int PollingInterval { get; }
        public int PollFailCount { get; private set; }

        private string _ipAddress;

        public IShellyClient Client { get; set; }

        protected readonly List<ShellyContainerNode> ContainerNodes = new();

        internal ShellyDriverDevice(IDriverContext driverContext, ITelegramMonitorInstance telegramMonitorInstance) : base(driverContext)
        {
            _telegramMonitorInstance = telegramMonitorInstance;
            ShellyId = GetPropertyValueString(ShellyFactory.DeviceIdPropertyKey);
            PollingInterval = GetPropertyValueInt("polling-interval");

            Generation = (ShellyGeneration)GetPropertyValue("shelly-generation", ShellyGeneration.Gen1);
        }

        public override async Task<bool> Start(CancellationToken token = new CancellationToken())
        {
            var useIp = GetPropertyValueBool("shelly-use-ip");

            if (useIp.HasValue && useIp.Value)
            {
                _ipAddress = GetPropertyValueString("shelly-ip");
            }
            else
            {
                var shellyDiscovery = (Parent as ShellyDriver).DiscoveredShellys.FirstOrDefault(a => a.Id.Split("-")[^1] == ShellyId);

                if (shellyDiscovery == null)
                {
                    DriverContext.Logger.LogError(
                        $"Could not find any shelly device with id {ShellyId} in the local network");
                    return false;
                }

                _ipAddress = shellyDiscovery.IpAddress;
            }

            if (Generation == ShellyGeneration.Gen1)
            {
                Client = new ShellyGen1Client(_telegramMonitorInstance,
                    new ShellyOptions
                    {
                        UserName = GetPropertyValueString("shelly-username"),
                        Password = GetPropertyValueString("shelly-password"),
                        IpAddress = _ipAddress
                    }, DriverContext.Logger);

            }
            else if (Generation == ShellyGeneration.Gen2)
            {
                var gen2Client = new ShellyGen2Client(_telegramMonitorInstance,
                    new ShellyOptions
                    {
                        Password = GetPropertyValueString("shelly-password"),
                        IpAddress = _ipAddress
                    }, DriverContext.Logger);

                gen2Client.OnNotifyEvent += Gen2ClientOnOnNotifyEvent;

                Client = gen2Client;
            }
            else
            {
                return false;
            }

            
            var ret = await base.Start(token);

            if (!ret)
            {
                return false;
            }

            var shellyInfo = await Client.GetInfo(token);

            if (ShellyId.ToLowerInvariant() != shellyInfo.Mac.ToLowerInvariant().Trim())
            {
                DriverContext.Logger.LogError($"Invalid shelly id....");
                return false;
            }

            try
            {
                if (!await Poll(token))
                {
                    return false;
                }
            }
            catch (Exception e)
            {
                throw;
            }

            _timer = new Timer(PollingInterval);
            _timer.Elapsed += _timer_Elapsed;
            _timer.Start();

            await Client.Connect(token);

          
            return true;
        }

        private async void Gen2ClientOnOnNotifyEvent(object sender, NotifyStatusEvent eventMessage)
        {
            try
            {
                foreach (var containerNode in ContainerNodes)
                {
                    await containerNode.FromStatusUpdate(eventMessage);
                }

            }
            catch (Exception ex)
            {
                DriverContext.Logger.LogError(ex, "Something strange happened during an event message....");
            }

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

        public override Task OnDelete(NodeInstance instance, CancellationToken token = new CancellationToken())
        {
            return Parent.OnDelete(instance, token);
        }

        public override async Task<bool> Stop(CancellationToken token = new CancellationToken())
        {
            if (_timer != null)
            {
                _timer.Elapsed -= _timer_Elapsed;
                _timer.Stop();
                _timer.Dispose();
            }

            if (Client is ShellyGen2Client gen2Client)
            {
                gen2Client.OnNotifyEvent -= Gen2ClientOnOnNotifyEvent;
            }

            if (Client != null)
            {
                await Client.Disconnect(token);
            }

            return await base.Stop(token);
        }

        private async Task<bool> Poll(CancellationToken token = default)
        {
            try
            {
                foreach (var containerNode in ContainerNodes)
                {
                    await containerNode.ReadData();
                }

                return true;
            }
            catch (Exception ex)
            {
                DriverContext.Logger.LogError(ex, "Something strange happened....");
                throw;
            }
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
