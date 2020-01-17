using System;

namespace P3.Driver.HomeKit.Hap.TlvData.Exceptions
{
    public class TlvFragmentLengthException : Exception
    {
        public TlvFragmentLengthException(Constants tlvType) : base($"A fragment with length of 0 is not allowed ({tlvType})")
        {
            
        }
    }
}
