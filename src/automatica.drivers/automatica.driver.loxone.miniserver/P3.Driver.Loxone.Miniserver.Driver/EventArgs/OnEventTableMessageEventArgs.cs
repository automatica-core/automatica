using P3.Driver.Loxone.Miniserver.Driver.Data.Message;

namespace P3.Driver.Loxone.Miniserver.Driver.EventArgs
{
    public class OnEventTableMessageEventArgs : System.EventArgs
    {
        public OnEventTableMessageEventArgs(BinaryMessage message)
        {
            Message = message;
        }

        public BinaryMessage Message { get; }
    }
}
