using System;
using System.Runtime.Serialization;

namespace P3.Driver.ModBusDriver.Exceptions
{
    [Serializable]
    public class IllegalDataValueException : ModBusException

    {
        public IllegalDataValueException() : base(ModBusExceptionCode.IllegalDataValue)
        {
            
        }

        protected IllegalDataValueException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {

        }
    }
}
