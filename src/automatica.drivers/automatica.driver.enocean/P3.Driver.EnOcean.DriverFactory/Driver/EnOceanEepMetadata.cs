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
    /// An EEP case Automatica sends (actuator command): the profile and the telegram layout before the written values
    /// are set.
    /// </summary>
    internal sealed class EepOutgoing
    {
        public EepOutgoing(byte rorg, byte func, byte type, int size, EepCondition[] defaults)
        {
            Rorg = rorg;
            Func = func;
            Type = type;
            Size = size;
            Defaults = defaults;
        }

        public byte Rorg { get; }
        public byte Func { get; }
        public byte Type { get; }

        /// <summary>
        /// User data length in bytes.
        /// </summary>
        public int Size { get; }

        /// <summary>
        /// Case constants (e.g. CMD 0x1), "do not change" / "all channels" values and the LRN bit of 4BS data telegrams.
        /// </summary>
        public IReadOnlyList<EepCondition> Defaults { get; }
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
        private static readonly Dictionary<Guid, EepOutgoing> OutgoingTypes = new Dictionary<Guid, EepOutgoing>();
        private static readonly Dictionary<Guid, Guid> ReplyCommands = new Dictionary<Guid, Guid>();
        private static readonly HashSet<Guid> CommandTypes = new HashSet<Guid>();

        static EnOceanEepMetadata()
        {
            Load();
            LoadReplies();
        }

        /// <summary>
        /// A5-20 HVAC components (battery valves, room panels) only listen for about a second after their own
        /// telegram: the controller answers every status telegram with the command case. Status case (actuator to
        /// controller) -> command case (controller to actuator).
        /// </summary>
        private static void LoadReplies()
        {
            Reply(EnOceanRorgA5Data.TypeA5_20_01_1Guid, EnOceanRorgA5Data.TypeA5_20_01_2Guid, 0x01);
            Reply(EnOceanRorgA5Data.TypeA5_20_02_3Guid, EnOceanRorgA5Data.TypeA5_20_02_4Guid, 0x02);
            Reply(EnOceanRorgA5Data.TypeA5_20_03_5Guid, EnOceanRorgA5Data.TypeA5_20_03_6Guid, 0x03);
            Reply(EnOceanRorgA5Data.TypeA5_20_04_7Guid, EnOceanRorgA5Data.TypeA5_20_04_8Guid, 0x04);
            Reply(EnOceanRorgA5Data.TypeA5_20_05_9Guid, EnOceanRorgA5Data.TypeA5_20_05_10Guid, 0x05);
            Reply(EnOceanRorgA5Data.TypeA5_20_06_16Guid, EnOceanRorgA5Data.TypeA5_20_06_17Guid, 0x06);
        }

        private static void Reply(Guid statusType, Guid commandType, byte type)
        {
            ReplyCommands[statusType] = commandType;
            CommandTypes.Add(commandType);
            // a 4BS data telegram: LRN bit (DB0.3) = 1
            OutgoingTypes.TryAdd(commandType, new EepOutgoing(0xA5, 0x20, type, 4, new[] { new EepCondition(28, 1, 0x1) }));
        }

        /// <returns>the command case the device expects as answer to a telegram of <paramref name="statusType"/>, null if none</returns>
        public static Guid? GetReplyCommand(Guid statusType)
        {
            return ReplyCommands.TryGetValue(statusType, out var command) ? command : (Guid?)null;
        }

        /// <returns>true if the type is a command Automatica only sends as answer to a status telegram of the device</returns>
        public static bool IsReplyCommand(Guid typeTemplate)
        {
            return CommandTypes.Contains(typeTemplate);
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

        private static void Outgoing(Guid typeTemplate, byte rorg, byte func, byte type, int size, params (int Offset, int Length, long Value)[] defaults)
        {
            var list = new EepCondition[defaults.Length];
            for (var i = 0; i < defaults.Length; i++)
            {
                list[i] = new EepCondition(defaults[i].Offset, defaults[i].Length, defaults[i].Value);
            }
            OutgoingTypes[typeTemplate] = new EepOutgoing(rorg, func, type, size, list);
        }

        /// <returns>null if the type is not a command Automatica sends</returns>
        public static EepOutgoing GetOutgoing(Guid typeTemplate)
        {
            return OutgoingTypes.TryGetValue(typeTemplate, out var outgoing) ? outgoing : null;
        }

        /// <summary>
        /// Writes <paramref name="value"/> into <paramref name="length"/> bits at <paramref name="offset"/> (EEP bit
        /// numbering, offset 0 = MSB of the first byte), the counterpart of <see cref="EnOceanDataNode.ExtractBits"/>.
        /// </summary>
        public static void SetBits(byte[] data, int offset, int length, long value)
        {
            for (var i = 0; i < length; i++)
            {
                var pos = offset + i;
                if (pos < 0 || pos >= data.Length * 8)
                {
                    continue;
                }

                var mask = (byte)(0x80 >> (pos & 7));
                if (((value >> (length - 1 - i)) & 1) == 1)
                {
                    data[pos >> 3] |= mask;
                }
                else
                {
                    data[pos >> 3] &= (byte)~mask;
                }
            }
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
