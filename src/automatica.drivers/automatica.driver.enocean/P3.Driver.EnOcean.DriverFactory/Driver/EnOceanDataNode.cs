using System;
using System.Linq;
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

                if (UseRange && UseScale)
                {
                    Multiplier = (ScaleMax - ScaleMin) / (RangeMax - RangeMin);
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

        public object GetValueGeneric(ReadOnlyMemory<byte> data)
        {
            var dif = BitOffs / 8;
            var mod = BitOffs % Length;

            var size = Length / 8;

            int index;
            if (dif > 0)
            {
                index = BitOffs - (dif * 8);
                index = Math.Abs(index - 7);
            }
            else
            {
                index = Math.Abs(BitOffs - 7);
            }

            if (!data.IsEmpty && data.Length >= 1)
            {
                if (mod == 0 && size == 1) // full byte
                {
                    var b = data.Span[dif];
                    return ScaleMinMax(b);
                }
                if (Length > 8 && Length < 14) //variable byte
                {
                    return GetVariableByteLengthValue(index, data.Span[dif + 1], data.Span[dif]);
                }

                if (Length < 8)
                {
                    var bitMask = (byte)0x0;
                    for (var i = 0; i < Length; i++)
                    {
                        var bitIndex = index - i;
                        bitMask = Automatica.Core.Driver.Utility.Utils.SetBitsTo1(bitMask, (byte)(bitIndex));
                    }

                    if (BitOffs < 8 && Length > 1)
                    {
                        bitMask <<= BitOffs;
                    }

                    var ret = (data.Span[dif] & bitMask) >> (index - Length + 1);
                    if (Length == 1)
                    {
                        return ret >= 1;
                    }

                    if (BitOffs < 8 && Length > 1)
                    {
                        return ret >> BitOffs;
                    }

                    return ret;
                }

            }

            return null;
        }

        public object GetValueGeneric(RadioErp1Packet telegram)
        {
            return GetValueGeneric(telegram.Data);
        }

        internal object GetVariableByteLengthValue(int index, byte b1, byte b2)
        {
            var bitMask = (byte)0x0;
            var b2Len = Length - 8;

            for (var i = 0; i < b2Len; i++)
            {
                var bitIndex = index - i;
                bitMask = Automatica.Core.Driver.Utility.Utils.SetBitsTo1(bitMask, (byte)(bitIndex));
            }


            var val = b1 + ((b2 & bitMask) << 8);
            return ScaleMinMax(val);
        }

        public object ScaleMinMax(int value)
        {
            if (!UseScale && !UseRange)
            {
                return value;
            }
            return Multiplier * (value - RangeMin) + ScaleMin;
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            return null;
        }
    }
}
