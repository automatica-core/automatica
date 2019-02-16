using Automatica.Core.Driver.Utility;
using Microsoft.Extensions.Logging;
using P3.Knx.Core.Driver.Frames;
using System.Threading.Tasks;

namespace P3.Knx.Core.Driver
{
    internal abstract class KnxReceiver
    {
        private Task _receiverThread;
        private bool _isRunning;
        protected KnxReceiver(KnxConnection connection)
        {
            Connection = connection;
        }


        private async Task Run()
        {
            StartReceive();
            while (_isRunning)
            {

                var frame = await ReceiveFrame();
                if (!_isRunning)
                {
                    break;
                }
                if (frame== null)
                {
                    //todo log error
                    return;
                }

                Connection.HandleFrame(frame);
                
            }
            StopReceive();
        }

        internal async Task<KnxFrame> ReceiveFrame()
        {
            var data = await Receive();
            lock (Connection)
            {
                if(data == null || data.Length == 0)
                {
                    return null;
                }

                var frame = ParseFrame(data);

                KnxHelper.Logger.LogTrace($"Writing {frame}");
                KnxHelper.Logger.LogHexIn(data);
                return frame;
            }
        }

        internal virtual KnxFrame ParseFrame(byte[] data)
        {
            var type = KnxHelper.GetServiceType(data);

            switch (type)
            {
                case KnxHelper.ServiceType.SearchRequest:
                    break;
                case KnxHelper.ServiceType.SearchResponse:
                    break;
                case KnxHelper.ServiceType.DescriptionRequest:
                    break;
                case KnxHelper.ServiceType.DescriptionResponse:
                    break;
                case KnxHelper.ServiceType.ConnectRequest:
                    break;
                case KnxHelper.ServiceType.ConnectResponse:
                    return ConnectResponseFrame.CreateFrame(data, Connection);
                case KnxHelper.ServiceType.ConnectionstateRequest:
                    break;
                case KnxHelper.ServiceType.ConnectionstateResponse:
                    return ConnectionStateResponseFrame.CreateFrame(data, Connection);
                case KnxHelper.ServiceType.DisconnectRequest:
                    return DisconnectRequestFrame.Parse(Connection, data);
                case KnxHelper.ServiceType.DisconnectResponse:
                    return new DisconnectResponseFrame(Connection);
                case KnxHelper.ServiceType.DeviceConfigurationRequest:
                    break;
                case KnxHelper.ServiceType.DeviceConfigurationAck:
                    break;
                case KnxHelper.ServiceType.TunnellingRequest:
                    return TunnelingRequestFrame.CreateFrame(data, Connection, 0, false);
                case KnxHelper.ServiceType.TunnellingAck:
                    return TunnelingAckFrame.Parse(Connection, data);
                case KnxHelper.ServiceType.RoutingIndication:
                    break;
                case KnxHelper.ServiceType.RoutingLostMessage:
                    break;
                case KnxHelper.ServiceType.SecureWrapper:
                    break;
                case KnxHelper.ServiceType.SessionRequest:
                    break;
                case KnxHelper.ServiceType.SessionResponse:
                    break;
                case KnxHelper.ServiceType.SessionAuthenticate:
                    break;
                case KnxHelper.ServiceType.SessionStatus:
                    break;
                case KnxHelper.ServiceType.TimerNotify:
                    break;
                case KnxHelper.ServiceType.Unknown:
                    break;
            }

            return null;
        }

        public KnxConnection Connection { get; }

        protected abstract Task<byte[]> Receive();

        internal void Start()
        {
            _isRunning = true;
            _receiverThread = Task.Factory.StartNew(Run);

        }
        internal void Stop()
        {
            _isRunning = false;
        }

        protected abstract void StartReceive();
        protected abstract void StopReceive();

    }
}
