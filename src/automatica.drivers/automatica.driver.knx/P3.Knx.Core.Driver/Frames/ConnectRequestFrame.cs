using System;
using System.Collections.Generic;
using System.Text;

namespace P3.Knx.Core.Driver.Frames
{
    internal class ConnectRequestFrame : KnxFrame
    {
        protected ConnectRequestFrame(KnxConnection knx, KnxHelper.ServiceType serviceType) : base(knx, serviceType)
        {
        }

        internal override byte[] ToFrame()
        {
            throw new NotImplementedException();
        }
    }
}
