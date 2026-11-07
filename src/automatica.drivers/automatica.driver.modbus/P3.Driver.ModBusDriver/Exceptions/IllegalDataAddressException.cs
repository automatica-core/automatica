using System;

namespace P3.Driver.ModBusDriver.Exceptions
{
    public class IllegalDataAddressException : ModBusException
    {
        public IllegalDataAddressException() : base(ModBusExceptionCode.IllegalDataAddress)
        {
            
        }
    }
}
