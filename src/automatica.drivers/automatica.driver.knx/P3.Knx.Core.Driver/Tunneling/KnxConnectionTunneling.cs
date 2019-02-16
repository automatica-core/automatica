using P3.Knx.Core.Driver.Frames;
using System.Net;
using System.Net.Sockets;

namespace P3.Knx.Core.Driver.Tunneling
{
    public class KnxConnectionTunneling : KnxConnection
    {
        private UdpClient _client;
        public KnxConnectionTunneling(IPAddress host, int port, IPAddress localIp) : base(host, port, localIp)
        {
           
        }

        public KnxConnectionTunneling(IPAddress host, int port, IPAddress localIp, int localPort) : base(host, port, localIp)
        {

        }

        public override void Start()
        {
            if (UseNat)
            {
                _client = new UdpClient();
            }
            else
            {
                _client = new UdpClient(new IPEndPoint(LocalAddress, Port));
            }
            _client.Client.ReceiveTimeout = 500;

            base.Start();

            SendConnectRequest();
        }

        protected virtual void SendConnectRequest()
        {
            SendFrame(new TunnelingConnectRequestFrame(this));
        }

        protected override void Connect()
        {
            _client.Connect(new IPEndPoint(Host, Port));
        }

        protected override void Disconnect()
        {
            
        }

        internal override KnxReceiver CreateReceiver()
        {
            return new KnxTunnelingReceiver(this, _client);
        }

        internal override KnxSender CreateSender()
        {
            return new KnxTunnelingSender(this, _client);
        }
    }
}
