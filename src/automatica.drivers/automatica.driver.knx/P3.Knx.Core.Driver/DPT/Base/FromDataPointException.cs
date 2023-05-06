using System;

namespace P3.Knx.Core.Driver.DPT.Base
{
    public class FromDataPointException : ArgumentException
    {
        public FromDataPointException()
        {
            
        }

        public FromDataPointException(string message) :base(message)
        {

        }
        public FromDataPointException(Exception innerException) : base("", innerException)
        {

        }
    }
}
