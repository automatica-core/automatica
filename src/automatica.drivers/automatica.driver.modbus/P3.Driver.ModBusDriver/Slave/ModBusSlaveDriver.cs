using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Automatica.Core.Base.TelegramMonitor;
using P3.Driver.ModBusDriver.Exceptions;
using Automatica.Core.Driver.Utility;
[assembly: InternalsVisibleTo("P3.Driver.ModBus.Tests")]

namespace P3.Driver.ModBusDriver.Slave
{
    public class RegisterChangedEventArgs : EventArgs {
        public int DeviceId { get; }
        public ushort StartIndex { get;  }
        public ushort Length { get; }

        public RegisterChangedEventArgs(int deviceId, ushort index, ushort length)
        {
            DeviceId = deviceId;
            StartIndex = index;
            Length = length;
        }
    }
    public abstract class ModBusSlaveDriver<T> : IModBusSlaveDriver where T : ModBusSlaveConfig
    {
        protected T Config { get; }
        public ITelegramMonitorInstance TelegramMonitor { get; }
        public bool Opened { get; private set; }

        internal Dictionary<int, ModBusSlaveStore> DeviceStore = new Dictionary<int, ModBusSlaveStore>();

        public event EventHandler<RegisterChangedEventArgs> HoldingRegisterChanged;
        public event EventHandler<RegisterChangedEventArgs> CoilRegisterChanged;
        private readonly object _lock = new object();

        protected ModBusSlaveDriver(T config, ITelegramMonitorInstance telegramMonitor)
        {
            Config = config;
            TelegramMonitor = telegramMonitor;
        }
        

        public bool Close()
        {
            return CloseConnection();
        }

        public void InitInputRegister(in int deviceId, in ushort register, in ushort value)
        {
            lock (_lock)
            {
                if (DeviceStore.ContainsKey(deviceId))
                {
                    DeviceStore[deviceId].InitInputRegister(in register, in value);
                }
            }
        }
        public void InitHoldingRegister(in int deviceId, in ushort register, in ushort value)
        {
            lock (_lock)
            {
                if (DeviceStore.ContainsKey(deviceId))
                {
                    DeviceStore[deviceId].InitHoldingRegister(in register, in value);
                }
            }
        }
        public void InitDiscreteInput(in int deviceId, in ushort register, in bool value)
        {
            lock (_lock)
            {
                if (DeviceStore.ContainsKey(deviceId))
                {
                    DeviceStore[deviceId].InitDiscreteInput(in register, in value);
                }
            }
        }
        public void InitCoil(in int deviceId, in ushort register, in bool value)
        {
            lock (_lock)
            {
                if (DeviceStore.ContainsKey(deviceId))
                {
                    DeviceStore[deviceId].InitCoil(in register, in value);
                }
            }
        }

        public void SetInputRegister(in int deviceId, in ushort register, in ushort value)
        {
            lock (_lock)
            {
                if (DeviceStore.ContainsKey(deviceId))
                {
                    DeviceStore[deviceId].SetInputRegister(in register, in value);
                }
            }
        }
        public void SetHoldingRegister(in int deviceId, in ushort register, in ushort value)
        {
            lock (_lock)
            {
                if (DeviceStore.ContainsKey(deviceId))
                {
                    DeviceStore[deviceId].SetHoldingRegister(in register, in value);
                }
            }
        }
        public void SetDiscreteInput(in int deviceId, in ushort register, in bool value)
        {
            lock (_lock)
            {
                if (DeviceStore.ContainsKey(deviceId))
                {
                    DeviceStore[deviceId].SetDiscreteInput(in register, in value);
                }
            }
        }
        public void SetCoil(in int deviceId, in ushort register, in bool value)
        {
            lock (_lock)
            {
                if (DeviceStore.ContainsKey(deviceId))
                {
                    DeviceStore[deviceId].SetCoil(in register, in value);
                }
            }
        }

