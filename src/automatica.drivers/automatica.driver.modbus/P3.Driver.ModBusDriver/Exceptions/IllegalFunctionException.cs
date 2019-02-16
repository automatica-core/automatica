using System;
using System.Runtime.Serialization;

namespace P3.Driver.ModBusDriver.Exceptions
{
    [Serializable]
    public class IllegalFunctionException : ModBusException
    {
        public IllegalFunctionException() : base(ModBusExceptionCode.IllegalFunction)
        {
            
        }

        protected IllegalFunctionException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {

        }
    }
}
