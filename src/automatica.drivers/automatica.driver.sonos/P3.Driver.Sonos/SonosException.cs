using System;

namespace P3.Driver.Sonos
{
    public class SonosException : Exception
    {
        public SonosException()
        {
        }

        public SonosException(string message) : base(message)
        {
        }

        public SonosException(string message, Exception inner) : base(message, inner)
        {
        }

    }
}