        public ushort GetHoldingRegister(in int deviceId, in ushort register)
        {
            lock (_lock)
            {
                var reg = GetHoldingRegisters(in deviceId, in register, 1);
                if (reg.Length == 1)
                {
                    return reg[0];
                }
                throw new ArgumentException("Invalid register");
            }
        }

        public Span<ushort> GetHoldingRegisters(in int deviceId, in ushort registers, in ushort length)
        {
            lock (_lock)
            {
                if (DeviceStore.ContainsKey(deviceId))
                {
                    var sh = new ushort[length];
                    for (ushort i = 0; i < length; i++)
                    {
                        var addr = (ushort)(registers + i);

                        if (!DeviceStore[deviceId].HasRegister(in addr))
                        {
                            throw new IllegalDataAddressException();
                        }

                        sh[i] = DeviceStore[deviceId].GetRegisterValue(in addr);
                    }
                    return sh;
                }
                throw new ArgumentException($"Modbus device with id {deviceId} is not initialized");
            }
        }

        public ushort GetInputRegister(in int deviceId, in ushort register)
        {
            lock (_lock)
            {
                var reg = GetInputRegisters(in deviceId, in register, 1);
                if (reg.Length == 1)
                {
                    return reg[0];
                }
                throw new ArgumentException("Invalid register");
            }
        }

        public Span<ushort> GetInputRegisters(in int deviceId, in ushort registers, in ushort length)
        {
            lock (_lock)
            {
                if (DeviceStore.ContainsKey(deviceId))
                {

                    var sh = new ushort[length];
                    for (ushort i = 0; i < length; i++)
                    {
                        var addr = (ushort)(registers + i);

                        if (!DeviceStore[deviceId].HasInputRegister(in addr))
                        {
                            throw new IllegalDataAddressException();
                        }

                        sh[i] = DeviceStore[deviceId].GetInputRegisterValue(in addr);
                    }
                    return sh;
                }
                throw new ArgumentException($"Modbus device with id {deviceId} is not initialized");
            }
        }

        public bool GetCoil(in int deviceId, in ushort register)
        {
            lock (_lock)
            {
                var reg = GetCoils(in deviceId, in register, 1);
                if (reg.Length == 1)
                {
                    return reg[0];
                }
                throw new ArgumentException("Invalid register");
            }
        }

        public Span<bool> GetCoils(in int deviceId, in ushort register, in ushort length)
        {
            lock (_lock)
            {
                if (DeviceStore.ContainsKey(deviceId))
                {

                    var sh = new bool[length];
                    for (ushort i = 0; i < length; i++)
                    {
                        var addr = (ushort)(register + i);

                        if (!DeviceStore[deviceId].HasCoil(in addr))
                        {
                            throw new IllegalDataAddressException();
                        }

                        sh[i] = DeviceStore[deviceId].GetCoilValue(in addr);
                    }
                    return sh;
                }
                throw new ArgumentException($"Modbus device with id {deviceId} is not initialized");
            }
        }

        public bool GetDiscreteInput(in int deviceId, in ushort register)
        {
            lock (_lock)
            {
                var reg = GetDiscreteInput(in deviceId, in register, 1);
                if (reg.Length == 1)
                {
                    return reg[0];
                }
                throw new ArgumentException("Invalid register");
            }
        }

        public Span<bool> GetDiscreteInput(in int deviceId, in ushort register, in ushort length)
        {
            lock (_lock)
            {
                if (DeviceStore.ContainsKey(deviceId))
                {
                    var sh = new bool[length];
                    for (ushort i = 0; i < length; i++)
                    {
                        var addr = (ushort)(register + i);

                        if (!DeviceStore[deviceId].HasDiscreteInput(in addr))
                        {
                            throw new IllegalDataAddressException();
                        }

                        sh[i] = DeviceStore[deviceId].GetDiscreteInputValue(in addr);
                    }
                    return sh;
                }
                throw new ArgumentException($"Modbus device with id {deviceId} is not initialized");
            }
        }

