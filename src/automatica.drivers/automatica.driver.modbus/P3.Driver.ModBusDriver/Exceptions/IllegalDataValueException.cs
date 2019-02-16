using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

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
