using System;

namespace P3.Knx.Core.Driver.DPT.Base
{
    public class ToDataPointException : ArgumentException
    {
        public ToDataPointException()
        {
            
        }

        public ToDataPointException(string message) : base(message)
        {

        }
        public ToDataPointException(Exception innerException) : base("", innerException)
        {

        }
    }
}
