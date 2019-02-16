using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Automatica.Core.Base.TelegramMonitor;
using Automatica.Core.Driver.Monitor;
using Automatica.Core.Driver.Utility;

namespace P3.Driver.ModBusDriver.Master
{

    public enum ModBusRequestStatus
    {
        Success,
        InvalidSlaveAnswered,
        InvalidDataRead,
        InvalidReturnFunction,
        Exception
    }

    public class ModBusExceptionReturn : ModBusFrameReturn
    {
        public ModBusExceptionCode Exception { get; }

        public ModBusExceptionReturn(ModBusExceptionCode exception) : base(ModBusRequestStatus.Exception, null)
        {
            Exception = exception;
        }

        public override string ToString()
        {
            return Enum.GetName(typeof(ModBusExceptionCode), Exception);
        }
    }
    public class ModBusIoStateValueReturn : ModBusReturn
    {
        public IList<bool> Data { get; }
        public IList<ushort> DataShort { get; }

        public ModBusIoStateValueReturn(ModBusRequestStatus modBusRequestStatus, IList<bool> data, IList<ushort> dataShort) : base(modBusRequestStatus)
        {
            Data = data;
            DataShort = dataShort;
        }
    }

    public class ModBusFrameReturn : ModBusReturn
    {
        public IList<byte> Data { get; }

        public ModBusFrameReturn(ModBusRequestStatus modBusRequestStatus, IList<byte> data) : base(modBusRequestStatus)
        {
            Data = data;
        }
    }

    public class ModBusRegisterValueReturn : ModBusReturn
    {
        public IList<ushort> Data { get; }
        public IList<ushort> DataShort { get; }

        public ModBusRegisterValueReturn(ModBusRequestStatus modBusRequestStatus, IList<ushort> data, IList<ushort> dataShort) : base(modBusRequestStatus)
        {
            Data = data;
            DataShort = dataShort;
        }
    }

    public class ModBusReturn
    {
        public ModBusRequestStatus ModBusRequestStatus { get; }

       

        public ModBusReturn(ModBusRequestStatus modBusRequestStatus)
        {
            ModBusRequestStatus = modBusRequestStatus;
        }
    }

    public abstract class ModBusMasterDriver<T> : IModBusMasterDriver where T : ModBusMasterConfig
    {
        protected readonly T Config;

        protected ModBusMasterDriver(T config, ITelegramMonitorInstance monitor)
        {
            Config = config;
            Monitor = monitor;
        }

        public abstract bool Connected { get; }
        public ITelegramMonitorInstance Monitor { get; }

        public bool Open()
        {
            return OpenConnection();
        }
       
        public async Task<ModBusReturn> ReadInputRegisters(byte slaveId, ushort addr, int numberOfRegisters)
        {
            return await ReadRegisters(slaveId, addr, numberOfRegisters, ModBusFunction.ReadInputRegisters);
        }

        public async Task<ModBusReturn> ReadRegisters(byte slaveId, ushort addr, int numberOfRegisters)
        {
            return await ReadRegisters(slaveId, addr, numberOfRegisters, ModBusFunction.ReadHoldingRegisters);
        }

        public async Task<ModBusReturn> ReadCoils(byte slaveId, ushort addr, int numberOfRegisters)
        {
            return await ReadCoils(slaveId, addr, numberOfRegisters, ModBusFunction.ReadCoils);
        }

        public async Task<ModBusReturn> ReadDiscreteInputs(byte slaveId, ushort addr, int numberOfRegisters)
        {
            return await ReadCoils(slaveId, addr, numberOfRegisters, ModBusFunction.ReadDiscreteInputs);
        }


        public async Task<ModBusReturn> WriteHoldingRegister(int deviceId, ushort register, ushort value)
        {
            return await WriteSingleRegister((byte) deviceId, register, value);
        }
        

        public async Task<ModBusReturn> WriteCoil(int deviceId, ushort register, bool value)
        {
            return await WriteSingleCoil((byte)deviceId, register, value);
        }

