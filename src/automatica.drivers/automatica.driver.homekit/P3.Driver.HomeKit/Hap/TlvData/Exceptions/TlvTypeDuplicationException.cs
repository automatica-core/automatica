using System;

namespace P3.Driver.HomeKit.Hap.TlvData.Exceptions
{
    public class TlvTypeDuplicationException : Exception
    {
        public TlvTypeDuplicationException(Constants tlvType) : base($"Duplicate TLV fragments in one message are not allowed ({tlvType})")
        {
            
        }
    }
}
