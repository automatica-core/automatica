using System;
using System.Collections.Generic;
using Automatica.Core.Driver;
using Automatica.Core.Driver.Exceptions;

namespace P3.Driver.ModBusDriverFactory.Attributes
{
    public abstract class ModBus8ByteAttribute : ModBusAttribute
    {
        public ModBus8ByteOrder ByteOrder { get; }
        protected ModBus8ByteAttribute(IDriverContext driverContext) : base(driverContext)
        {
            var prop = driverContext.NodeInstance.GetProperty("modbus-byte-order");

            if (prop?.ValueInt != null)
            {
                ByteOrder = (ModBus8ByteOrder) prop.ValueInt.Value;
            }
            else
            {
                ByteOrder = ModBus8ByteOrder.AB_CD_EF_GH;
            }
        }

        public sealed override int RegisterLength => 4;

        protected abstract byte[] ConvertToBus(object value, out object convertedValue);
        protected abstract object ConvertFromBus(byte[] value);

        public sealed override ushort[] ConvertValueToBus(object value, out object convertedValue)
        {
            var bytes = ConvertToBus(value, out convertedValue);

            switch (ByteOrder)
            {
                case ModBus8ByteOrder.AB_CD_EF_GH:
                case ModBus8ByteOrder.GH_EF_CD_AB:
                    break;
                case ModBus8ByteOrder.BA_DC_FE_HG:
                case ModBus8ByteOrder.HF_FE_DC_BA:
                    {
                        var b1 = bytes[0];
                        var b2 = bytes[1];

                        var b3 = bytes[2];
                        var b4 = bytes[3];

                        var b5 = bytes[4];
                        var b6 = bytes[5];

                        var b7 = bytes[6];
                        var b8 = bytes[7];

                        bytes[0] = b2;
                        bytes[1] = b1;

                        bytes[3] = b3;
                        bytes[2] = b4;

                        bytes[5] = b5;
                        bytes[4] = b6;

                        bytes[7] = b7;
                        bytes[6] = b8;
                    }
                    break;
            }

            var v1 = BitConverter.ToUInt16(new[] { bytes[0], bytes[1] }, 0);
            var v2 = BitConverter.ToUInt16(new[] { bytes[2], bytes[3] }, 0);
            var v3 = BitConverter.ToUInt16(new[] { bytes[4], bytes[5] }, 0);
            var v4 = BitConverter.ToUInt16(new[] { bytes[6], bytes[7] }, 0);

            switch (ByteOrder)
            {
                case ModBus8ByteOrder.AB_CD_EF_GH:
                case ModBus8ByteOrder.BA_DC_FE_HG:
                    return new[] { v1, v2, v3, v4 };
                case ModBus8ByteOrder.GH_EF_CD_AB:
                case ModBus8ByteOrder.HF_FE_DC_BA:
                    return new[] { v4, v3, v2, v1 };
            }
            throw new InvalidInputValueException();
        }
        public sealed override object ConvertValueFromBus(ushort[] value)
        {
            var bytes = new List<byte>();

            foreach (var va in value)
            {
                bytes.AddRange(BitConverter.GetBytes(va));
            }

            switch (ByteOrder)
            {
                case ModBus8ByteOrder.AB_CD_EF_GH:
                    WordSwap(ref bytes);
                    break;
                case ModBus8ByteOrder.GH_EF_CD_AB:
                    break;
                case ModBus8ByteOrder.BA_DC_FE_HG:
                    bytes.Reverse();
                    break;
                case ModBus8ByteOrder.HF_FE_DC_BA:
                    ByteSwap(ref bytes);
                    break;
            }

            return ConvertFromBus(bytes.ToArray());
        }
        private void ByteSwap(ref List<byte> bytes)
        {
            var b1 = bytes[0];
            var b2 = bytes[1];

            var b3 = bytes[2];
            var b4 = bytes[3];

            var b5 = bytes[4];
            var b6 = bytes[5];

            var b7 = bytes[6];
            var b8 = bytes[7];

            bytes[0] = b2;
            bytes[1] = b1;

            bytes[3] = b3;
            bytes[2] = b4;

            bytes[5] = b5;
            bytes[4] = b6;

            bytes[7] = b7;
            bytes[6] = b8;
        }

        private void WordSwap(ref List<byte> bytes)
        {
            ByteSwap(ref bytes);
            bytes.Reverse();
        }
    }
}
