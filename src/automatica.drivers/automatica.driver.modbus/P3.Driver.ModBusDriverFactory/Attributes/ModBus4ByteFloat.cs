﻿using System;
using Automatica.Core.Driver;
using Automatica.Core.Driver.Exceptions;
using Microsoft.Extensions.Logging;

namespace P3.Driver.ModBusDriverFactory.Attributes
{
    public class ModBus4ByteFloat : ModBus4ByteAttribute
    {
        public ModBus4ByteFloat(IDriverContext driverContext) : base(driverContext)
        {
     
        }

        protected override byte[] ConvertToBus(object value, out object convertedValue)
        {
            try
            {
                var dValue = Convert.ToSingle(value);
                dValue = (float)(dValue * Factor - Offset);
                
                convertedValue = dValue;

                var bytes = BitConverter.GetBytes(dValue);

                return new[] { bytes[2], bytes[3], bytes[0], bytes[1] };
            }
            catch (Exception e)
            {
                DriverContext.Logger.LogError(e, $"Could not convert {value} to int");
                throw new InvalidInputValueException(e);
            }
        }

        protected override object ConvertFromBus(byte[] value)
        {
            if (value.Length != 4)
            {
                throw new ArgumentException(nameof(value));
            }
            var bytes = new[] { value[2], value[3], value[0], value[1] };
            
            var val = BitConverter.ToSingle(bytes, 0);

            var conVal = (float)(val / Factor + Offset);
            
            return conVal;
        }
    }
}
