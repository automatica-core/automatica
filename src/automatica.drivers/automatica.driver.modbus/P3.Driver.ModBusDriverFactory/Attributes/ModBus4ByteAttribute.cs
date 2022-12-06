using System;
using System.Collections.Generic;
using Automatica.Core.Base.IO;
using Automatica.Core.Driver;
using Automatica.Core.Driver.Exceptions;

namespace P3.Driver.ModBusDriverFactory.Attributes
{
    public abstract class ModBus4ByteAttribute : ModBusAttribute
    {
        public ModBus4ByteOrder ByteOrder { get; }
        protected ModBus4ByteAttribute(IDriverContext driverContext) : base(driverContext)
        {
            var prop = driverContext.NodeInstance.GetProperty("modbus-byte-order");

            if (prop?.ValueInt != null)
            {
                ByteOrder = (ModBus4ByteOrder)prop.ValueInt.Value;
            }
            else
            {
                ByteOrder = ModBus4ByteOrder.AB_CD;
            }
        }
        public sealed override int RegisterLength => 2;

        protected abstract byte[] ConvertToBus(IDispatchable source, object value, out object convertedValue);
        protected abstract object ConvertFromBus(IDispatchable source, byte[] value);

        public sealed override ushort[] ConvertValueToBus(IDispatchable source, object value, out object convertedValue)
        {
            var bytes = ConvertToBus(source, value, out convertedValue);

            switch (ByteOrder)
            {
                case ModBus4ByteOrder.AB_CD:
                case ModBus4ByteOrder.CD_AB:
                    break;
                case ModBus4ByteOrder.BA_DC:
                case ModBus4ByteOrder.DC_BA:
                {
                    var b1 = bytes[0];
                    var b2 = bytes[1];

                    var b3 = bytes[2];
                    var b4 = bytes[3];

                    bytes[0] = b2;
                    bytes[1] = b1;

                    bytes[3] = b3;
                    bytes[2] = b4;

                }
                    break;
            }

            var v1 = BitConverter.ToUInt16(new[] { bytes[0], bytes[1] }, 0);
            var v2 = BitConverter.ToUInt16(new[] { bytes[2], bytes[3] }, 0);

            switch (ByteOrder)
            {
                case ModBus4ByteOrder.AB_CD:
                case ModBus4ByteOrder.BA_DC:
                    return new[] { v1, v2 };
                case ModBus4ByteOrder.DC_BA:
                case ModBus4ByteOrder.CD_AB:
                    return new[] { v2, v1 };
            }
            throw new InvalidInputValueException();
        }
        public sealed override object ConvertValueFromBus(IDispatchable source, ushort[] value)
        {
            var bytes = new List<byte>();

            foreach (var va in value)
            {
                bytes.AddRange(BitConverter.GetBytes(va));
            }

            switch (ByteOrder)
            {
                case ModBus4ByteOrder.AB_CD:
                    break;
                case ModBus4ByteOrder.CD_AB:
                    WordSwap(ref bytes);
                    break;
                case ModBus4ByteOrder.BA_DC:
                    ByteSwap(ref bytes);
                  //  
                    break;
                case ModBus4ByteOrder.DC_BA:
                    ByteSwap(ref bytes);
                    WordSwap(ref bytes);
                    break;
            }

            var correctValue = ConvertFromBus(source, bytes.ToArray());

            return correctValue;
        }

        private void WordSwap(ref List<byte> bytes)
        {
            var b1 = bytes[0];
            var b2 = bytes[1];

            var b3 = bytes[2];
            var b4 = bytes[3];

            bytes[0] = b3;
            bytes[1] = b4;

            bytes[2] = b1;
            bytes[3] = b2;
        }

        private void ByteSwap(ref List<byte> bytes)
        {
            var b1 = bytes[0];
            var b2 = bytes[1];

            var b3 = bytes[2];
            var b4 = bytes[3];

            bytes[0] = b2;
            bytes[1] = b1;

            bytes[3] = b3;
            bytes[2] = b4;
        }

    }
}
