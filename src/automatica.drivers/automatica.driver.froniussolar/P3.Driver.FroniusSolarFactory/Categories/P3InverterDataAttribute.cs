using System;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using FroniusSolarClient;

namespace P3.Driver.FroniusSolarFactory.Categories
{
    internal class P3InverterDataAttribute : FroniusCategoryAttribute
    {
        public P3InverterDataAttribute(IDriverContext driverContext, SolarClient solarClient, FroniusDeviceAttribute device) : base(driverContext, solarClient, device)
        {
        }


        public override async Task PollAttributes()
        {
            await Task.CompletedTask;
            var data = SolarClient.GetP3InverterData(Device.DeviceId);

            foreach (var attr in _attributes.Values)
            {
                switch (attr.Key)
                {
                    case "l1-ac-current":
                        attr.DispatchValue(data?.Body.Data.L1AcCurrent?.Value ?? 0);
                        break;
                    case "l2-ac-current":
                        attr.DispatchValue(data?.Body.Data.L2AcCurrent?.Value ?? 0);
                        break;
                    case "l3-ac-current":
                        attr.DispatchValue(data?.Body.Data.L3AcCurrent?.Value ?? 0);
                        break;
                    case "l1-ac-voltage":
                        attr.DispatchValue(data?.Body.Data.L1AcVoltage?.Value ?? 0);
                        break;
                    case "l2-ac-voltage":
                        attr.DispatchValue(data?.Body.Data.L2AcVoltage?.Value ?? 0);
                        break;
                    case "l3-ac-voltage":
                        attr.DispatchValue(data?.Body.Data.L3AcVoltage?.Value ?? 0);
                        break;
                    case "ambient-temperature":
                        attr.DispatchValue(data?.Body.Data.AmbientTemperature?.Value);
                        break;
                    case "fan-front-left-speed":
                        attr.DispatchValue(data?.Body.Data.FanFrontLeftSpeed?.Value ?? 0);
                        break;
                    case "fan-front-right-speed":
                        attr.DispatchValue(data?.Body.Data.FanBackRightSpeed?.Value ?? 0);
                        break;
                    case "fan-back-left-speed":
                        attr.DispatchValue(data?.Body.Data.FanBackLeftSpeed?.Value ?? 0);
                        break;
                    case "fan-back-right-speed":
                        attr.DispatchValue(data?.Body.Data.FanBackRightSpeed?.Value ?? 0);
                        break;

                }
            }
        }
    }
}
