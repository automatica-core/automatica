using System.Net;
using System.Net.Sockets;
using P3.Knx.Core.Driver.Frames;
using P3.Knx.Core.Driver.IpSecure.Frames;

namespace P3.Knx.Core.Driver.IpSecure
{
    internal class KnxSenderTunnelingSecure : KnxSender
    {
        private readonly TcpClient client;

        internal KnxSenderTunnelingSecure(KnxConnection connection, TcpClient client)
            : base(connection)
        {
            this.client = client;
        }

        internal override bool SendFrame(KnxFrame frame)
        {
            byte[] datagram = null;
            if (!frame.IsSecureFrame)
            {
                var secFrame = SecureWrapperFrame.Create((KnxConnectionTunnelingSecure)Connection, frame);
                datagram = secFrame.ToFrame();
            }
            else
            {
                datagram = frame.ToFrame();
            }

            int? sent = client?.Client.Send(datagram);

            return sent == datagram.Length;

        }

        internal override void Start()
        {

        }

        internal override void Stop()
        {
        }

        internal void SetRemoteEndpoint(IPEndPoint iPEndPoint)
        {
            //needed ?
        }
    }

}
