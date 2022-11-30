using Automatica.Core.Base.TelegramMonitor;
using Automatica.Core.Driver.Utility;
using P3.Driver.ModBus.SolarmanV5.Config;
using P3.Driver.ModBusDriver;
using P3.Driver.ModBusDriver.Master.Tcp;

namespace P3.Driver.ModBus.SolarmanV5
{
    public class SolarmanConnection : ModBusMasterTcpDriver
    {
        private readonly byte[] _serial;
        public SolarmanConnection(SolarmanConfig config, ITelegramMonitorInstance monitor) : base(config, monitor)
        {
            _serial = BitConverter.GetBytes(config.SolarmanSerialNumber);
        }

        protected override byte[] BuildHeaderFromDataFrame(byte[] dataFrame, byte slaveId, ModBusFunction function)
        {
            var frame = new List<byte>
            {
                0xA5,
                0x00, //length will be set later
                0x00, //length will be set later
                0x10, //control code
                0x45, //control code
                0x00, //serial
                0x00, //serial
            };

            frame.AddRange(_serial);

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
            return frame.ToArray();
        }

        protected override ushort GetLengthToRead(byte[] buffer)
        {
            return (ushort)(Utils.GetUShort(buffer[2], buffer[1]) );
        }

        protected override byte[] ParseFrame(byte slaveId, ModBusFunction function, int numberOfRegisters, byte[] input)
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
            var rtuFrame = new List<byte>();
            var headerFrame = BuildHeaderFromDataFrame(new byte[] { }, slaveId, function);
            var dataFrame = new List<byte>();

            rtuFrame.Add(slaveId);
            rtuFrame.Add((byte)function);

            rtuFrame.Add(Utils.ShiftRight(addr, 8));
            rtuFrame.Add((byte)(addr & 0x00FF));
            rtuFrame.Add(Utils.ShiftRight(numberOfRegister, 8));
            rtuFrame.Add((byte)(numberOfRegister & 0x00FF));

            var crc = ModbusUtility.CalculateCrc(rtuFrame.ToArray());

            rtuFrame.AddRange(crc);

            dataFrame.AddRange(headerFrame);
            dataFrame.AddRange(rtuFrame);

            dataFrame.Add(0x00); //crc - will be set later
            dataFrame.Add(0x15);  //end byte
            var frame = dataFrame.ToArray();

            var length = GetRequestLength(frame);
            frame[2] = Utils.ShiftRight(length, 8);
            frame[1] = (byte)(length & 0x00FF);

            var crc2 = ModbusUtility.CalculateCrc(frame, 1, frame.Length-1);

            frame[^2] = crc2;

            return frame;

        }

        protected override int HeaderLength => 13;
    }
}