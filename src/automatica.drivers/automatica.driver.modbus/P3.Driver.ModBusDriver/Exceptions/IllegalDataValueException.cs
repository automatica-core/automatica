using System;

namespace P3.Driver.ModBusDriver.Exceptions
{
    public class IllegalDataValueException : ModBusException

    {
        public IllegalDataValueException() : base(ModBusExceptionCode.IllegalDataValue)
        {
            
        }

    }
}