        public async Task<ModBusReturn> WriteMultipleHoldingRegisters(int deviceId, ushort startRegister, ushort[] values)
        {
            var functionCode = ModBusFunction.WriteMultipleRegisters;
            var dataFrame = new byte[6+ (2 * values.Length)];

            var amount = values.Length;

            dataFrame[0] = (byte)functionCode;

            dataFrame[1] = Utils.ShiftRight(startRegister, 8);
            dataFrame[2] = (byte)(startRegister & 0x00FF);
            dataFrame[3] = Utils.ShiftRight(amount, 8);
            dataFrame[4] = (byte)(amount & 0x00FF);
            dataFrame[5] = (byte)(2 * values.Length);

            int pos = 6;
            foreach (var value in values)
            {
                dataFrame[pos++] = Utils.ShiftRight(value, 8);
                dataFrame[pos++] = (byte)(value & 0x00FF);
            }

            var frame = BuildHeaderFromDataFrame(dataFrame, (byte)deviceId, functionCode);

            await Monitor.NotifyTelegram(TelegramDirection.Output, "MASTER", deviceId.ToString(), Utils.ByteArrayToString(frame), $"WriteMultipleRegisters from {startRegister} values {String.Join(',', values)}");
            await WriteFrame(frame);

            var readFrame = await RequestFrame((byte)deviceId, functionCode);

            if (readFrame.ModBusRequestStatus != ModBusRequestStatus.Success)
            {
                return readFrame;
            }

            return new ModBusReturn(ModBusRequestStatus.Success);

        }

        public async Task<ModBusReturn> WriteMultipleCoils(int deviceId, ushort startRegister, bool[] values)
        {
            var functionCode = ModBusFunction.WriteMultipleCoils;
            int bytesToWrite = values.Length / 8 + 1;

            var dataFrame = new byte[6 + bytesToWrite];

            var amount = values.Length;

            dataFrame[0] = (byte)functionCode;

            dataFrame[1] = Utils.ShiftRight(startRegister, 8);
            dataFrame[2] = (byte)(startRegister & 0x00FF);
            dataFrame[3] = Utils.ShiftRight(amount, 8);
            dataFrame[4] = (byte)(amount & 0x00FF);
            dataFrame[5] = (byte)bytesToWrite;

            int pos = 6;

            for (int i = 0; i < bytesToWrite; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    int index = j + (i * 7) + (i > 0 ? 1 : 0);
                    if (index >= values.Length)
                    {
                        break;
                    }
                    if (values[index])
                    {
                        dataFrame[pos] = Utils.SetBitsTo1(dataFrame[pos], (byte)j);
                    }
                }
                pos++;
            }

            var frame = BuildHeaderFromDataFrame(dataFrame, (byte)deviceId, functionCode);

            await Monitor.NotifyTelegram(TelegramDirection.Output, "MASTER", deviceId.ToString(), Utils.ByteArrayToString(frame), $"WriteMultipleCoils from {startRegister} values {String.Join(',', values)}");
            await WriteFrame(frame);

            var readFrame = await RequestFrame((byte)deviceId, functionCode);

            if (readFrame.ModBusRequestStatus != ModBusRequestStatus.Success)
            {
                return readFrame;
            }

            return new ModBusReturn(ModBusRequestStatus.Success);
        }

        private async Task<ModBusReturn> ReadCoils(byte slaveId, ushort addr, int numberOfRegisters, ModBusFunction function)
        {
            var readFrame = await GetFrame(slaveId, addr, numberOfRegisters, function);

            if (readFrame.ModBusRequestStatus != ModBusRequestStatus.Success)
            {
                return new ModBusReturn(readFrame.ModBusRequestStatus);
            }
            var requestRegisters = readFrame.Data[2];

            var offset = 3;
            var offsetEnd = offset + requestRegisters;
            var bitsRead = 0;
            var bitsToRead = numberOfRegisters;
            var data = new List<bool>();
            var dataShort = new List<ushort>();

            for (var i = offset; i < offsetEnd; i++)
            {
                /* Shift reg hi_byte to temp */
                var temp = readFrame.Data[i];

                for (var bit = 0x00; bit < bitsToRead; bit++)
                {
                    if (bit == 8)
                    {
                        break;
                    }
                    bitsRead++;
                    var value = Utils.IsBitSet(temp, (byte) bit);
                    data.Add(value);
                    dataShort.Add(value ? (ushort)1 : (ushort)0);
                }

                bitsToRead -= bitsRead;
            }

            return new ModBusIoStateValueReturn(ModBusRequestStatus.Success, data, dataShort);
        }

        private async Task<ModBusReturn> WriteSingleCoil(byte slaveId, int addr, bool value)
        {
            var dataFrame = new byte[5];

            dataFrame[0] = (byte) ModBusFunction.WriteSingleCoil;

            dataFrame[1] = Utils.ShiftRight(addr, 8);
            dataFrame[2] = (byte) (addr & 0x00FF);
            dataFrame[3] = value ? (byte) 0xff : (byte) 0x00;
            dataFrame[4] = 0;

            var frame = BuildHeaderFromDataFrame(dataFrame, slaveId, ModBusFunction.WriteSingleCoil);


            await Monitor.NotifyTelegram(TelegramDirection.Output, "MASTER", slaveId.ToString(), Utils.ByteArrayToString(frame), $"WriteSingleCoil at {addr} value {value}");
            await WriteFrame(frame);

            var readFrame = await RequestFrame(slaveId, ModBusFunction.WriteSingleCoil);

            if (readFrame.ModBusRequestStatus != ModBusRequestStatus.Success)
            {
                return readFrame;
            }

            return new ModBusReturn(ModBusRequestStatus.Success);
        }


