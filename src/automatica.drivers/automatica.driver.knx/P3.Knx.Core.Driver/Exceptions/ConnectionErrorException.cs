using System;

namespace P3.Knx.Core.Driver.Exceptions
{
    public class ConnectionErrorException : Exception
    {
        internal ConnectionErrorException(KnxConnection configuration)
            : base(string.Format("ConnectionErrorException: Error connecting to {0}:{1}", configuration.Host, configuration.Port))
        {
        }

        internal ConnectionErrorException(KnxConnection configuration, Exception innerException)
            : base(string.Format("ConnectionErrorException: Error connecting to {0}:{1}", configuration.Host, configuration.Port), innerException)
        {
        }

        public override string ToString()
        {
            return Message;
        }
    }
}