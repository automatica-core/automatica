using System;
using Automatica.Core.Driver;
using Automatica.Core.Driver.Exceptions;

namespace P3.Driver.ModBusDriverFactory.Attributes
{
    public class ModBusBinaryAttribute : ModBusAttribute
    {
        public ModBusBinaryAttribute(IDriverContext driverContext) : base(driverContext)
        {
        }

        public override int RegisterLength => 1;
        public override ushort[] ConvertValueToBus(object value, out object convertedValue)
        {
            try
            {
                var bo = Convert.ToBoolean(value);
                convertedValue = bo;
                
                return new [] { bo ? (ushort)1 : (ushort)0 };
            }
            catch (Exception e)
            {
                throw new InvalidInputValueException(e);
            }
        }

        public override object ConvertValueFromBus(ushort[] value)
        {
            if (value.Length == 1)
            {
                return value[0] == 1;
            }

            throw new ArgumentException(nameof(value));
        }

    }
}
