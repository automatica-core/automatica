using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using Automatica.Core.EF.Exceptions;
using Microsoft.Extensions.Logging;
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

        /// <summary>
        /// The node template of this data point, null for nodes without a template.
        /// </summary>
        protected Guid? Template { get; private set; }

        /// <summary>
        /// Segmented scale (e.g. D2-14 temperature 0..250 = -40..+60 °C, 251..255 = error codes), null if the
        /// field scales with range/scale or not at all.
        /// </summary>
        internal IReadOnlyList<EepScaleSegment> ScaleSegments { get; set; }

        internal int BitOffset => BitOffs;
        internal int FieldLength => Length;

        /// <summary>
        /// The device (EEP case) node this data point belongs to, sends the commands.
        /// </summary>
        internal EnOceanTypeNode Device { get; set; }

        /// <summary>
        /// Raw value of the last successful write; the next command of the case carries it again, so writing one
        /// data point (e.g. the dim time) does not reset the others (e.g. the output value).
        /// </summary>
        internal long? WrittenRaw { get; private set; }

        /// <summary>
        /// The two-state enum mapping (first range = false, second range = true) only applies to fields whose
        /// enum really has two states. Nodes created by older driver versions carry the enum properties on
        /// multi-state fields as well (e.g. A5-10 fan speed), the template decides.
        /// </summary>
        protected bool UseBoolEnum => (!Template.HasValue || EnOceanEepMetadata.IsBoolEnum(Template.Value)) &&
                                      EnumFirstMin.HasValue && EnumFirstMax.HasValue && EnumSecondMin.HasValue && EnumSecondMax.HasValue;

        /// <summary>
        /// false for data points that are not a field of the telegram (teach-in).
        /// </summary>
        protected virtual bool HasField => true;

        protected EnOceanDataNode(IDriverContext driverContext, ITeachInManager teachInManager) : base(driverContext, teachInManager)
        {
        }

        protected override async Task Write(object value, IWriteContext writeContext, CancellationToken token = new CancellationToken())
        {
            await SendValue(value, writeContext);
        }

        /// <summary>
        /// Encodes <paramref name="value"/>, sends the command of the case and dispatches the value once the stick
        /// accepted the telegram.
        /// </summary>
        internal virtual async Task<bool> SendValue(object value, IWriteContext writeContext)
        {
            if (Device?.Outgoing == null)
            {
                DriverContext.Logger.LogWarning($"{Name}: the data point belongs to a telegram the device sends, it cannot be written");
                return false;
            }

            var raw = Encode(value);
            if (!raw.HasValue)
            {
                DriverContext.Logger.LogWarning($"{Name}: cannot encode {value ?? "null"}");
                return false;
            }

            if (!await Device.SendCommand(this, raw.Value))
            {
                DriverContext.Logger.LogWarning($"{Name}: sending {value} failed");
                return false;
            }

            WrittenRaw = raw;
            if (writeContext != null)
            {
                await writeContext.DispatchValue(value);
            }
            return true;
        }

        /// <summary>
        /// The counterpart of <see cref="GetValueGeneric(ReadOnlyMemory{byte})"/> and the two-state enum mapping: the raw
        /// field value for <paramref name="value"/>.
        /// </summary>
        /// <returns>null if the value cannot be converted</returns>
        internal long? Encode(object value)
        {
            if (value == null || Length < 1 || Length > 32)
            {
                return null;
            }

            var fieldMax = (1L << Length) - 1;
            long raw;
            if (value is bool b)
            {
                if (Length > 1 && UseBoolEnum)
                {
                    raw = b ? EnumSecondMin.Value : EnumFirstMin.Value;
                }
                else
                {
                    raw = b ? 1 : 0;
                }
            }
            else
            {
                double number;
                try
                {
                    number = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                }
                catch (Exception e) when (e is FormatException || e is InvalidCastException || e is OverflowException)
                {
                    return null;
                }

                if (double.IsNaN(number) || double.IsInfinity(number))
                {
                    return null;
                }

                if (ScaleSegments != null)
                {
                    var segment = FindSegment(number);
                    if (!segment.HasValue)
                    {
                        return null;
                    }
                    raw = Unscale(segment.Value.RangeMin, segment.Value.RangeMax, segment.Value.ScaleMin, segment.Value.ScaleMax, number);
                }
                else if (UseScale && UseRange)
                {
                    raw = Unscale(RangeMin, RangeMax, ScaleMin, ScaleMax, number);
                }
                else
                {
                    raw = (long)Math.Round(number, MidpointRounding.AwayFromZero);
                }
            }

            return raw < 0 || raw > fieldMax ? (long?)null : raw;
        }

        private EepScaleSegment? FindSegment(double value)
        {
            EepScaleSegment? nearest = null;
            var nearestDistance = double.MaxValue;
            foreach (var segment in ScaleSegments)
            {
                var min = Math.Min(segment.ScaleMin, segment.ScaleMax);
                var max = Math.Max(segment.ScaleMin, segment.ScaleMax);
                var distance = value < min ? min - value : value > max ? value - max : 0;
                if (distance < nearestDistance)
                {
                    nearest = segment;
                    nearestDistance = distance;
                }
            }

            return nearest;
        }

        /// <summary>
        /// Inverse of the linear scale, clamped to the range.
        /// </summary>
        private static long Unscale(long rangeMin, long rangeMax, double scaleMin, double scaleMax, double value)
        {
            if (scaleMax.Equals(scaleMin))
            {
                return rangeMin;
            }

            var raw = rangeMin + (value - scaleMin) * (rangeMax - rangeMin) / (scaleMax - scaleMin);
            var rounded = (long)Math.Round(raw, MidpointRounding.AwayFromZero);
            return Math.Min(Math.Max(rounded, Math.Min(rangeMin, rangeMax)), Math.Max(rangeMin, rangeMax));
        }

        protected override Task<bool> Read(IReadContext writeContext, CancellationToken token = new CancellationToken())
        {
            return Task.FromResult(false);
        }

        public override Task<bool> Init(CancellationToken token = default)
        {
            if (!HasField)
            {
                return base.Init(token);
            }

            BitOffs = GetPropertyValueInt("enocean-bitoffset");
            Length = GetPropertyValueInt("enocean-length");
            Template = DriverContext.NodeInstance?.This2NodeTemplate;
            ScaleSegments = Template.HasValue ? EnOceanEepMetadata.GetScaleSegments(Template.Value) : null;
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
                // would map every value to ScaleMin - treat it as "no scale" and dispatch the raw value.
                // A range the field cannot hold belongs to a value split over several fields (A5-13-06 LAT(LSB),
                // D2-01 MD_LSB/MD_MSB): scaling one part gives a wrong value, the part is dispatched raw.
                var fieldMax = Length >= 63 ? long.MaxValue : (1L << Length) - 1;
                if (UseRange && UseScale && RangeMax != RangeMin && !ScaleMax.Equals(ScaleMin) &&
                    Math.Max(RangeMin, RangeMax) <= fieldMax)
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

            if (ScaleSegments != null)
            {
                foreach (var segment in ScaleSegments)
                {
                    if (segment.Contains(raw.Value))
                    {
                        return segment.Scale(raw.Value);
                    }
                }

                // a status code (e.g. "sensor error", "not available") - no measurement value
                return null;
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
