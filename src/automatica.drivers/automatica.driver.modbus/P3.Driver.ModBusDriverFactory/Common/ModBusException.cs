using System;
using P3.Driver.ModBusDriver;

namespace P3.Driver.ModBusDriverFactory.Common
{
    public class ModBusException : Exception
    {
        public ModBusExceptionCode ExceptionCode { get; }

        public ModBusException(ModBusExceptionCode exceptionCode)
        {
            ExceptionCode = exceptionCode;
        }
    }
}