        private async Task<ModBusReturn> WriteSingleRegister(byte slaveId, ushort addr, ushort value)
        {
            var dataFrame = new byte[5];

            dataFrame[0] = (byte)ModBusFunction.WriteSingleRegister;

            dataFrame[1] = Utils.ShiftRight(addr, 8);
            dataFrame[2] = (byte)(addr & 0x00FF);
            dataFrame[3] = Utils.ShiftRight(value, 8);
            dataFrame[4] = (byte)(value & 0x00FF);

            var frame = BuildHeaderFromDataFrame(dataFrame, slaveId, ModBusFunction.WriteSingleRegister);

            await Monitor.NotifyTelegram(TelegramDirection.Output, "MASTER", slaveId.ToString(), Utils.ByteArrayToString(frame), $"WriteSingleRegister at {addr} value {value}");
            await WriteFrame(frame);

            var readFrame = await RequestFrame(slaveId, ModBusFunction.WriteSingleRegister);

            if (readFrame.ModBusRequestStatus != ModBusRequestStatus.Success)
            {
                return readFrame;
            }

            return new ModBusReturn(ModBusRequestStatus.Success);
        }

        private async Task<ModBusReturn> ReadRegisters(byte slaveId, ushort addr, int numberOfRegisters, ModBusFunction function)
        {
            var readFrame = await GetFrame(slaveId, addr, numberOfRegisters, function);

            if (readFrame.ModBusRequestStatus != ModBusRequestStatus.Success)
            {
                return readFrame;
            }

            var data = new List<ushort>();
            var dataShort = new List<ushort>();

            var requestRegisters = readFrame.Data[2];

            for (int i = 0; i < requestRegisters; i+=2)
            {
                var value = Utils.GetUShort(readFrame.Data[3+ i], readFrame.Data[3 + i + 1]);
                data.Add(value);
                dataShort.Add(value);
            }

            return new ModBusRegisterValueReturn(ModBusRequestStatus.Success, data, dataShort);
        }

        private async Task<ModBusFrameReturn> GetFrame(byte slaveId, ushort addr, int numberOfRegisters, ModBusFunction function)
        {
            var frame = BuildReadFrame(slaveId, addr, numberOfRegisters, function);
            var length = GetRequestLength(frame);

            frame[4] = Utils.ShiftRight(length, 8);
            frame[5] = (byte)(length & 0x00FF);

            await Monitor.NotifyTelegram(TelegramDirection.Output, "MASTER", slaveId.ToString(), Utils.ByteArrayToString(frame), $"GetFrame Addr: {addr} number{numberOfRegisters} function {function}");
            await WriteFrame(frame);


            return await RequestFrame(slaveId, function);
        }

        private async Task<ModBusFrameReturn> RequestFrame(byte slaveId, ModBusFunction function)
        {
            var readFrame = await ReadFrame();

            await Monitor.NotifyTelegram(TelegramDirection.Input, slaveId.ToString(), "MASTER", Utils.ByteArrayToString(readFrame), null);

            if (readFrame == null || readFrame.Length == 0)
            {
                return new ModBusFrameReturn(ModBusRequestStatus.InvalidDataRead, null);
            }
            if (slaveId != readFrame[0])
            {
                return new ModBusFrameReturn(ModBusRequestStatus.InvalidSlaveAnswered, null);

            }
            var retFunction = readFrame[1];
            if ((byte)function != retFunction)
            {
                if (retFunction >= 0x80)
                {
                    return new ModBusExceptionReturn((ModBusExceptionCode)readFrame[2]);
                }
                return new ModBusFrameReturn(ModBusRequestStatus.InvalidReturnFunction, null);
            }
            return new ModBusFrameReturn(ModBusRequestStatus.Success, readFrame.ToList());
        }

        protected abstract byte[] BuildReadFrame(byte slaveId, int addr, int numberOfRegister, ModBusFunction function);
        protected abstract byte[] BuildHeaderFromDataFrame(byte[] dataFrame, byte slaveId, ModBusFunction function);

        protected abstract short GetRequestLength(byte[] request);

        protected abstract bool OpenConnection();
        protected abstract Task<bool> WriteFrame(byte[] data);
        protected abstract Task<byte[]> ReadFrame();

        protected abstract Task<bool> Close();
    }
}
