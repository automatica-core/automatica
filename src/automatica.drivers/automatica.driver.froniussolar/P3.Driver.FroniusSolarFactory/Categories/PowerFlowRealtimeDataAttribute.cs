using System;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using FroniusSolarClient;

namespace P3.Driver.FroniusSolarFactory.Categories
{
    internal class PowerFlowRealtimeDataAttribute : FroniusCategoryAttribute
    {
        public PowerFlowRealtimeDataAttribute(IDriverContext driverContext, SolarClient solarClient, FroniusDeviceAttribute device) : base(driverContext, solarClient, device)
        {
        }

        public override async Task PollAttributes(CancellationToken token = default)
        {
            await Read(token);

        }

        protected override Task Write(object value, IWriteContext writeContext, CancellationToken token = new CancellationToken())
        {
            return Task.CompletedTask;
        }

        protected override async Task<bool> Read(IReadContext readContext, CancellationToken token = new CancellationToken())
        {
            var flow = SolarClient.GetPowerFlowRealtimeData();
            await Task.CompletedTask;

            if (flow != null && flow.Body.Data.Inverters.ContainsKey(Device.DeviceId.ToString()))
            {
                var data = flow.Body.Data.Inverters[Device.DeviceId.ToString()];
                foreach (var attr in _attributes.Values)
                {
                    switch (attr.Key)
                    {
                        case "device-type":
                            attr.DispatchRead(data.DT);
                            break;
                        case "current-power":
                            attr.DispatchRead(data.P);
                            break;
                        case "ac-energy-today":
                            attr.DispatchRead(data.EDay);
                            break;
                        case "ac-energy-year":
                            attr.DispatchRead(data.EYear);
                            break;
                        case "ac-energy-total":
                            attr.DispatchRead(data.ETotal);
                            break;
                    }
                }
            }
            return true;
        }
    }
}
