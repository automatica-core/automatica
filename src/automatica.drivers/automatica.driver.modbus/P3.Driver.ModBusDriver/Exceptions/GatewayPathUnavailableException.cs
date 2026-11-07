using System;

namespace P3.Driver.ModBusDriver.Exceptions
{
    public class GatewayPathUnavailableException : ModBusException
    {
        public GatewayPathUnavailableException() : base(ModBusExceptionCode.GatewayPathUnavailable)
        {
            
        }
    }
}
