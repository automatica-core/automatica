using System;
using Automatica.Core.Driver;
using Automatica.Core.Driver.Exceptions;
using Microsoft.Extensions.Logging;

namespace P3.Driver.ModBusDriverFactory.Attributes
{
    public class ModBus8ByteFloat  : ModBus8ByteAttribute
    {
        public ModBus8ByteFloat(IDriverContext driverContext) : base(driverContext)
        {
        }
        
        protected override byte[] ConvertToBus(object value, out object convertedValue)
        {
            try
            {
                var dblValue = Convert.ToDouble(value);
                dblValue = dblValue * Factor - Offset;
                convertedValue = dblValue;

                var bytes = BitConverter.GetBytes(dblValue);

                return new[] { bytes[6], bytes[7], bytes[4], bytes[5] , bytes[2], bytes[3], bytes[0], bytes[1] };
            }
            catch (Exception e)
            {
                DriverContext.Logger.LogError(e, $"Could not convert {value} to int64");
                throw new InvalidInputValueException(e);
            }
        }

        protected override object ConvertFromBus(byte[] value)
        {
           
            var dblValue = BitConverter.ToDouble(value, 0);
            dblValue = dblValue / Factor + Offset;

            return dblValue;
        }
    }
}
