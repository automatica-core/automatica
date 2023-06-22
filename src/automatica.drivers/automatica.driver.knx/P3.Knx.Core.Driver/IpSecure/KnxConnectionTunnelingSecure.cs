using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using P3.Elliptic;
using P3.Knx.Core.Driver.Frames;
using P3.Knx.Core.Driver.IpSecure.Error;
using P3.Knx.Core.Driver.IpSecure.Frames;
using P3.Knx.Core.Driver.Tunneling;

namespace P3.Knx.Core.Driver.IpSecure
{
    public class KnxConnectionTunnelingSecure : KnxConnection
    {
        public EventHandler<IpSecureEventArgs> IpSecureErrorOccured { get; set; }

        private const string CSalt = "user-password.1.secure.ip.knx.org";
        private const string CAuthSalt = "device-authentication-code.1.secure.ip.knx.org";
        private UInt64 _secureSequenceNumber;

        internal static readonly byte[] SerialNumber = {0x00, 0x01, 0x2B, 0x00, 0x03, 0x00};

        internal byte[] ClientPublic { get; private set; }
        internal byte[] ClientPrivate { get; private set; }

        internal byte[] ServerPublic { get; private set; }

        internal byte[] HashedPassword { get; private set; }
        internal byte[] HashedAuthCode { get; private set; }

        internal byte[] SessionKey { get; private set; }
        internal byte[] SessionId { get; private set; }

        internal SessionRequestFrame SessionRequestFrame { get; set; }

        private TcpClient _client;

        public KnxConnectionTunnelingSecure(IKnxEvents knxEvents, IPAddress remoteIpAddress, int remotePort, IPAddress localIpEndpoint, string mgmPassword, string authCode, string individualAddress)
            : this(knxEvents, remoteIpAddress, remotePort, localIpEndpoint, mgmPassword, authCode)
        {
            IndividualAddress = individualAddress;

            if (!KnxHelper.IsAddressIndividual(IndividualAddress))
            {
                throw new InvalidDataException("Individual address must be in type 0.0.0");
            }

            if (KnxHelper.GetAddress(individualAddress).Length != 2)
            {
                throw new InvalidDataException($"Individual address {individualAddress} is invalid");
            }
        }

        public KnxConnectionTunnelingSecure(IKnxEvents knxEvents, IPAddress remoteIpAddress, int remotePort, IPAddress localIpAddress, string mgmPassword, string authCode)
            : base(knxEvents, remoteIpAddress, remotePort, localIpAddress)
        {
            HashedPassword = CryptoHelper.Pbkdf2Sha256GetBytes(16, Encoding.UTF8.GetBytes(mgmPassword),
                Encoding.UTF8.GetBytes(CSalt), 65536);
            HashedAuthCode = CryptoHelper.Pbkdf2Sha256GetBytes(16, Encoding.UTF8.GetBytes(authCode),
                Encoding.UTF8.GetBytes(CAuthSalt), 65536);

            SessionId = new byte[] {0x00, 0x01};

            ResetSecquenceNumber();

            IndividualAddress = null;
        }

        public KnxConnectionTunnelingSecure(IKnxEvents knxEvents, IPAddress remoteIpAddress, int remotePort,
            IPAddress localIpAddress, int localPort, string mgmPassword, string authCode)
            : base(knxEvents, remoteIpAddress, remotePort, localIpAddress, localPort)
        {
            HashedPassword = CryptoHelper.Pbkdf2Sha256GetBytes(16, Encoding.UTF8.GetBytes(mgmPassword),
                Encoding.UTF8.GetBytes(CSalt), 65536);
            HashedAuthCode = CryptoHelper.Pbkdf2Sha256GetBytes(16, Encoding.UTF8.GetBytes(authCode),
                Encoding.UTF8.GetBytes(CAuthSalt), 65536);

            SessionId = new byte[] {0x00, 0x01};

            ResetSecquenceNumber();
            IndividualAddress = null;

            
        }

        internal void ResetSecquenceNumber()
        {
            _secureSequenceNumber = 0;
        }

        internal void IncSequenceNumber()
        {
            _secureSequenceNumber++;
        }

        internal UInt64 SequenceNumber => _secureSequenceNumber;

