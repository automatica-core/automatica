using System;
using Automatica.Core.Driver;
using Automatica.Core.Driver.Exceptions;
using Microsoft.Extensions.Logging;

namespace P3.Driver.ModBusDriverFactory.Attributes
{
    public class ModBus2ByteInteger : ModBusAttribute
    {
        public ModBus2ByteInteger(IDriverContext driverContext) : base(driverContext)
        {
        }

        public override int RegisterLength => 1;
        public override ushort[] ConvertValueToBus(object value, out object convertedValue)
        {
            try
            {
                var intValue = Convert.ToUInt16(value);
                var shortValue = (ushort)(intValue * Factor - Offset);
                convertedValue = shortValue;
                return new[] { shortValue };
            }
            catch (Exception e)
            {
                DriverContext.Logger.LogError(e, $"Could not convert {value} to short");
                throw new InvalidInputValueException(e);
            }
          
        }

        public override object ConvertValueFromBus(ushort[] value)
        {
            return value[0];
        }
    }
}
