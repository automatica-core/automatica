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
            await Task.CompletedTask;
            var flow = SolarClient.GetPowerFlowRealtimeData();

            if (flow != null && flow.Body.Data.Inverters.ContainsKey(Device.DeviceId.ToString()))
            {
                var data = flow.Body.Data.Inverters[Device.DeviceId.ToString()];
                foreach (var attr in _attributes.Values)
                {
                    switch (attr.Key)
                    {
                        case "device-type":
                            attr.DispatchValue(data.DT);
                            break;
                        case "current-power":
                            attr.DispatchValue(data.P);
                            break;
                        case "ac-energy-today":
                            attr.DispatchValue(data.EDay);
                            break;
                        case "ac-energy-year":
                            attr.DispatchValue(data.EYear);
                            break;
                        case "ac-energy-total":
                            attr.DispatchValue(data.ETotal);
                            break;
                    }
                }
            }

        }

    }
}
