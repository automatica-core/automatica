using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace P3.Knx.Core.Driver.Tunneling
{
    internal class KnxTunnelingReceiver : KnxReceiver
    {
        private readonly IPEndPoint _endpoint;
        private readonly UdpClient _client;
        public KnxTunnelingReceiver(KnxConnection connection, UdpClient client) : base(connection)
        {
            _endpoint = new IPEndPoint(Connection.Host, Connection.Port);

            _client = client;
        }

        protected override async Task<byte[]> Receive()
        {
            try
            {
                var result = await _client.ReceiveAsync();
                if (result.RemoteEndPoint.Address.ToString() == _endpoint.Address.ToString())
                {
                    return result.Buffer;
                }
                return new byte[] { };
            }
            catch(SocketException e)
            {
                if (e.ErrorCode == 10060)
                {
                    return new byte[] { };
                }
                throw;
            }
            catch(ObjectDisposedException)
            {
                return new byte[] { };
            }
        }

        protected override void StartReceive()
        {
        }

        protected override void StopReceive()
        {
            _client.Dispose();
        }
        
    }
}
