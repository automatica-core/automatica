using System;
using System.Runtime.Serialization;

namespace P3.Driver.ModBusDriver.Exceptions
{
    [Serializable]
    public class ServerDeviceFailureException : ModBusException
    {
        public ServerDeviceFailureException() : base(ModBusExceptionCode.ServerDeviceFailure)
        {
            
        }

        protected ServerDeviceFailureException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {

        }
    }
}
