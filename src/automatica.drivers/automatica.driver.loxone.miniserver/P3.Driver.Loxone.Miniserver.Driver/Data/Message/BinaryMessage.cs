using System;

namespace P3.Driver.Loxone.Miniserver.Driver.Data.Message
{
    public abstract class BinaryMessage
    {
        protected BinaryMessage(Header header)
        {
            Header = header;
        }

        public static BinaryMessage GenerateBinaryMessage(HeaderIdentifier identifier, Span<byte> data, Header header)
        {
            BinaryMessage msg = null;

            switch (identifier)
            {
                case HeaderIdentifier.TextMessage:
                    throw new NotSupportedException("This messagetype will be delivered in the WebSocket.OnMessage method");
                case HeaderIdentifier.BinaryFile:
                    break;
                case HeaderIdentifier.EventTableOfValueStates:
                    msg = new EventTableOfValueStates(header);
                    break;
                case HeaderIdentifier.EventTableOfTextStates:
                    msg = new EventTableOfTextStates(header);
                    break;
                case HeaderIdentifier.EventTableOfDaytimerStates:
                    msg = new EventTableOfDaytimerStates(header);
                    break;
                case HeaderIdentifier.OutOfService:
                    break;
                case HeaderIdentifier.KeepAlive:
                    return null;
                case HeaderIdentifier.EventTableOfWeatherStates:
                    msg = new EventTableOfWeatherStates(header);
                    break;

            }

            msg?.Parse(data);
            return msg;
        }
        public static BinaryMessage GenerateBinaryMessage(Header header, Span<byte> data)
        {
            return GenerateBinaryMessage(header.Identifier, data, header);
        }

        protected abstract void Parse(Span<byte> data);

        public Header Header { get; }
    }
}
