using System;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using Automatica.Core.EF.Exceptions;
using P3.Driver.EnOcean.Data.Packets;
using P3.Driver.EnOcean.DriverFactory.Driver.Learned;

namespace P3.Driver.EnOcean.DriverFactory.Driver
{
    public abstract class EnOceanDataNode : EnOceanBaseNode<EnOceanDataNode>
    {
        protected int BitOffs { get; private set; }
        protected int Length { get; private set; }

        public bool UseRange { get; set; }
        public long RangeMin { get; set; }
        public long RangeMax { get; set; }

        public bool UseScale { get; set; }
        public double ScaleMin { get; set; }
        public double ScaleMax { get; set; }

        public double Multiplier { get; set; }

        public long? EnumFirstMin { get; set; }
        public long? EnumFirstMax { get; set; }
        public long? EnumSecondMin { get; set; }
        public long? EnumSecondMax { get; set; }


        protected EnOceanDataNode(IDriverContext driverContext, ITeachInManager teachInManager) : base(driverContext, teachInManager)
        {
        }
        protected override Task Write(object value, IWriteContext writeContext, CancellationToken token = new CancellationToken())
        {
            return Task.CompletedTask;
        }

        protected override Task<bool> Read(IReadContext writeContext, CancellationToken token = new CancellationToken())
        {
            return Task.FromResult(false);
        }

        public override Task<bool> Init(CancellationToken token = default)
        {
            BitOffs = GetPropertyValueInt("enocean-bitoffset");
            Length = GetPropertyValueInt("enocean-length");
            try
            {
                var rangeMinProp = GetProperty("enocean-range-min");
                var rangeMaxProp = GetProperty("enocean-range-max");
                var scaleMinProp = GetProperty("enocean-scale-min");
                var scaleMaxProp = GetProperty("enocean-scale-max");

                if (rangeMinProp != null && rangeMaxProp != null)
                {
                    UseRange = true;
                    RangeMin = GetPropertyValueLong("enocean-range-min");
                    RangeMax = GetPropertyValueLong("enocean-range-max");
                }

                if (scaleMinProp != null && scaleMaxProp != null)
                {
                    UseScale = true;
                    ScaleMin = GetPropertyValueDouble("enocean-scale-min");
                    ScaleMax = GetPropertyValueDouble("enocean-scale-max");
                }
                else
                {
                    ScaleMax = 1;
                    ScaleMin = 0;
                }

                // a degenerate range/scale (e.g. A5-12 MR: scale 0..0, real scaling depends on DIV/DT)
                // would map every value to ScaleMin - treat it as "no scale" and dispatch the raw value
                if (UseRange && UseScale && RangeMax != RangeMin && !ScaleMax.Equals(ScaleMin))
                {
                    Multiplier = (ScaleMax - ScaleMin) / (RangeMax - RangeMin);
                }
                else
                {
                    UseScale = false;
                }
            }
            catch (PropertyNotFoundException)
            {
                //ignore
            }
            try 
            {
                var enumFirstMin = GetProperty("enocean-enum-first-min");
                var enumFirstMax = GetProperty("enocean-enum-first-max");
                var enumSecondMin= GetProperty("enocean-enum-second-min");
                var enumSecondMax = GetProperty("enocean-enum-second-max");

                if (enumFirstMin != null && enumFirstMax != null && enumSecondMin != null && enumSecondMax != null)
                {
                    EnumFirstMin = enumFirstMin.ValueLong;
                    EnumFirstMax = enumFirstMax.ValueLong;
                    EnumSecondMin = enumSecondMin.ValueLong;
                    EnumSecondMax = enumSecondMax.ValueLong;
                }
            }
            catch (PropertyNotFoundException)
            {
                //ignore
            }
            return base.Init(token);
        }

        /// <summary>
        /// Reads <see cref="Length"/> bits starting at <see cref="BitOffs"/>, EEP bit numbering:
        /// the user data is one big-endian bit string, offset 0 is the MSB of the first data byte.
        /// </summary>
        /// <returns>null if the field does not fit into the data</returns>
        internal static long? ExtractBits(ReadOnlySpan<byte> data, int bitOffset, int length)
        {
            if (length < 1 || length > 32 || bitOffset < 0 || bitOffset + length > data.Length * 8)
            {
                return null;
            }

            long value = 0;
            for (var i = 0; i < length; i++)
            {
                var bit = bitOffset + i;
                var bitValue = (data[bit >> 3] >> (7 - (bit & 7))) & 1;
                value = (value << 1) | (long)bitValue;
            }

            return value;
        }

        public object GetValueGeneric(ReadOnlyMemory<byte> data)
        {
            var raw = ExtractBits(data.Span, BitOffs, Length);

            if (!raw.HasValue)
            {
                return null;
            }

            if (Length == 1)
            {
                return raw.Value == 1;
            }

            return ScaleMinMax(raw.Value);
        }

        public object GetValueGeneric(RadioErp1Packet telegram)
        {
            return GetValueGeneric(telegram.Data);
        }

        public object ScaleMinMax(long value)
        {
            if (!UseScale || !UseRange)
            {
                // keep int for everything that fits, the enum/bit nodes match on int
                return value <= int.MaxValue ? (object)(int)value : value;
            }
            return Multiplier * (value - RangeMin) + ScaleMin;
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            return null;
        }
    }
}
