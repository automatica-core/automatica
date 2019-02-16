﻿using System;
using Automatica.Core.Base.IO;
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
        public override ushort[] ConvertValueToBus(IDispatchable source, object value)
        {
            try
            {
                var intValue = Convert.ToUInt16(value);
                var shortValue = (ushort)(intValue * Factor - Offset);
                return new[] { shortValue };
            }
            catch (Exception e)
            {
                DriverContext.Logger.LogError(e, $"Could not convert {value} to short");
                throw new InvalidInputValueException(e);
            }
          
        }

        public override object ConvertValueFromBus(IDispatchable source, ushort[] value)
        {
            return value[0];
        }
    }
}
