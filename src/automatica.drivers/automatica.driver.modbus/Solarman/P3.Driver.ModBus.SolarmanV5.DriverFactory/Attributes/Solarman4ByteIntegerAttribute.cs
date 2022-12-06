using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using Newtonsoft.Json.Linq;
using P3.Driver.ModBusDriver.Master;

namespace P3.Driver.ModBus.SolarmanV5.DriverFactory.Attributes
{
    internal class Solarman4ByteIntegerAttribute : SolarmanAttrribute
    {
        public Solarman4ByteIntegerAttribute(IDriverContext driverContext, SolarmanGroupAttribute parent) : base(driverContext, parent)
        {
        }

        public override async Task<object> ConvertValue(ModBusRegisterValueReturn modbusReturn)
        {
            await Task.CompletedTask;

            var value = new List<byte>();

            foreach (var va in modbusReturn.Data)
            {
                value.AddRange(BitConverter.GetBytes(va));
            }

            
            if (value.Count != 4)
            {
                throw new ArgumentException(nameof(value));
            }

            var bytes = new[] { value[0], value[1], value[2], value[3] };

            var val = BitConverter.ToInt32(bytes, 0);

            if (Offset > 0)
            {
                val -= Offset;
            }

            if (Scale > 1.0 || Scale < 1.0)
            {
                return Math.Round(val * Scale, 2);
            }

            return val;
        }
    }
}
