using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using FroniusSolarClient;
using FroniusSolarClient.Entities.SolarAPI.V1.InverterRealtimeData;

namespace P3.Driver.FroniusSolarFactory.Categories
{
    internal class CommonInverterDataAttribute : FroniusCategoryAttribute
    {

  
        public CommonInverterDataAttribute(IDriverContext driverContext, SolarClient solarClient, FroniusDeviceAttribute device) : base(driverContext, solarClient, device)
        {
        }
        public override async Task PollAttributes(CancellationToken token = default)
        {
            await Task.CompletedTask;
            var commonInverterData = SolarClient.GetCommonInverterData(Device.DeviceId);

            if (commonInverterData == null || commonInverterData.Body == null)
            {
                DeviceStateAttribute?.DriverContext.Dispatcher.DispatchValue(DeviceStateAttribute, false);
            }

            if (commonInverterData != null && commonInverterData.Body != null)
            {
                DeviceStateAttribute?.DriverContext.Dispatcher.DispatchValue(DeviceStateAttribute, true);
            }

            await Read(token);
            
           

        }

        protected override Task Write(object value, IWriteContext writeContext, CancellationToken token = new CancellationToken())
        {
            return Task.CompletedTask;
        }

        protected override async Task<bool> Read(IReadContext readContext, CancellationToken token = new CancellationToken())
        {
            var commonInverterData = SolarClient.GetCommonInverterData(Device.DeviceId);

            await Task.CompletedTask;

            foreach (var attr in _attributes.Values)
            {
                switch (attr.Key)
                {
                    case "ac-power":
                        attr.DispatchRead(commonInverterData?.Body?.Data.AcPower?.Value ?? 0);
                        break;
                    case "ac-current":
                        attr.DispatchRead(commonInverterData?.Body?.Data.AcCurrent?.Value ?? 0);
                        break;
                    case "ac-voltage":
                        attr.DispatchRead(commonInverterData?.Body?.Data.AcVoltage?.Value ?? 0);
                        break;
                    case "ac-frequency":
                        attr.DispatchRead(commonInverterData?.Body?.Data.AcFrequency?.Value ?? 0);
                        break;
                    case "dc-current":
                        attr.DispatchRead(commonInverterData?.Body?.Data.DcCurrent?.Value ?? 0);
                        break;
                    case "dc-voltage":
                        attr.DispatchRead(commonInverterData?.Body?.Data.DcVoltage?.Value ?? 0);
                        break;
                    case "current-day-energy":
                        attr.DispatchRead(commonInverterData?.Body?.Data.CurrentDayEnergy.Value);
                        break;
                    case "current-year-energy":
                        attr.DispatchRead(commonInverterData?.Body?.Data.CurrentYearEnergy.Value);
                        break;
                    case "total-energy":
                        attr.DispatchRead(commonInverterData?.Body?.Data.TotalEnergy.Value);
                        break;
                    case "device-status":
                        attr.DispatchRead(commonInverterData?.Body?.Data.DeviceStatus.StatusCode);
                        break;
                    case "error-code":
                        attr.DispatchRead(commonInverterData?.Body?.Data.DeviceStatus.ErrorCode);
                        break;
                    case "mgm-timer-remaining-time":
                        attr.DispatchRead(commonInverterData?.Body?.Data.DeviceStatus
                            .MgmtTimerRemainingTime);
                        break;
                }

            }
            return true;
        }
    }
}
