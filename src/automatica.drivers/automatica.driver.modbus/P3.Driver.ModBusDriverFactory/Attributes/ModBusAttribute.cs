using System;
using Automatica.Core.Driver;
using Automatica.Core.Driver.Exceptions;
using P3.Driver.ModBusDriver;

namespace P3.Driver.ModBusDriverFactory.Attributes
{
    public abstract class ModBusAttribute
    {
        public ModBusTable Table { get; }
        public ushort Register { get; }

        public double Factor { get; set; }
        public double Offset { get; set; }

        protected ModBusAttribute(IDriverContext driverContext)
        {
            Table = (ModBusTable)driverContext.NodeInstance.GetProperty("modbus-table").ValueInt.Value;
            Register = (ushort)driverContext.NodeInstance.GetProperty("modbus-register").ValueInt.Value;

            var fa = driverContext.NodeInstance.GetPropertyValue("factor", 1d);
            Factor = (double) fa;
            
            var off = driverContext.NodeInstance.GetPropertyValue("offset", 0d);
            Offset = (double) off;
            DriverContext = driverContext;
        }

        public abstract int RegisterLength { get; }
        public IDriverContext DriverContext { get; }

        public abstract ushort[] ConvertValueToBus(object value, out object convertedValue);

        public abstract object ConvertValueFromBus(ushort[] value);

        /// <summary>
        /// A factor of 0 would divide by zero when reading, it is treated as 1
        /// </summary>
        protected double EffectiveFactor => Factor == 0 ? 1 : Factor;

        protected bool IsIdentityScale => EffectiveFactor == 1 && Offset == 0;

        /// <summary>
        /// The read formula is the reference for all types: value = raw / factor + offset
        /// </summary>
        protected double ScaleFromBus(double raw)
        {
            return raw / EffectiveFactor + Offset;
        }

        /// <summary>
        /// Exact inverse of <see cref="ScaleFromBus"/>: raw = (value - offset) * factor
        /// </summary>
        protected double ScaleToBus(double value)
        {
            return IsIdentityScale ? value : (value - Offset) * EffectiveFactor;
        }

        /// <summary>
        /// Scales <paramref name="value"/> to the raw bus value (raw = (value - offset) * factor), rounds it (after scaling)
        /// and checks it against [<paramref name="min"/>, <paramref name="max"/>]. decimal keeps 64 bit values exact.
        /// </summary>
        protected decimal ScaleToBusInteger(object value, decimal min, decimal max)
        {
            decimal raw;
            try
            {
                var dec = Convert.ToDecimal(value);
                raw = IsIdentityScale ? dec : (dec - (decimal)Offset) * (decimal)EffectiveFactor;
                raw = Math.Round(raw, 0, MidpointRounding.AwayFromZero);
            }
            catch (Exception e)
            {
                throw new InvalidInputValueException(e);
            }

            if (raw < min || raw > max)
            {
                throw new InvalidInputValueException(new ArgumentOutOfRangeException(nameof(value),
                    $"{value} (raw {raw}) is out of range [{min}, {max}]"));
            }

            return raw;
        }

        /// <summary>
        /// Without scaling the raw value is returned unchanged (keeps its integer type), otherwise raw / factor + offset as double
        /// </summary>
        protected object ScaleIntegerFromBus(object typedRaw, double raw)
        {
            if (IsIdentityScale)
            {
                return typedRaw;
            }

            return ScaleFromBus(raw);
        }
    }
}
