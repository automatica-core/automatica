using System;
using System.Collections.Generic;

namespace P3.Driver.EnOcean.DriverFactory.Driver
{
    /// <summary>
    /// A data field value a telegram must carry to belong to an EEP case (e.g. D2-01-00 CMD = 0x4 actuator status).
    /// </summary>
    internal struct EepCondition
    {
        public EepCondition(int offset, int length, long value)
        {
            Offset = offset;
            Length = length;
            Value = value;
        }

        public int Offset { get; }
        public int Length { get; }
        public long Value { get; }
    }

    /// <summary>
    /// Raw values RangeMin..RangeMax map linearly to ScaleMin..ScaleMax, raw values outside every segment are status codes.
    /// </summary>
    internal struct EepScaleSegment
    {
        public EepScaleSegment(long rangeMin, long rangeMax, double scaleMin, double scaleMax)
        {
            RangeMin = rangeMin;
            RangeMax = rangeMax;
            ScaleMin = scaleMin;
            ScaleMax = scaleMax;
        }

        public long RangeMin { get; }
        public long RangeMax { get; }
        public double ScaleMin { get; }
        public double ScaleMax { get; }

        public bool Contains(long raw) => raw >= Math.Min(RangeMin, RangeMax) && raw <= Math.Max(RangeMin, RangeMax);

        public double Scale(long raw) => RangeMax == RangeMin
            ? ScaleMin
            : ScaleMin + (raw - RangeMin) * (ScaleMax - ScaleMin) / (RangeMax - RangeMin);
    }

    /// <summary>
    /// EEP semantics the node properties cannot express, looked up by node template guid (generated from eep.xml,
    /// see EnOceanEepMetadata.Generated.cs). Keyed by template so it applies to existing node instances as well.
    /// </summary>
    internal static partial class EnOceanEepMetadata
    {
        private static readonly Dictionary<Guid, EepCondition[]> TypeConditions = new Dictionary<Guid, EepCondition[]>();
        private static readonly Dictionary<Guid, EepScaleSegment[]> ScaleSegments = new Dictionary<Guid, EepScaleSegment[]>();
        private static readonly HashSet<Guid> BoolEnums = new HashSet<Guid>();

        static EnOceanEepMetadata()
        {
            Load();
        }

        static partial void Load();

        private static void Condition(Guid typeTemplate, params (int Offset, int Length, long Value)[] conditions)
        {
            var list = new EepCondition[conditions.Length];
            for (var i = 0; i < conditions.Length; i++)
            {
                list[i] = new EepCondition(conditions[i].Offset, conditions[i].Length, conditions[i].Value);
            }
            TypeConditions[typeTemplate] = list;
        }

        private static void Segments(Guid dataFieldTemplate, params (long RangeMin, long RangeMax, double ScaleMin, double ScaleMax)[] segments)
        {
            var list = new EepScaleSegment[segments.Length];
            for (var i = 0; i < segments.Length; i++)
            {
                list[i] = new EepScaleSegment(segments[i].RangeMin, segments[i].RangeMax, segments[i].ScaleMin, segments[i].ScaleMax);
            }
            ScaleSegments[dataFieldTemplate] = list;
        }

        private static void BoolEnum(Guid dataFieldTemplate)
        {
            BoolEnums.Add(dataFieldTemplate);
        }

        /// <returns>empty if every telegram of the device belongs to the type</returns>
        public static IReadOnlyList<EepCondition> GetConditions(Guid typeTemplate)
        {
            return TypeConditions.TryGetValue(typeTemplate, out var conditions) ? conditions : Array.Empty<EepCondition>();
        }

        /// <returns>null if the data field has no segmented scale</returns>
        public static IReadOnlyList<EepScaleSegment> GetScaleSegments(Guid dataFieldTemplate)
        {
            return ScaleSegments.TryGetValue(dataFieldTemplate, out var segments) ? segments : null;
        }

        public static bool IsBoolEnum(Guid dataFieldTemplate)
        {
            return BoolEnums.Contains(dataFieldTemplate);
        }

        /// <returns>true if the user data carries every condition value</returns>
        public static bool Matches(IReadOnlyList<EepCondition> conditions, ReadOnlySpan<byte> data)
        {
            foreach (var condition in conditions)
            {
                var value = EnOceanDataNode.ExtractBits(data, condition.Offset, condition.Length);
                if (value != condition.Value)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
