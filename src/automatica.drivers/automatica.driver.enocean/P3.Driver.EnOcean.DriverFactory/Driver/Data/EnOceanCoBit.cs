using Automatica.Core.Driver;
using P3.Driver.EnOcean.Data.Packets;
using P3.Driver.EnOcean.DriverFactory.Driver.Learned;

namespace P3.Driver.EnOcean.DriverFactory.Driver.Data
{
    public class EnOceanCoBit : EnOceanDataNode
    {
        public EnOceanCoBit(IDriverContext driverContext, ITeachInManager teachInManager) : base(driverContext, teachInManager)
        {
        }


        public override void TelegramReceived(RadioErp1Packet telegram)
        {
            var value = GetValueGeneric(telegram);
            if (value != null && value is int bValue)
            {
                DispatchValue(bValue > 0);
            }
            else if (value is bool)
            {
                DispatchValue(value);
            }
        }
    }
}
