using P3.Knx.Core.Driver.Frames;
using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;

namespace P3.Knx.Core.Driver.Tunneling
{
    public class KnxConnectionTunneling : KnxConnection
    {
        private UdpClient _client;
        public KnxConnectionTunneling(IKnxEvents knxEvents, IPAddress host, int port, IPAddress localIp) : base(knxEvents, host, port, localIp)
        {
           
        }

        public KnxConnectionTunneling(IKnxEvents knxEvents, IPAddress host, int port, IPAddress localIp, int localPort) : base(knxEvents, host, port, localIp)
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


            KnxHelper.Logger.LogDebug($"{_client.GetHashCode()}");
            base.Start();

            SendConnectRequest();
        }

        public override void Stop()
        {
            Disconnect();
            base.Stop();
        }

        protected virtual void SendConnectRequest()
        {
            SendFrame(new TunnelingConnectRequestFrame(this));
        }

        protected override void Connect()
        {
            KnxHelper.Logger.LogDebug($"Connect client socket...");
            _client.Connect(new IPEndPoint(Host, Port));
        }

        protected override void Disconnect()
        {
            KnxHelper.Logger.LogDebug($"Close client socket...");
            KnxHelper.Logger.LogDebug($"{_client.GetHashCode()}");
            if (_client != null)
            {
                _client.Client?.Shutdown(SocketShutdown.Both);
                _client.Close();
                _client.Dispose();
            } 
            else
            {

                KnxHelper.Logger.LogDebug($"Client socket is null...");
            }
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
