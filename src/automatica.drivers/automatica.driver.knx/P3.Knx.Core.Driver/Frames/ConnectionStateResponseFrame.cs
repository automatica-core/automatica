using System;

namespace P3.Knx.Core.Driver.Frames
{
    internal class ConnectionStateResponseFrame : KnxFrame
    {

        internal bool DisconnectSession { get; private set; }
        private ConnectionStateResponseFrame(KnxConnection knx)
            : base(knx, KnxHelper.ServiceType.ConnectionstateRequest)
        {
            
        }

        internal static ConnectionStateResponseFrame CreateFrame(byte[] datagram, KnxConnection knx)
        {
            ConnectionStateResponseFrame frame = new ConnectionStateResponseFrame(knx) {DisconnectSession = false};
            var response = datagram[7];

            if (response != 0x21)
                return frame;

            frame.DisconnectSession = true;
            return frame;
        }

        internal override byte[] ToFrame()
        {
            throw new NotImplementedException();
        }
    }
}
