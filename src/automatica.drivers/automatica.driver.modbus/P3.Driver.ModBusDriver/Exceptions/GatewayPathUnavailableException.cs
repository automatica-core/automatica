using System;
using System.Runtime.Serialization;

namespace P3.Driver.ModBusDriver.Exceptions
{
    [Serializable]
    public class GatewayPathUnavailableException : ModBusException
    {
        public GatewayPathUnavailableException() : base(ModBusExceptionCode.GatewayPathUnavailable)
        {
            
        }
        protected GatewayPathUnavailableException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {

        }
    }
}
