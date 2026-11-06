using System;
using System.Collections.Generic;
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

        /// <summary>
        /// true for the "±4 Byte" template (two's complement int), false for the "+4 Byte" template (uint)
        /// </summary>
        protected virtual bool IsSigned => false;

        protected override byte[] ConvertToBus(object value, out object convertedValue)
        {
            try
            {
                uint intValue;
                if (IsSigned)
                {
                    var signedValue = (int)ScaleToBusInteger(value, int.MinValue, int.MaxValue);
                    convertedValue = signedValue;
                    intValue = unchecked((uint)signedValue);
                }
                else
                {
                    intValue = (uint)ScaleToBusInteger(value, uint.MinValue, uint.MaxValue);
                    convertedValue = intValue;
                }

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
                DriverContext.Logger.LogError(e, $"Could not convert {value} to int");
                throw e as InvalidInputValueException ?? new InvalidInputValueException(e);
            }
        }

        protected override object ConvertFromBus(byte[] value)
        {
            var bytes = new[] { value[2], value[3], value[0], value[1] };

            var intValue = BitConverter.ToUInt32(bytes, 0);

            if (IsSigned)
            {
                var signedValue = unchecked((int)intValue);
                return ScaleIntegerFromBus(signedValue, signedValue);
            }

            return ScaleIntegerFromBus(intValue, intValue);
        }
    }

    /// <summary>
    /// "±4 Byte" template, the registers hold a two's complement 32 bit value
    /// </summary>
    public class ModBus4ByteSignedInteger : ModBus4ByteInteger
    {
        public ModBus4ByteSignedInteger(IDriverContext driverContext) : base(driverContext)
        {
        }

        protected override bool IsSigned => true;
    }
}
