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
                DispatchValue(data);
            }
        }
    }
}
