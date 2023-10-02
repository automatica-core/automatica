using System;
using Automatica.Core.Driver;
using P3.Driver.EnOcean.Data.Packets;
using P3.Driver.EnOcean.DriverFactory.Driver.Learned;

namespace P3.Driver.EnOcean.DriverFactory.Driver.Data
{
    public class EnOceanWinData : EnOceanDataNode
    {
        public EnOceanWinData(IDriverContext driverContext, ITeachInManager teachInManager) : base(driverContext, teachInManager)
        {
        }


        public override void TelegramReceived(RadioErp1Packet telegram)
        {
            var value = Convert.ToInt32(GetValueGeneric(telegram));

            var action = (value & 0x70) >> 4;

            switch (action)
            {
                case 0x07:
                    DispatchRead(1); // closed
                    break;
                case 0x04:
                case 0x06:
                    DispatchRead(0); //open
                    break;
                case 0x05:
                    DispatchRead(2); //tilt
                    break;
            }
        }

       
    }
}
