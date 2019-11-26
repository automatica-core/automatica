using System;
using System.Net;
using Microsoft.Extensions.Logging;
using P3.Knx.Core.Driver.Frames;

namespace P3.Knx.Core.Driver
{
    public enum ConnectionType
    {
        Ipv4Tcp,
        Ipv4Udp

    }

    public class KnxDatgramEventArgs : EventArgs
    {
        public KnxDatagram Datagram { get; }

        public KnxDatgramEventArgs(KnxDatagram datagram)
        {
            Datagram = datagram;
        }
    }

    public abstract class KnxConnection : IKnxConnection
    {

        internal KnxSender Sender { get; private set; }
        internal KnxReceiver Receiver { get; private set; }
        private byte _sequenceNumber;

        public EventHandler<EventArgs> OnDisconnected { get; set; }
        public EventHandler<EventArgs> OnConnected { get; set; }
        public EventHandler<KnxDatgramEventArgs> OnDatagramReceived { get; set; }

        private readonly HeartbeatMonitor _heartbeatMonitor;
        private readonly object _lock = new object();

        protected KnxConnection(IPAddress host, int port, IPAddress localIp)
        {
            Host = host;
            Port = port;
            LocalAddress = localIp;

            LocalEndpoint = new IPEndPoint(LocalAddress, 3671);

            _heartbeatMonitor = new HeartbeatMonitor(this);
        }

        protected KnxConnection(IPAddress host, int port, IPAddress localIp, int localPort)
            : this(host, port, localIp)
        {

            LocalEndpoint = new IPEndPoint(LocalAddress, localPort);
        }

        internal abstract KnxSender CreateSender();
        internal abstract KnxReceiver CreateReceiver();

        internal void StartForTest()
        {
            Sender = CreateSender();
            Receiver = CreateReceiver();


        }
        public virtual void Start()
        {
            KnxHelper.Logger.LogInformation($"Try to connect to {Host}:{Port}...");
            Connect();

            Sender = CreateSender();
            Receiver = CreateReceiver();

            Sender.Start();
            Receiver.Start();

            _heartbeatMonitor.Start();
            _heartbeatMonitor.OnHeartbeatFailure += HeartbeatMonitor_OnFailure;

        }

        private void HeartbeatMonitor_OnFailure(object sender, EventArgs e)
        {
            OnDisconnected?.Invoke(this, EventArgs.Empty);
        }

        public virtual void Stop()
        {
            _heartbeatMonitor.OnHeartbeatFailure -= HeartbeatMonitor_OnFailure;
            _heartbeatMonitor.Stop();

            Sender?.Stop();
            Receiver?.Stop();
        }



        protected abstract void Connect();
        protected abstract void Disconnect();

        public string IndividualAddress { get; internal set; }

        public bool Connected { get; protected set; }


        public byte ChannelId { get; private set; }

        internal byte GetSequenceNumber()
        {
            return _sequenceNumber;
        }

        private void SetSequenceNumber(byte value)
        {
            _sequenceNumber = value;
        }

        public byte GenerateSequenceNumber()
        {
            lock (_lock)
            {
                return _sequenceNumber++;
            }
        }

        public IPAddress Host { get; }
        public int Port { get; }
        public IPAddress LocalAddress { get; }
        public IPEndPoint LocalEndpoint { get; private set; }
        public bool UseNat { get; set; }
        public byte ActionMessageCode { get; internal set; }

        internal virtual void HandleFrame(KnxFrame frame)
        {
            _heartbeatMonitor.Reset();

            switch (frame.ServiceType)
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
                    var connectResponse = (ConnectResponseFrame)frame;
                    if (connectResponse.IsValid)
                    {
                        IndividualAddress = connectResponse.IndividualAddress;
                        Connected = true;
                        SetSequenceNumber(0);
                        ChannelId = connectResponse.ChannelId;

                        OnConnected?.Invoke(this, EventArgs.Empty);
                    }
                    else
                    {
                        OnDisconnected?.Invoke(this, EventArgs.Empty);
                        Connected = false;
                    }
                    break;
                case KnxHelper.ServiceType.ConnectionstateRequest:
                    break;
                case KnxHelper.ServiceType.ConnectionstateResponse:
                    break;
                case KnxHelper.ServiceType.DisconnectRequest:
                    SendFrame(new DisconnectResponseFrame(this));
                    Connected = false;
                    OnDisconnected?.Invoke(this, EventArgs.Empty);
                    break;
                case KnxHelper.ServiceType.DisconnectResponse:
                    break;
                case KnxHelper.ServiceType.DeviceConfigurationRequest:
                    break;
                case KnxHelper.ServiceType.DeviceConfigurationAck:
                    break;
                case KnxHelper.ServiceType.TunnellingRequest:
                    var p = (TunnelingRequestFrame)frame;
                    var ack = new TunnelingAckFrame(this, p.FrameSequenceNumber);
                    SendFrame(ack);

                    if (KnxHelper.ProcessCemi(p.Datagram, p.CemiPacket))
                    {
                        OnDatagramReceived?.Invoke(this, new KnxDatgramEventArgs(p.Datagram));
                    }

                    break;
                case KnxHelper.ServiceType.TunnellingAck:
                    break;
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
        }


        public void Read(string destinationAddress)
        {
            lock (_lock)
            {
                RequestStatusFrame fr = RequestStatusFrame.CreateFrame(this, destinationAddress);
                SendFrame(fr);
            }
        }

        public void Write(string address, byte[] data)
        {
            if (!Connected)
            {
                return;
            }
            lock (_lock)
            {
                WriteStatusFrame ws = WriteStatusFrame.CreateFrame(this, address, data);
                SendFrame(ws);


            }
        }

        internal void SendFrame(KnxFrame frame)
        {
            lock (_lock)
            {
                Sender.SendFrame(frame);
            }
        }
    }
}