        internal void SetPublicAndPrivateKey(byte[] pub, byte[] priv)
        {
            ClientPublic = pub;
            ClientPrivate = priv;
        }

        protected override void Disconnect()
        {
            SessionStatusFrame frame = SessionStatusFrame.Create(this, SessionStatus.StatusClose);
            SendFrame(frame);
        }

        internal void SetHashedPassword(byte[] pw)
        {
            HashedPassword = pw;
        }

        internal void SetHashedDeviceAuthCode(byte[] authCode)
        {
            HashedAuthCode = authCode;
        }

        internal void SetSessionKey(byte[] sessionKey)
        {
            SessionKey = sessionKey;
        }

        internal void SetSessionId(byte[] sessionId)
        {
            SessionId = sessionId;
        }


        internal override void HandleFrame(KnxFrame frame)
        {
            switch (frame.ServiceType)
            {
                case KnxHelper.ServiceType.SessionRequest:
                    SessionRequestFrame = (SessionRequestFrame)frame;

                    SessionResponseFrame response = SessionResponseFrame.Create(this, (SessionRequestFrame)frame);
                    SendFrame(response);

                    return;
                case KnxHelper.ServiceType.SessionResponse:
                    var sessionResponseFrame = (SessionResponseFrame)frame;

                    if (sessionResponseFrame.IsValid())
                    {
                        SessionAuthenticationRequest authReq = SessionAuthenticationRequest.Create(this);
                        SecureWrapperFrame wrapper = SecureWrapperFrame.Create(this, authReq);
                        SendFrame(wrapper);
                    }
                    else
                    {
                        IpSecureErrorOccured?.Invoke(this, new IpSecureEventArgs("E_INVALID_DEV_AUTH_CODE", KnxError.IpSecInvalidDeviceAuthCode));
                    }

                    return;
                case KnxHelper.ServiceType.SecureWrapper:


                    return;

                case KnxHelper.ServiceType.SessionStatus:
                    SessionStatusFrame ssf = (SessionStatusFrame)frame;
                    if (ssf.SessionStatus == SessionStatus.StatusAuthSuccess)
                    {
                        if (string.IsNullOrEmpty(IndividualAddress))
                        {
                            TunnelingConnectRequestFrame crf = new TunnelingConnectRequestFrame(this);
                            SendFrame(crf);
                        }
                        else
                        {
                            TunnelingConnectRequestFrameExt crf = new TunnelingConnectRequestFrameExt(this, KnxHelper.GetAddress(IndividualAddress));
                            SendFrame(crf);
                        }

                    }
                    else if (ssf.SessionStatus == SessionStatus.StatusTimeout)
                    {
                        IpSecureErrorOccured?.Invoke(this, new IpSecureEventArgs("E_SESSION_TIMEOUT", KnxError.IpSecSessionTimeoutReceived));
                    }
                    else
                    {
                        IpSecureErrorOccured?.Invoke(this, new IpSecureEventArgs("E_INVALID_USER_AUTH_CODE", KnxError.IpSecInvalidUserAuthCode));
                    }

                    return;
            }
            base.HandleFrame(frame);
        }


        internal override KnxReceiver CreateReceiver()
        {
            return new KnxReceiverTunnelingSecure(this, _client);
        }

        internal override KnxSender CreateSender()
        {
            return new KnxSenderTunnelingSecure(this, _client);
        }

        internal void SetServerPublicKey(byte[] serverPublic)
        {
            ServerPublic = serverPublic;

            byte[] sharedSecret = Curve25519.GetSharedSecret(ClientPrivate, ServerPublic);
            SessionKey = CryptoHelper.GetSessionKey(sharedSecret);
        }

        public override void Start()
        {
            if (UseNat)
            {
                _client = new TcpClient();
            }
            else
            {
                _client = new TcpClient(new IPEndPoint(LocalAddress, Port));
            }
            _client.Client.ReceiveTimeout = 500;

            base.Start();

            SessionRequestFrame = SessionRequestFrame.Create(this);
            SendFrame(SessionRequestFrame);
        }

        protected override void Connect()
        {
            _client.Connect(new IPEndPoint(Host, Port));
        }
    }
}