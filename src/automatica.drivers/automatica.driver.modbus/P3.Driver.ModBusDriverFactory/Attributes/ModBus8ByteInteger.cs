using System;
using System.Collections.Generic;
using Automatica.Core.Driver;
using Automatica.Core.Driver.Exceptions;
using Microsoft.Extensions.Logging;

namespace P3.Driver.ModBusDriverFactory.Attributes
{
    public class ModBus8ByteInteger : ModBus8ByteAttribute
    {
        public ModBus8ByteInteger(IDriverContext driverContext) : base(driverContext)
        {
        }

        protected override byte[] ConvertToBus(object value, out object convertedValue)
        {
            try
            {
                var intValue = Convert.ToUInt64(value);
                intValue = ((UInt64)(intValue * Factor) - (UInt64)Offset);
                convertedValue = intValue;

                var firstQ = (ushort)(intValue >> 48);
                var secondQ = (ushort)(intValue >> 32);
                var thirdQ = (ushort)(intValue >> 16);
                var fourthQ = (ushort)(intValue & 0xffff);

                var bytes = new List<byte>();
                bytes.AddRange(BitConverter.GetBytes(firstQ));
                bytes.AddRange(BitConverter.GetBytes(secondQ));
                bytes.AddRange(BitConverter.GetBytes(thirdQ));
                bytes.AddRange(BitConverter.GetBytes(fourthQ));

                return bytes.ToArray();
            }
            catch (Exception e)
            {
                DriverContext.Logger.LogError(e, $"Could not convert {value} to int64");
                throw new InvalidInputValueException(e);
            }
        }

        protected override object ConvertFromBus(byte[] value)
        {
            var intValue = BitConverter.ToUInt64(value, 0);
            intValue = ((UInt64)(intValue / Factor) + (UInt64)Offset);
            return intValue;
        }
    }
}
