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

        /// <summary>
        /// true for the "±2 Byte" template (two's complement short), false for the "+2 Byte" template (ushort)
        /// </summary>
        protected virtual bool IsSigned => false;

        public override int RegisterLength => 1;
        public override ushort[] ConvertValueToBus(object value, out object convertedValue)
        {
            try
            {
                if (IsSigned)
                {
                    var signedValue = (short)ScaleToBusInteger(value, short.MinValue, short.MaxValue);
                    convertedValue = signedValue;
                    return new[] { unchecked((ushort)signedValue) };
                }

                var shortValue = (ushort)ScaleToBusInteger(value, ushort.MinValue, ushort.MaxValue);
                convertedValue = shortValue;
                return new[] { shortValue };
            }
            catch (Exception e)
            {
                DriverContext.Logger.LogError(e, $"Could not convert {value} to short");
                throw e as InvalidInputValueException ?? new InvalidInputValueException(e);
            }
        }

        public override object ConvertValueFromBus(ushort[] value)
        {
            if (IsSigned)
            {
                var signedValue = unchecked((short)value[0]);
                return ScaleIntegerFromBus(signedValue, signedValue);
            }

            return ScaleIntegerFromBus(value[0], value[0]);
        }
    }

    /// <summary>
    /// "±2 Byte" template, the register holds a two's complement 16 bit value
    /// </summary>
    public class ModBus2ByteSignedInteger : ModBus2ByteInteger
    {
        public ModBus2ByteSignedInteger(IDriverContext driverContext) : base(driverContext)
        {
        }

        protected override bool IsSigned => true;
    }
}
