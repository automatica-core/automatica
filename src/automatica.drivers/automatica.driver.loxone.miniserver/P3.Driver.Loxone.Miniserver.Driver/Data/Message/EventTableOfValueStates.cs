using System;
using System.Collections.Generic;

namespace P3.Driver.Loxone.Miniserver.Driver.Data.Message
{
    public class EventTableOfValueStates : BinaryMessage
    {
        public Dictionary<string, double> Values { get; }
        public EventTableOfValueStates(Header header) : base(header)
        {
            Values = new Dictionary<string, double>();
        }

        protected override void Parse(Span<byte> data)
        {
            var length = data.Length / 24;

            for (int i = 0; i < length; i++)
            {
                var curData = data.Slice(i * 24, 24);

                var uuid = curData.Slice(0, 16);
                var lUuid = new LoxoneUuid(uuid);
                var value = curData.Slice(16, 8);

                var dValue = BitConverter.ToDouble(value);

                Values.Add(lUuid.Uuid.ToLower(), dValue);
            }
        }
    }
}