        protected void NotifyHoldingRegisterChanged(int deviceId, ushort startRegister, ushort length)
        {
            HoldingRegisterChanged?.Invoke(this, new RegisterChangedEventArgs(deviceId, startRegister, length));
        }

        protected void NotifyCoilChanged(int deviceId, ushort startRegister, ushort length)
        {
            CoilRegisterChanged?.Invoke(this, new RegisterChangedEventArgs(deviceId, startRegister, length));
        }

        protected abstract bool IgnoreDeviceId { get; }
        

        public Span<byte> GetResponseData(in byte deviceId, in byte functionCode, in ushort startIndex, in ushort length, Span<byte> frame, Span<byte> header) 
        {
            try
            {
                lock (_lock)
                {
                    TelegramMonitor.NotifyTelegram(TelegramDirection.Input, deviceId.ToString(), "SLAVE", Utils.ByteArrayToString(frame), "");
                    if (!IgnoreDeviceId && !DeviceStore.ContainsKey(deviceId))
                    {
                        //invalid device id
                        throw new GatewayPathUnavailableException();
                    }

                    ModBus.Logger.LogHexIn(header.ToArray().Concat(frame.ToArray()).ToArray());

                    byte[] data;
                    int intDeviceId = deviceId;
                    switch (functionCode)
                    {
                        case (byte) ModBusFunction.ReadHoldingRegisters:
                        {
                                data = GetRegisterValues(GetHoldingRegisters(in intDeviceId, in startIndex, in length));
                            break;
                        }
                        case (byte) ModBusFunction.ReadInputRegisters:
                        {

                            data = GetRegisterValues(GetInputRegisters(in intDeviceId, in startIndex, in length));
                            break;
                        }
                        case (byte) ModBusFunction.ReadCoils:
                        {
                            data = GetCoilValues(GetCoils(in intDeviceId, in startIndex, in length));
                            break;
                        }
                        case (byte) ModBusFunction.ReadDiscreteInputs:
                        {
                            data = GetCoilValues(GetDiscreteInput(in intDeviceId, in startIndex, in length));
                            break;
                        }
                        case (byte) ModBusFunction.WriteMultipleCoils:
                        case (byte) ModBusFunction.WriteMultipleRegisters:
                            var byteCount = frame[6];
                            var writeData = frame.Slice(7, byteCount);

                            if (functionCode == (byte) ModBusFunction.WriteMultipleRegisters)
                            {
                                SetMultipleRegister(deviceId, startIndex, writeData);
                            }
                            else if (functionCode == (byte) ModBusFunction.WriteMultipleCoils)
                            {
                                SetMultipleCoils(deviceId, startIndex, writeData, length);
                            }

                            data = new byte[6];

                            Array.Copy(BitConverter.GetBytes((short) startIndex).Reverse().ToArray(), 0, data, 2, 2);
                            Array.Copy(BitConverter.GetBytes((short) length).Reverse().ToArray(), 0, data, 4, 2);
                            break;
                        case (byte) ModBusFunction.WriteSingleCoil:
                        {

                            if (length != 0 && length != 0xFF00)
                            {
                                throw new IllegalDataValueException();
                            }
                            bool coilValue = length == 0xFF00;

                            SetCoil(deviceId,  startIndex, coilValue);
                            NotifyCoilChanged(deviceId,  startIndex, 1);

                            data = new byte[6];
                            var byteLength = BitConverter.GetBytes((short) startIndex).Reverse().ToArray();
                            Array.Copy(byteLength, 0, data, 2, 2);

                            data[4] = coilValue ? (byte) 0xFF : (byte) 0x00;
                            data[5] = 0x00;

                        }
                            break;
                        case (byte) ModBusFunction.WriteSingleRegister:
                            SetHoldingRegister(deviceId, startIndex, length);
                            NotifyCoilChanged(deviceId, startIndex, 1);

                            data = new byte[6];
                            var bLeng = BitConverter.GetBytes(startIndex).Reverse().ToArray();
                            Array.Copy(bLeng, 0, data, 2, 2);

                            var registerData = BitConverter.GetBytes(length).Reverse().ToArray();
                            Array.Copy(registerData, 0, data, 4, 2);
                            break;
                        default:
                        {
                            //invalid function code received
                            throw new IllegalFunctionException();
                        }
                    }


                    if (data == null)
                    {
                        throw new ServerDeviceFailureException();
                    }

                    data[0] = deviceId;
                    data[1] = functionCode;

                    var resp = CreateResponseFrame(data, header);

                    ModBus.Logger.LogHexOut(resp);

                    TelegramMonitor.NotifyTelegram(TelegramDirection.Output, "SLAVE", deviceId.ToString(), Utils.ByteArrayToString(resp), "");

                    return resp;
                }
            }
            catch (ModBusException modBusException)
            {
                var ex = modBusException.Serialize((ModBusFunction)functionCode, deviceId);
                var resp = CreateResponseFrame(ex, header);

                ModBus.Logger.LogHexOut(resp);
                return resp;
            }
        }

