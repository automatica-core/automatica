using System;
using System.Runtime.Serialization;

namespace P3.Driver.Sonos
{
    [Serializable]
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

        protected SonosException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}
