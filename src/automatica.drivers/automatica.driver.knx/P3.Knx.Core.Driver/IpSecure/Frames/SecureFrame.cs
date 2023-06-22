using P3.Knx.Core.Driver.Frames;

namespace P3.Knx.Core.Driver.IpSecure.Frames
{
    internal abstract class SecureFrame : KnxFrame
    {
        protected readonly KnxConnectionTunnelingSecure KnxConnectionSecure;
        protected SecureFrame(KnxConnectionTunnelingSecure knx, KnxHelper.ServiceType serviceType)
            : base(knx, serviceType)
        {
            KnxConnectionSecure = knx;
            IsSecureFrame = true;
        }
    }
}
