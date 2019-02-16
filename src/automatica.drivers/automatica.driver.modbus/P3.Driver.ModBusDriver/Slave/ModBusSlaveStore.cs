using System;
using System.Collections.Generic;
using P3.Driver.ModBusDriver.Exceptions;

namespace P3.Driver.ModBusDriver.Slave
{
    public class ModBusSlaveStore
    {
        public int DeviceId { get; }

        private Dictionary<ushort, ushort> InputRegisterValues { get; } = new Dictionary<ushort, ushort>();
        private Dictionary<ushort, ushort> HoldingRegisterValues { get; } = new Dictionary<ushort, ushort>();
        private Dictionary<ushort, bool> DiscreteInputValues { get; } = new Dictionary<ushort, bool>();
        private Dictionary<ushort, bool> CoilValues { get; } = new Dictionary<ushort, bool>();

        private readonly object _lock = new object();

        public ModBusSlaveStore(int deviceId)
        {
            DeviceId = deviceId;
        }

        public void InitInputRegister(in ushort register, in ushort value)
        {
            lock (_lock)
            {
                if (!InputRegisterValues.ContainsKey(register))
                    InputRegisterValues.TryAdd(register, value);
            }
        }
        public void InitHoldingRegister(in ushort register, in ushort value)
        {
            lock (_lock)
            {
                if (!HoldingRegisterValues.ContainsKey(register))
                    HoldingRegisterValues.TryAdd(register, value);
            }
        }
        public void InitDiscreteInput(in ushort register, in bool value)
        {
            lock (_lock)
            {
                if (!DiscreteInputValues.ContainsKey(register))
                    DiscreteInputValues.TryAdd(register, value);
            }
        }

        public void InitCoil(in ushort register, in bool value)
        {
            lock (_lock)
            {
                if (!CoilValues.ContainsKey(register))
                    CoilValues.TryAdd(register, value);
            }
        }

        public void SetInputRegister(in ushort register, in ushort value)
        {
            lock (_lock)
            {
                InputRegisterValues[register] = value;
            }
        }
        public void SetHoldingRegister(in ushort register, in ushort value)
        {
            lock (_lock)
            {
                if (!HoldingRegisterValues.ContainsKey(register))
                    throw new IllegalDataAddressException();

                HoldingRegisterValues[register] = value;
            }
        }
        public void SetDiscreteInput(in ushort register, in bool value)
        {
            lock (_lock)
            {
                DiscreteInputValues[register] = value;
            }
        }
        public void SetCoil(in ushort register, in bool value)
        {
            lock (_lock)
            {
                if (!CoilValues.ContainsKey(register))
                    throw new IllegalDataValueException();

                CoilValues[register] = value;
            }
        }

        public ushort GetInputRegisterValue(in ushort addr)
        {
            if (!HasInputRegister(in addr))
            {
                throw new IllegalDataAddressException();
            }
            var value = InputRegisterValues[addr];
            return value;
        }
        public ushort GetRegisterValue(in ushort addr)
        {
            if (!HasRegister(in addr))
            {
                throw new IllegalDataAddressException();
            }
            var value = HoldingRegisterValues[addr];
            return value;
        }
        public bool GetCoilValue(in ushort addr)
        {
            if (!HasCoil(in addr))
            {
                throw new IllegalDataAddressException();
            }
            var value = CoilValues[addr];
            return value;
        }
        public bool GetDiscreteInputValue(in ushort addr)
        {
            if (!HasDiscreteInput(in addr))
            {
                throw new IllegalDataAddressException();
            }
            var value = DiscreteInputValues[addr];
            return value;
        }

        public bool HasInputRegister(in ushort addr)
        {
            return InputRegisterValues.ContainsKey(addr);
        }
        public bool HasRegister(in ushort addr)
        {
            return HoldingRegisterValues.ContainsKey(addr);
        }
        public bool HasCoil(in ushort addr)
        {
            return CoilValues.ContainsKey(addr);
        }
        public bool HasDiscreteInput(in ushort addr)
        {
            return DiscreteInputValues.ContainsKey(addr);
        }
    }
}
