using System;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using P3.Driver.EnOcean.Data.Packets;

namespace P3.Driver.EnOcean.DriverFactory.Driver.Simulated
{
    public class EnOceanSimulatedRelay : EnOceanSimulatedNode
    {
        public EnOceanSimulatedRelay(IDriverContext driverContext, P3.Driver.EnOcean.Driver driver) : base(driverContext, driver)
        {
        }

        protected override async Task Write(object value, IWriteContext writeContext, CancellationToken token = new CancellationToken())
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
                    await writeContext.DispatchValue(value, token);
                }
                else
                {
                    var dg = new RadioErp1Packet(Rorg.Rps, new ReadOnlyMemory<byte>(new byte[] { 0x30 }));

                    await Driver.SendTelegram(dg);
                    await Task.Delay(150);

                    dg = new RadioErp1Packet(Rorg.Rps, new ReadOnlyMemory<byte>(new byte[] { 0x20 }));
                    await Driver.SendTelegram(dg);
                    await writeContext.DispatchValue(value, token);
                }
            }
        }


    }
}
