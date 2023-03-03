using Automatica.Core.Driver;
using P3.Driver.EnOcean.Data.Packets;
using P3.Driver.EnOcean.DriverFactory.Driver.Learned;

namespace P3.Driver.EnOcean.DriverFactory.Driver.Data
{
    public class EnOceanGenericData : EnOceanDataNode
    {
        public EnOceanGenericData(IDriverContext driverContext, ITeachInManager teachInManager) : base(driverContext, teachInManager)
        {
        }

        public override void TelegramReceived(RadioErp1Packet telegram)
        {
            var data = GetValueGeneric(telegram);

            if (data != null)
            {
                if (data is int dataInt && EnumFirstMax.HasValue && EnumFirstMin.HasValue && EnumSecondMax.HasValue && EnumSecondMin.HasValue)
                {
                    if (dataInt >= EnumFirstMin && dataInt <= EnumFirstMax)
                    {
                        DispatchValue(0);
                    }
                    else if (dataInt >= EnumSecondMin && dataInt >= EnumSecondMax)
                    {
                        DispatchValue(1);
                    }
                    else
                    {
                        DispatchValue(data);
                    }
                }
                else
                {
                    DispatchValue(data);
                }
            }
        }
    }
}
