using System;
using System.Runtime.Serialization;

namespace P3.Driver.ModBusDriver.Exceptions
{
    [Serializable]
    public abstract class ModBusException : Exception
    {
        private readonly ModBusExceptionCode _code;

        protected ModBusException(ModBusExceptionCode code)
        {
            _code = code;
        }
        protected ModBusException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }


        public byte[] Serialize(ModBusFunction functionCode, byte slaveId)
        {
            var data = new byte[3];
            data[0] = slaveId;
            data[1] = (byte) (functionCode + 0x80);
            data[2] = (byte) _code;
            return data;
        }
    }
}
