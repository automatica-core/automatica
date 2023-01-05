using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.TelegramMonitor;
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

        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);

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

        public async Task<bool> Stop()
        {
            return await Close();
        }

        public async Task<ModBusReturn> ReadInputRegisters(byte slaveId, ushort address, int numberOfRegisters, CancellationToken cts = default)
        {
            await _semaphore.WaitAsync(cts);
            try
            {
                return await ReadRegisters(slaveId, address, numberOfRegisters, ModBusFunction.ReadInputRegisters, cts);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<ModBusReturn> ReadRegisters(byte slaveId, ushort address, int numberOfRegisters, CancellationToken cts = default)
        {
            await _semaphore.WaitAsync(cts);
            try
            {
                return await ReadRegisters(slaveId, address, numberOfRegisters, ModBusFunction.ReadHoldingRegisters, cts);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<ModBusReturn> ReadCoils(byte slaveId, ushort address, int numberOfRegisters, CancellationToken cts = default)
        {
            await _semaphore.WaitAsync(cts);
            try
            {
                return await ReadCoils(slaveId, address, numberOfRegisters, ModBusFunction.ReadCoils, cts);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<ModBusReturn> ReadDiscreteInputs(byte slaveId, ushort address, int numberOfRegisters, CancellationToken cts = default)
        {
            await _semaphore.WaitAsync(cts);
            try
            {
                return await ReadCoils(slaveId, address, numberOfRegisters, ModBusFunction.ReadDiscreteInputs, cts);
            }
            finally
            {
                _semaphore.Release();
            }
        }


        public async Task<ModBusReturn> WriteHoldingRegister(int deviceId, ushort register, ushort value, CancellationToken cts = default)
        {
            await _semaphore.WaitAsync(cts);
            try
            {
                return await WriteSingleRegister((byte)deviceId, register, value);
            }
            finally
            {
                _semaphore.Release();
            }
        }


        public async Task<ModBusReturn> WriteCoil(int deviceId, ushort register, bool value, CancellationToken cts = default)
        {
            await _semaphore.WaitAsync(cts);
            try
            {
                return await WriteSingleCoil((byte)deviceId, register, value, cts);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<ModBusReturn> WriteMultipleHoldingRegisters(int deviceId, ushort startRegister,
            ushort[] values, CancellationToken cts = default)
        {
            await _semaphore.WaitAsync(cts);
            try
            {
                var functionCode = ModBusFunction.WriteMultipleRegisters;
                var dataFrame = new byte[6 + (2 * values.Length)];

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
                frame = UpdateWriteFrame(frame);

                await Monitor.NotifyTelegram(TelegramDirection.Output, "MASTER", deviceId.ToString(),
                    Utils.ByteArrayToString(frame),
                    $"WriteMultipleRegisters from {startRegister} values {String.Join(',', values)}");
                await WriteFrame(frame, cts);

                var readFrame = await RequestFrame((byte)deviceId, functionCode, values.Length, cts);

                if (readFrame.ModBusRequestStatus != ModBusRequestStatus.Success)
                {
                    return readFrame;
                }

                return new ModBusReturn(ModBusRequestStatus.Success);
            }
            finally
            {
                _semaphore.Release();
            }

        }

        public async Task<ModBusReturn> WriteMultipleCoils(int deviceId, ushort startRegister, bool[] values, CancellationToken cts = default)
        {
            await _semaphore.WaitAsync(cts);
            try
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
                frame = UpdateWriteFrame(frame);

                await Monitor.NotifyTelegram(TelegramDirection.Output, "MASTER", deviceId.ToString(),
                    Utils.ByteArrayToString(frame),
                    $"WriteMultipleCoils from {startRegister} values {String.Join(',', values)}");
                await WriteFrame(frame, cts);

                var readFrame = await RequestFrame((byte)deviceId, functionCode, values.Length, cts);

                if (readFrame.ModBusRequestStatus != ModBusRequestStatus.Success)
                {
                    return readFrame;
                }

                return new ModBusReturn(ModBusRequestStatus.Success);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private async Task<ModBusReturn> ReadCoils(byte slaveId, ushort address, int numberOfRegisters,
            ModBusFunction function, CancellationToken cts = default)
        {
            var readFrame = await GetFrame(slaveId, address, numberOfRegisters, function, cts);

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
                    var value = Utils.IsBitSet(temp, (byte)bit);
                    data.Add(value);
                    dataShort.Add(value ? (ushort)1 : (ushort)0);
                }

                bitsToRead -= bitsRead;
            }

            return new ModBusIoStateValueReturn(ModBusRequestStatus.Success, data, dataShort);
        }

        private async Task<ModBusReturn> WriteSingleCoil(byte slaveId, int address, bool value, CancellationToken cts = default)
        {
            var dataFrame = new byte[5];

            dataFrame[0] = (byte)ModBusFunction.WriteSingleCoil;

            dataFrame[1] = Utils.ShiftRight(address, 8);
            dataFrame[2] = (byte)(address & 0x00FF);
            dataFrame[3] = value ? (byte)0xff : (byte)0x00;
            dataFrame[4] = 0;

            var frame = BuildHeaderFromDataFrame(dataFrame, slaveId, ModBusFunction.WriteSingleCoil);
            frame = UpdateWriteFrame(frame);


            await Monitor.NotifyTelegram(TelegramDirection.Output, "MASTER", slaveId.ToString(),
                Utils.ByteArrayToString(frame), $"WriteSingleCoil at {address} value {value}");
            await WriteFrame(frame, cts);

            var readFrame = await RequestFrame(slaveId, ModBusFunction.WriteSingleCoil, 1, cts);

            if (readFrame.ModBusRequestStatus != ModBusRequestStatus.Success)
            {
                return readFrame;
            }

            return new ModBusReturn(ModBusRequestStatus.Success);
        }


        private async Task<ModBusReturn> WriteSingleRegister(byte slaveId, ushort address, ushort value)
        {
            var dataFrame = new byte[5];

            dataFrame[0] = (byte)ModBusFunction.WriteSingleRegister;

            dataFrame[1] = Utils.ShiftRight(address, 8);
            dataFrame[2] = (byte)(address & 0x00FF);
            dataFrame[3] = Utils.ShiftRight(value, 8);
            dataFrame[4] = (byte)(value & 0x00FF);

            var frame = BuildHeaderFromDataFrame(dataFrame, slaveId, ModBusFunction.WriteSingleRegister);
            frame = UpdateWriteFrame(frame);

            await Monitor.NotifyTelegram(TelegramDirection.Output, "MASTER", slaveId.ToString(),
                Utils.ByteArrayToString(frame), $"WriteSingleRegister at {address} value {value}");
            await WriteFrame(frame);

            var readFrame = await RequestFrame(slaveId, ModBusFunction.WriteSingleRegister, 1);

            if (readFrame.ModBusRequestStatus != ModBusRequestStatus.Success)
            {
                return readFrame;
            }

            return new ModBusReturn(ModBusRequestStatus.Success);
        }

        private async Task<ModBusReturn> ReadRegisters(byte slaveId, ushort address, int numberOfRegisters,
            ModBusFunction function, CancellationToken cts = default)
        {
            var readFrame = await GetFrame(slaveId, address, numberOfRegisters, function, cts);

            if (readFrame.ModBusRequestStatus != ModBusRequestStatus.Success)
            {
                return readFrame;
            }

            var data = new List<ushort>();
            var dataShort = new List<ushort>();

            var requestRegisters = readFrame.Data[2];

            for (int i = 0; i < requestRegisters; i += 2)
            {
                var value = Utils.GetUShort(readFrame.Data[3 + i], readFrame.Data[3 + i + 1]);
                data.Add(value);
                dataShort.Add(value);
            }

            return new ModBusRegisterValueReturn(ModBusRequestStatus.Success, data, dataShort);
        }

        private async Task<ModBusFrameReturn> GetFrame(byte slaveId, ushort address, int numberOfRegisters,
            ModBusFunction function, CancellationToken cts = default)
        {
            var frame = BuildReadFrame(slaveId, address, numberOfRegisters, function);

            await Monitor.NotifyTelegram(TelegramDirection.Output, "MASTER", slaveId.ToString(),
                Utils.ByteArrayToString(frame), $"GetFrame Address: {address} number{numberOfRegisters} function {function}");
            await WriteFrame(frame, cts);

            return await RequestFrame(slaveId, function, numberOfRegisters, cts);
        }

        protected virtual byte[] ParseFrame(byte slaveId, ModBusFunction function, int numberOfRegisters, byte[] input)
        {
            return input;
        }

        private async Task<ModBusFrameReturn> RequestFrame(byte slaveId, ModBusFunction function, int numberOfRegisters, CancellationToken cts = default)
        {
            var readFrame = await ReadFrame(function, numberOfRegisters, cts);

            await Monitor.NotifyTelegram(TelegramDirection.Input, slaveId.ToString(), "MASTER",
                Utils.ByteArrayToString(readFrame), null);

            if (readFrame == null || readFrame.Length == 0)
            {
                return new ModBusFrameReturn(ModBusRequestStatus.InvalidDataRead, null);
            }

            readFrame = ParseFrame(slaveId, function, numberOfRegisters, readFrame);

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


        protected abstract byte[] BuildReadFrame(byte slaveId, int address, int numberOfRegister, ModBusFunction function);

        protected abstract byte[] UpdateWriteFrame(byte[] frame);
        protected abstract byte[] BuildHeaderFromDataFrame(byte[] dataFrame, byte slaveId, ModBusFunction function);

        protected abstract short GetRequestLength(byte[] request);

        protected abstract bool OpenConnection();
        protected abstract Task<bool> WriteFrame(byte[] data, CancellationToken cts = default);
        protected abstract Task<byte[]> ReadFrame(ModBusFunction function, int numberOfRegisters, CancellationToken cts = default);

        protected abstract Task<bool> Close();
    }
}
