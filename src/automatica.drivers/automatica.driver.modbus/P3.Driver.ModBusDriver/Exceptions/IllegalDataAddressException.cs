using System;
using System.Runtime.Serialization;

namespace P3.Driver.ModBusDriver.Exceptions
{
    [Serializable]
    public class IllegalDataAddressException : ModBusException
    {
        public IllegalDataAddressException() : base(ModBusExceptionCode.IllegalDataAddress)
        {
            
        }
        protected IllegalDataAddressException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {

        }
    }
}
