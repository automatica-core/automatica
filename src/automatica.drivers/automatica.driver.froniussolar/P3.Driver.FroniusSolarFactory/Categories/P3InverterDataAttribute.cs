using System;
using System.Threading;
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


        public override async Task PollAttributes(CancellationToken token = default)
        {
            await Task.CompletedTask;
            await Read(token);
        }

        protected override Task Write(object value, IWriteContext writeContext, CancellationToken token = new CancellationToken())
        {
            return Task.CompletedTask;
        }

        protected override async Task<bool> Read(IReadContext readContext, CancellationToken token = new CancellationToken())
        {
            var data = SolarClient.GetP3InverterData(Device.DeviceId);
            await Task.CompletedTask;

            foreach (var attr in _attributes.Values)
            {
                switch (attr.Key)
                {
                    case "l1-ac-current":
                        attr.DispatchRead(data?.Body.Data.L1AcCurrent?.Value ?? 0);
                        break;
                    case "l2-ac-current":
                        attr.DispatchRead(data?.Body.Data.L2AcCurrent?.Value ?? 0);
                        break;
                    case "l3-ac-current":
                        attr.DispatchRead(data?.Body.Data.L3AcCurrent?.Value ?? 0);
                        break;
                    case "l1-ac-voltage":
                        attr.DispatchRead(data?.Body.Data.L1AcVoltage?.Value ?? 0);
                        break;
                    case "l2-ac-voltage":
                        attr.DispatchRead(data?.Body.Data.L2AcVoltage?.Value ?? 0);
                        break;
                    case "l3-ac-voltage":
                        attr.DispatchRead(data?.Body.Data.L3AcVoltage?.Value ?? 0);
                        break;
                    case "ambient-temperature":
                        attr.DispatchRead(data?.Body.Data.AmbientTemperature?.Value);
                        break;
                    case "fan-front-left-speed":
                        attr.DispatchRead(data?.Body.Data.FanFrontLeftSpeed?.Value ?? 0);
                        break;
                    case "fan-front-right-speed":
                        attr.DispatchRead(data?.Body.Data.FanBackRightSpeed?.Value ?? 0);
                        break;
                    case "fan-back-left-speed":
                        attr.DispatchRead(data?.Body.Data.FanBackLeftSpeed?.Value ?? 0);
                        break;
                    case "fan-back-right-speed":
                        attr.DispatchRead(data?.Body.Data.FanBackRightSpeed?.Value ?? 0);
                        break;

                }
            }

            return true;
        }
    }
}
