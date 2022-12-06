using System;
using System.Collections.Generic;
using Automatica.Core.Base.IO;
using Automatica.Core.Driver;
using Automatica.Core.Driver.Exceptions;
using Microsoft.Extensions.Logging;

namespace P3.Driver.ModBusDriverFactory.Attributes
{
    public class ModBus4ByteInteger : ModBus4ByteAttribute
    {
        public ModBus4ByteInteger(IDriverContext driverContext) : base(driverContext)
        {
        }
        
        protected override byte[] ConvertToBus(IDispatchable source, object value, out object convertedValue)
        {
            try
            {
                var intValue = Convert.ToUInt32(value);
                intValue = (uint)(intValue * Factor - Offset);

                convertedValue = intValue;

                var firstHalf = (ushort)(intValue >> 16);
                var secondHalf = (ushort)(intValue & 0xffff);

                var firstBytes = BitConverter.GetBytes(firstHalf);
                var secondBytes = BitConverter.GetBytes(secondHalf);

                var bytes = new List<byte>();
                bytes.AddRange(firstBytes);
                bytes.AddRange(secondBytes);
                return bytes.ToArray();
            }
            catch (Exception e)
            {
                DriverContext.Logger.LogError(e, $"Could not convert {value} to float");
                throw new InvalidInputValueException(e);
            }
        }

        protected override object ConvertFromBus(IDispatchable source, byte[] value)
        {
            var bytes = new[] { value[2], value[3], value[0], value[1] };

            var intValue = BitConverter.ToUInt32(bytes, 0);
            intValue = (uint)(intValue / Factor + Offset);

            return intValue;
        }
    }
}
