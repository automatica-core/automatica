using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Driver.Utility;
using P3.Knx.Core.Driver.Frames;
using P3.Knx.Core.Driver.IpSecure.Frames;

namespace P3.Knx.Core.Driver.IpSecure
{
    internal class KnxReceiverTunnelingSecure : KnxReceiver
    {

        internal KnxReceiverTunnelingSecure(KnxConnection connection, TcpClient client)
            : base(connection)
        {
            Client = client;
        }


        protected void SendKeepAlive()
        {
            var frame = SecureWrapperFrame.Create((KnxConnectionTunnelingSecure)Connection, SessionStatusFrame.Create(KnxConnectionTunneling, SessionStatus.StatusKeepAlive));
            KnxConnectionTunneling.SendFrame(frame);
        }

        private KnxConnectionTunnelingSecure KnxConnectionTunneling => (KnxConnectionTunnelingSecure) Connection;

        public TcpClient Client { get; }

        internal override KnxFrame ParseFrame(byte[] data)
        {
            var type = KnxHelper.GetServiceType(data);

            switch (type)
            {
                case KnxHelper.ServiceType.SecureWrapper:
                    bool isValid;
                    KnxFrame frame = SecureWrapperFrame.Parse(KnxConnectionTunneling, data, out isValid);
                    if (!isValid)
                    {
                        return null;
                    }
                    return frame;
                case KnxHelper.ServiceType.SessionRequest:
                    return SessionRequestFrame.Parse(KnxConnectionTunneling, data);
                case KnxHelper.ServiceType.SessionResponse:
                    return SessionResponseFrame.Parse(KnxConnectionTunneling, data);
                case KnxHelper.ServiceType.SessionAuthenticate:
                    break;
                case KnxHelper.ServiceType.SessionStatus:
                    return SessionStatusFrame.Parse(KnxConnectionTunneling, data);
            }
            return base.ParseFrame(data);
        }


        protected override async Task<byte[]> Receive()
        {
            byte[] header = new byte[6];
            int by = await Client.GetStream().ReadAsync(header, 0, 6);

            if (by == 6)
            {
                int length = (header[4] >> 8) + header[5];
                int maxTime = 10 * 1000;
                int currentWaitTime = 0;
                bool datagramComplete = true;
                do
                {
                    if (currentWaitTime >= maxTime)
                    {
                        datagramComplete = false;
                        break;
                    }
                    Thread.Sleep(10);
                    currentWaitTime += 10;
                } while (Client.Available + 6 < length);

                if (datagramComplete)
                {
                    var datagram = new byte[length];
                    Client.GetStream().Read(datagram, 6, length - 6);
                    Array.Copy(header, 0, datagram, 0, 6);

                    KnxHelper.Logger.LogHexIn(datagram);

                    return datagram;
                }
            }
            return new byte[] { };
        }

        protected override void StartReceive()
        {
            
        }

        protected override void StopReceive()
        {
            
        }
    }
}