        protected abstract Span<byte> CreateResponseFrame(Span<byte> data, Span<byte> header);

        private byte[] CreateResponse(byte[] data)
        {
            var res = new byte[3 + data.Length * 2];
            res[0] = 0; //device Id
            res[1] = 0; //functionCode
            res[2] = (byte)(data.Length); //byte count 

            Array.Copy(data, 0, res, 3, data.Length);
            return res;
        }

        internal void SetMultipleRegister(int deviceId, ushort startIndex, Span<byte> data)
        {
            var pos = 0;
            for (int i = 0; i < data.Length/2; i++)
            {
                var value = Utils.GetUShort(data[pos], data[pos + 1]);
                var curIndex = (ushort)(startIndex + i);

                SetHoldingRegister(deviceId, curIndex, value);

                pos += 2;

            }
        }

        internal void SetMultipleCoils(int deviceId, ushort startIndex, Span<byte> data, ushort length)
        {
            var pos = 0;
            var bitIndex = (byte)0;
            for (var i = 0; i < length; i++)
            {
                if (bitIndex == 8)
                {
                    bitIndex = 0;
                }
                var curIndex = (ushort)(startIndex + i);

                var byteIndex = i / 8;
                var usedByte = data[byteIndex];

                var value = Utils.IsBitSet(usedByte, bitIndex);

                SetCoil(deviceId, curIndex, value);

                bitIndex++;
                if (pos == length)
                {
                    break;
                }
            }
        }

        internal byte[] GetByteValues(Span<ushort> values)
        {
            Span<byte> responseData = new byte[values.Length * 2];
            var pos = 0;
            for (int i = 0; i < values.Length; i++)
            {
                var byteData = BitConverter.GetBytes(values[i]).Reverse().ToArray();

                responseData[pos++] = byteData[0];
                responseData[pos++] = byteData[1];

            }
            return responseData.ToArray();
        }

        internal byte[] GetRegisterValues(Span<ushort> values)
        {
            return CreateResponse(GetByteValues(values));
        }

        internal byte[] GetBinaryValues(Span<bool> data)
        {
            var responseData = new byte[(short)Math.Ceiling((double)data.Length/ 8)];

            var d = (byte)0x00;
            var x = (byte)0;
            for (int i = 0; i < data.Length; i++)
            {
                if (x == 8)
                {
                    x = 0;
                    d = 0;
                }
               
                var value = data[i];
                if (value)
                {
                    d = Utils.SetBitsTo1(d, x);
                }
                x++;

                responseData[i / 8] = d;
            }
            return responseData;
        }

        internal byte[] GetCoilValues(Span<bool> data)
        {
            var responseData = GetBinaryValues(data);

            return CreateResponse(responseData);
        }

        public bool Open()
        {
            Opened = true;
            return OpenConnection();
        }

        protected abstract bool OpenConnection();
        protected abstract bool CloseConnection();
    }
}
