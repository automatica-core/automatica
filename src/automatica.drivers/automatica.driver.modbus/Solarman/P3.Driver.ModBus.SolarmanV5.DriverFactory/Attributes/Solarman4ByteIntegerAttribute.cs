using System;
using System.Threading.Tasks;
using Automatica.Core.Driver;

namespace P3.Driver.ModBus.SolarmanV5.DriverFactory.Attributes
{
    internal class Solarman4ByteIntegerAttribute : SolarmanAttrribute
    {
        public Solarman4ByteIntegerAttribute(IDriverContext driverContext, SolarmanGroupAttribute parent) : base(driverContext, parent)
        {
        }

        public override async Task<object> ConvertValue(ushort[] data)
        {
            await Task.CompletedTask;

            if (data.Length != 2)
            {
                throw new ArgumentException($"Expected 2 registers, got {data.Length}", nameof(data));
            }

            // low word first
            return SolarmanRegisterCodec.Decode32(data[0], data[1], IsSigned, Offset, Scale);
        }
    }
}
