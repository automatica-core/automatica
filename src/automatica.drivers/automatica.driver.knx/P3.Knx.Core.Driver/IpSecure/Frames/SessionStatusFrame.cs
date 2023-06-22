namespace P3.Knx.Core.Driver.IpSecure.Frames
{
    public enum SessionStatus
    {
        StatusAuthSuccess = 0,
        StatusAuthFailed = 1,
        StatusUnauthenticated = 2,
        StatusTimeout = 3,
        StatusKeepAlive = 4,
        StatusClose = 5,
        StatusUnauthorized = 6
        
    }
    internal class SessionStatusFrame : SecureFrame
    {
        public SessionStatus SessionStatus { get; set; }
        private SessionStatusFrame(KnxConnectionTunnelingSecure knx, byte[] raw)
             : base(knx, KnxHelper.ServiceType.SessionStatus)
        {
            RawData = raw;
            SessionStatus = (SessionStatus) raw[6];
        }

        private SessionStatusFrame(KnxConnectionTunnelingSecure knx, SessionStatus status)
            : base(knx, KnxHelper.ServiceType.SessionStatus)
        {
            SessionStatus = status;
        }

        public byte[] RawData { get; }

        internal static SessionStatusFrame Create(KnxConnectionTunnelingSecure knx, SessionStatus status)
        {
            return new SessionStatusFrame(knx, status);
        }

        internal static SessionStatusFrame Parse(KnxConnectionTunnelingSecure knx, byte[] data)
        {
            return new SessionStatusFrame(knx, data);
        }

        internal override byte[] ToFrame()
        {
            byte[] datagram = new byte[8];
            datagram[0] = 0x06;
            datagram[1] = 0x10;

            datagram[2] = 0x09;
            datagram[3] = 0x54;

            datagram[4] = 0x00;
            datagram[5] = 0x08;

            datagram[6] = (byte)SessionStatus;
            datagram[7] = 0;
            return datagram;
        }
    }
}
