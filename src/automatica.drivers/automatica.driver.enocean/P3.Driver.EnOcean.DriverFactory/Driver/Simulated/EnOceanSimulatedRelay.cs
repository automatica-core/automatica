using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.Driver;
using P3.Driver.EnOcean.Data.Packets;

namespace P3.Driver.EnOcean.DriverFactory.Driver.Simulated
{
    public class EnOceanSimulatedRelay : EnOceanSimulatedNode
    {
        public EnOceanSimulatedRelay(IDriverContext driverContext, P3.Driver.EnOcean.Driver driver) : base(driverContext, driver)
        {
        }

        public override async Task WriteValue(IDispatchable source, object value)
        {
            if (bool.TryParse(value.ToString(), out bool bolValue))
            {
                if (bolValue)
                {
                    var dg = new RadioErp1Packet(Rorg.Rps, new ReadOnlyMemory<byte>(new byte[] { 0x10 }));
                    
                    await Driver.SendTelegram(dg);
                    await Task.Delay(150);

                    dg = new RadioErp1Packet(Rorg.Rps, new ReadOnlyMemory<byte>(new byte[] { 0x00 }));
                    await Driver.SendTelegram(dg);
                }
                else
                {
                    var dg = new RadioErp1Packet(Rorg.Rps, new ReadOnlyMemory<byte>(new byte[] { 0x30 }));

                    await Driver.SendTelegram(dg);
                    await Task.Delay(150);

                    dg = new RadioErp1Packet(Rorg.Rps, new ReadOnlyMemory<byte>(new byte[] { 0x20 }));
                    await Driver.SendTelegram(dg);
                }
            }
        }
    }
}
