using System.Threading.Tasks;
using Automatica.Core.Driver;

namespace P3.Driver.ModBus.SolarmanV5.DriverFactory.Attributes
{
    internal class Solarman2ByteIntegerAttribute : SolarmanAttrribute
    {
        public Solarman2ByteIntegerAttribute(IDriverContext driverContext, SolarmanGroupAttribute parent) : base(driverContext, parent)
        {
        }

        public override async Task<object> ConvertValue(ushort[] data)
        {
            await Task.CompletedTask;

            return SolarmanRegisterCodec.Decode16(data[0], IsSigned, Offset, Scale);
        }
    }
}
