using System;
using System.Collections.Generic;

namespace P3.Driver.Loxone.Miniserver.Driver.Data.Message
{
    public class DaytimerEntry
    {
        public const int LoxoneStructLength = 24;
        private DaytimerEntry(int mode, int from, int to, int needActivate, double value)
        {
            Mode = mode;
            From = from;
            To = to;
            NeedActivate = needActivate;
            Value = value;
        }

        public static DaytimerEntry Parse(Span<byte> data)
        {
            if (data.Length != 24)
            {
                throw new ArgumentException($"{nameof(DaytimerEntry)} must have a length of {LoxoneStructLength}");
            }
            var mode = data.Slice(0, 4);
            var from = data.Slice(4, 4);
            var to = data.Slice(8, 4);
            var needActivate = data.Slice(12, 4);
            var value = data.Slice(16, 8);
            
            return new DaytimerEntry(BitConverter.ToInt32(mode), BitConverter.ToInt32(from), BitConverter.ToInt32(to), BitConverter.ToInt32(needActivate), BitConverter.ToDouble(value));
        }

        public int Mode { get; }
        public int From { get; }
        public int To { get; }
        public int NeedActivate { get; }
        public double Value { get; }
    }

    public class Daytimer
    {
        public const int LoxoneStructLength = 28;
        public Daytimer(double defaultValue)
        {
            DefaultValue = defaultValue;
            DaytimerEntries = new List<DaytimerEntry>();
        }

        public IList<DaytimerEntry> DaytimerEntries { get; }

        public double DefaultValue { get; }
    }

    public class EventTableOfDaytimerStates : BinaryMessage
    {
        public Dictionary<LoxoneUuid, Daytimer> Values { get; }
        public EventTableOfDaytimerStates(Header header) : base(header)
        {
            Values = new Dictionary<LoxoneUuid, Daytimer>();
        }

        protected override void Parse(Span<byte> data)
        {
            var pos = 0;
            do
            {
                var uuid = data.Slice(pos + 0, 16);
                var lUuid = new LoxoneUuid(uuid);

                var defaultValue = data.Slice(pos + 16, 8);
                var defaultValueD = BitConverter.ToDouble(defaultValue);

                var daytimer = new Daytimer(defaultValueD);

                var entriesCount = BitConverter.ToInt32(data.Slice(pos + 24, 4));

                for(int i  = 0; i < entriesCount; i++)
                {
                    var entryData = data.Slice(pos + Daytimer.LoxoneStructLength + (i * DaytimerEntry.LoxoneStructLength), DaytimerEntry.LoxoneStructLength);
                    var daytimerEntry = DaytimerEntry.Parse(entryData);
                    daytimer.DaytimerEntries.Add(daytimerEntry);
                }

                pos += (entriesCount * DaytimerEntry.LoxoneStructLength) + Daytimer.LoxoneStructLength;
                Values.Add(lUuid, daytimer);

            } while (pos != data.Length);
        }
    }
}
