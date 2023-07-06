using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using FroniusSolarClient;

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
                DeviceStateAttribute?.DispatchValue(false);
            }

            if (commonInverterData != null && commonInverterData.Body != null)
            {
                DeviceStateAttribute?.DispatchValue(true);
            }
            
            foreach (var attr in _attributes.Values)
            {
                switch (attr.Key)
                {
                    case "ac-power":
                        attr.DispatchValue(commonInverterData?.Body?.Data.AcPower?.Value ?? 0);
                        break;
                    case "ac-current":
                        attr.DispatchValue(commonInverterData?.Body?.Data.AcCurrent?.Value ?? 0);
                        break;
                    case "ac-voltage":
                        attr.DispatchValue(commonInverterData?.Body?.Data.AcVoltage?.Value ?? 0);
                        break;
                    case "ac-frequency":
                        attr.DispatchValue(commonInverterData?.Body?.Data.AcFrequency?.Value ?? 0);
                        break;
                    case "dc-current":
                        attr.DispatchValue(commonInverterData?.Body?.Data.DcCurrent?.Value ?? 0);
                        break;
                    case "dc-voltage":
                        attr.DispatchValue(commonInverterData?.Body?.Data.DcVoltage?.Value ?? 0);
                        break;
                    case "current-day-energy":
                        attr.DispatchValue(commonInverterData?.Body?.Data.CurrentDayEnergy.Value);
                        break;
                    case "current-year-energy":
                        attr.DispatchValue(commonInverterData?.Body?.Data.CurrentYearEnergy.Value);
                        break;
                    case "total-energy":
                        attr.DispatchValue(commonInverterData?.Body?.Data.TotalEnergy.Value);
                        break;
                    case "device-status":
                        attr.DispatchValue(commonInverterData?.Body?.Data.DeviceStatus.StatusCode);
                        break;
                    case "error-code":
                        attr.DispatchValue(commonInverterData?.Body?.Data.DeviceStatus.ErrorCode);
                        break;
                    case "mgm-timer-remaining-time":
                        attr.DispatchValue(commonInverterData?.Body?.Data.DeviceStatus.MgmtTimerRemainingTime);
                        break;

                }
            }

        }

    }
}
