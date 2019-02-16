﻿using P3.Knx.Core.Driver.Tunneling;
using System;
using System.Collections.Generic;
using System.Text;
using static P3.Knx.Core.Driver.KnxHelper;

namespace P3.Knx.Core.Driver.Frames
{
    internal abstract class KnxFrame
    {
        protected readonly KnxConnection KnxConnection;
        public DateTime CreationDateTime { get; }
        protected KnxFrame(KnxConnection knx, ServiceType serviceType)
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
