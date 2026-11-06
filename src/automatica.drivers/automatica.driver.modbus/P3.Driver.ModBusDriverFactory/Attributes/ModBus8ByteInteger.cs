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

        /// <summary>
        /// true for the "±8 Byte" template (two's complement long), false for the "+8 Byte" template (ulong)
        /// </summary>
        protected virtual bool IsSigned => false;

        protected override byte[] ConvertToBus(object value, out object convertedValue)
        {
            try
            {
                ulong intValue;
                if (IsSigned)
                {
                    var signedValue = (long)ScaleToBusInteger(value, long.MinValue, long.MaxValue);
                    convertedValue = signedValue;
                    intValue = unchecked((ulong)signedValue);
                }
                else
                {
                    intValue = (ulong)ScaleToBusInteger(value, ulong.MinValue, ulong.MaxValue);
                    convertedValue = intValue;
                }

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
                throw e as InvalidInputValueException ?? new InvalidInputValueException(e);
            }
        }

        protected override object ConvertFromBus(byte[] value)
        {
            var intValue = BitConverter.ToUInt64(value, 0);

            if (IsSigned)
            {
                var signedValue = unchecked((long)intValue);
                return ScaleIntegerFromBus(signedValue, signedValue);
            }

            return ScaleIntegerFromBus(intValue, intValue);
        }
    }

    /// <summary>
    /// "±8 Byte" template, the registers hold a two's complement 64 bit value
    /// </summary>
    public class ModBus8ByteSignedInteger : ModBus8ByteInteger
    {
        public ModBus8ByteSignedInteger(IDriverContext driverContext) : base(driverContext)
        {
        }

        protected override bool IsSigned => true;
    }
}
