using System;
using System.Collections.Generic;
using System.Text;

namespace P3.Driver.Loxone.Miniserver.Driver.Data.Message
{
    public class TextEventData
    {
        public TextEventData(LoxoneUuid iconId, string text)
        {
            IconId = iconId;
            Text = text;
        }

        public LoxoneUuid IconId { get; }
        public string Text { get; }
    }
    public class EventTableOfTextStates : BinaryMessage
    {
        public Dictionary<LoxoneUuid, TextEventData> Values { get; }
        public EventTableOfTextStates(Header header) : base(header)
        {
            Values = new Dictionary<LoxoneUuid, TextEventData>();
        }

        protected override void Parse(Span<byte> data)
        {
            var pos = 0;

            do
            {
                var uuid = data.Slice(pos + 0, 16);
                var lUuid = new LoxoneUuid(uuid);


                var uuidIcon = data.Slice(pos + 16, 16);
                var lUuidIcon = new LoxoneUuid(uuid);

                var textLength = BitConverter.ToUInt32(data.Slice(pos + 32, 4));

                var textBinary = data.Slice(pos + 36, (int)textLength);
                var text = Encoding.UTF8.GetString(textBinary);

                var paddingBytes = (int)(textLength % 4);

                Values.Add(lUuid, new TextEventData(lUuidIcon, text));
                pos += (16 + 16 + 4 + (int)textLength);

                if(paddingBytes > 0)
                {
                    pos += 4 - paddingBytes;
                }


            } while (pos != data.Length);
        }
    }
}
