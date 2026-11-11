using System.Buffers.Binary;
using Automatica.Core.Base.TelegramMonitor;
using Automatica.Core.Driver.Utility;
using Microsoft.Extensions.Logging;
using P3.Driver.ModBus.SolarmanV5.Config;
using P3.Driver.ModBusDriver;
using P3.Driver.ModBusDriver.Master.Tcp;

namespace P3.Driver.ModBus.SolarmanV5
{
    public class SolarmanConnection : ModBusMasterTcpDriver
    {
        /// <summary>
        /// V5 envelope bytes outside the length field: start (1), length (2), control code (2), sequence (2),
        /// logger serial (4), checksum (1) and end (1)
        /// </summary>
        public const int V5EnvelopeLength = 13;

        private const byte V5Start = 0xA5;
        private const byte V5End = 0x15;

        private readonly uint _serial;

        public SolarmanConnection(SolarmanConfig config, ITelegramMonitorInstance monitor, ILogger logger = null) : base(config, monitor, logger)
        {
            _serial = config.SolarmanSerialNumber;
        }

        /// <summary>
        /// The master builds write requests as function code + data. Solarman V5 tunnels a complete Modbus RTU frame,
        /// so the slave id and the CRC are added here and the RTU frame is wrapped in the V5 envelope.
        /// </summary>
        protected override byte[] BuildHeaderFromDataFrame(byte[] dataFrame, byte slaveId, ModBusFunction function)
        {
            var rtuFrame = new byte[dataFrame.Length + 1];
            rtuFrame[0] = slaveId;
            Array.Copy(dataFrame, 0, rtuFrame, 1, dataFrame.Length);

            return BuildFrame(_serial, rtuFrame);
        }

        /// <summary>
        /// Solarman V5 frames have no MBAP transaction id
        /// </summary>
        protected override bool IsResponseToLastRequest(byte[] header)
        {
            return true;
        }

        protected override ushort GetLengthToRead(byte[] buffer)
        {
            return (ushort)(Utils.GetUShort(buffer[2], buffer[1]) );
        }

        protected override byte[] ParseFrame(byte slaveId, ModBusFunction function, int numberOfRegisters, byte[] input)
        {
            return ExtractRtuFrame(function, numberOfRegisters, input);
        }

        /// <summary>
        /// Returns the Modbus RTU frame (slave id up to and including the CRC) from the end of a V5 response,
        /// <paramref name="input"/> ends with the RTU CRC, the V5 checksum and the V5 end byte.
        /// </summary>
        public static byte[] ExtractRtuFrame(ModBusFunction function, int numberOfRegisters, byte[] input)
        {
            var readLength = 5;

            if (function == ModBusFunction.ReadInputRegisters ||
                function == ModBusFunction.ReadHoldingRegisters)
            {
                readLength += numberOfRegisters * 2;
            }
            else if (function == ModBusFunction.ReadDiscreteInputs ||
                     function == ModBusFunction.ReadCoils)
            {
                readLength += (numberOfRegisters + 7) / 8;
            }
            else if (function == ModBusFunction.WriteMultipleRegisters ||
                     function == ModBusFunction.WriteSingleRegister)
            {
                readLength += 3;
            }
            else if (function == ModBusFunction.WriteMultipleCoils ||
                     function == ModBusFunction.WriteSingleCoil)
            {
                readLength += 3;
            }

            var frame  = new byte[readLength];
            var sourceIndex = input.Length - (readLength + 2);
            if (sourceIndex > input.Length || sourceIndex < 0)
            {
                return input;
            }
            Array.Copy(input, sourceIndex, frame, 0, readLength);

            return frame;
        }


        protected override byte[] BuildReadFrame(byte slaveId, int addr, int numberOfRegister, ModBusFunction function)
        {
            var rtuFrame = new[]
            {
                slaveId,
                (byte)function,
                Utils.ShiftRight(addr, 8),
                (byte)(addr & 0x00FF),
                Utils.ShiftRight(numberOfRegister, 8),
                (byte)(numberOfRegister & 0x00FF)
            };

            return BuildFrame(_serial, rtuFrame);
        }

        /// <summary>
        /// Wraps a Modbus RTU frame (slave id, function code and data, without CRC) in a Solarman V5 request:
        /// header, payload (frame type, sensor type, times), the RTU frame with its CRC, checksum and end byte.
        /// </summary>
        public static byte[] BuildFrame(uint loggerSerial, byte[] rtuFrameWithoutCrc)
        {
            var frame = new List<byte>
            {
                V5Start,
                0x00, //length will be set later
                0x00, //length will be set later
                0x10, //control code
                0x45, //control code
                0x00, //serial
                0x00, //serial
            };

            var serial = new byte[4];
            BinaryPrimitives.WriteUInt32LittleEndian(serial, loggerSerial);
            frame.AddRange(serial);

            frame.AddRange(new List<byte>
            {
                0x02, //frame type
                0x00, //sensor type
                0x00, //sensor type
                0x00, //delivery time,
                0x00, //delivery time,
                0x00, //delivery time,
                0x00, //delivery time,
                0x00, //power on time,
                0x00, //power on time,
                0x00, //power on time,
                0x00, //power on time,
                0x00, //offset time,
                0x00, //offset time,
                0x00, //offset time,
                0x00, //offset time,
            });

            frame.AddRange(rtuFrameWithoutCrc);
            frame.AddRange(ModbusUtility.CalculateCrc(rtuFrameWithoutCrc));

            frame.Add(0x00); //checksum - will be set later
            frame.Add(V5End);

            var result = frame.ToArray();

            var length = result.Length - V5EnvelopeLength;
            result[2] = Utils.ShiftRight(length, 8);
            result[1] = (byte)(length & 0x00FF);

            result[^2] = ModbusUtility.CalculateCrc(result, 1, result.Length - 1);

            return result;
        }

        protected override int HeaderLength => V5EnvelopeLength;
    }
}
