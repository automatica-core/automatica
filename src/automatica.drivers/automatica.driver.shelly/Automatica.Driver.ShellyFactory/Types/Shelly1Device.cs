using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using Automatica.Driver.Shelly.Clients;
using Automatica.Driver.Shelly.Options;
using Automatica.Driver.ShellyFactory.Discovery;
using Microsoft.Extensions.Logging;

namespace Automatica.Driver.ShellyFactory.Types
{
    internal class Shelly1Device : ShellyDriverDevice
    {
        private string _ipAddress;
        public ShellyDevice Device { get; private set; }

        public IShelly1Pm Shelly { get; set; }

        private List<ShellyRelay> _relays = new List<ShellyRelay>();

        public Shelly1Device(IDriverContext driverContext) : base(driverContext)
        {
        }

        public override async Task<bool> Start(CancellationToken token = new CancellationToken())
        {
            var shellyDiscovery = (Parent as ShellyDriver).DiscoveredShellys.SingleOrDefault(a => a.Id == ShellyId);

            if (shellyDiscovery == null)
            {
                DriverContext.Logger.LogError($"Could not find any shelly device with id {ShellyId} in the local network");
                return false;
            }

            _ipAddress = shellyDiscovery.IpAddress;
            Device = shellyDiscovery;

            Shelly = new Shelly1PmClient(new HttpClient { BaseAddress = new Uri($"http://{_ipAddress}") },
                new Shelly1PmOptions
                {
                    UserName = GetPropertyValueString("shelly-username"),
                    Password = GetPropertyValueString("shelly-password")
                });

            return await base.Start(token);
        }

        public override async Task<bool> Write(int channelId, bool value, CancellationToken token = default)
        {
            await Shelly.SetStatus(channelId, value, token);
            return true;
        }

        public override async Task<bool> Poll(CancellationToken token = default)
        {
            if (Shelly != null)
            {
                try
                {
                    var status = await Shelly.GetStatus(token);

                    if (status.IsSuccess)
                    {
                        foreach (var relay in _relays)
                        {
                            if (status.Value.Relays.Length >= relay.RelayId + 1)
                            {
                                await relay.GetValueFromShelly(status.Value.Relays[relay.RelayId]);
                            }
                        }
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    DriverContext.Logger.LogError(ex, $"Could not poll shelly...{ex}");
                }
            }

            return false;
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            var relay =  new ShellyRelay(ctx, this);
            _relays.Add(relay);
            return relay;
        }
    }
}
