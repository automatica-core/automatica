using System;
using static P3.Knx.Core.Driver.KnxHelper;

namespace P3.Knx.Core.Driver.Frames
{
    internal abstract class KnxFrame
    {
        protected readonly IKnxConnection KnxConnection;
        public DateTime CreationDateTime { get; }
        protected KnxFrame(IKnxConnection knx, ServiceType serviceType)
        {
            KnxConnection = knx;
            CreationDateTime = DateTime.Now;
            ServiceType = serviceType;
            IsSecureFrame = false;
        }

        internal abstract byte[] ToFrame();

        public ServiceType ServiceType { get; }

        public bool IsSecureFrame { get; protected set; }
    }
}
