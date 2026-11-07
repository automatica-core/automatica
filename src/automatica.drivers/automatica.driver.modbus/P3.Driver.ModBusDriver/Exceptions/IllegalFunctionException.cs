using System;

namespace P3.Driver.ModBusDriver.Exceptions
{
    public class IllegalFunctionException : ModBusException
    {
        public IllegalFunctionException() : base(ModBusExceptionCode.IllegalFunction)
        {
            
        }

    }
}
