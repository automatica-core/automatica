using System;

namespace P3.Driver.ModBusDriver.Exceptions
{
    public class ServerDeviceFailureException : ModBusException
    {
        public ServerDeviceFailureException() : base(ModBusExceptionCode.ServerDeviceFailure)
        {
            
        }

    }
}
