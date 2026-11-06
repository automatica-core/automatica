using System;

namespace P3.Driver.ModBusDriver.Slave
{
    public interface IModBusSlaveDriver
    {
        /// <summary>
        /// A master wrote holding registers (FC06, FC16)
        /// </summary>
        event EventHandler<RegisterChangedEventArgs> HoldingRegisterChanged;

        /// <summary>
        /// A master wrote coils (FC05, FC15)
        /// </summary>
        event EventHandler<RegisterChangedEventArgs> CoilRegisterChanged;

        bool Open();
        bool Close();
        void InitInputRegister(in int deviceId, in ushort register, in ushort value );
        void InitHoldingRegister(in int deviceId, in ushort register, in ushort value );
        void InitDiscreteInput(in int deviceId, in ushort register, in bool value);
        void InitCoil(in int deviceId, in ushort register, in bool value);
        void SetInputRegister(in int deviceId, in ushort register, in ushort value);
        void SetHoldingRegister(in int deviceId, in ushort register, in ushort value);
        void SetDiscreteInput(in int deviceId, in ushort register, in bool value);
        void SetCoil(in int deviceId, in ushort register, in bool value);


        ushort GetHoldingRegister(in int deviceId, in ushort register);
        Span<ushort> GetHoldingRegisters(in int deviceId, in ushort registers, in ushort length);

        ushort GetInputRegister(in int deviceId, in ushort register);
        Span<ushort> GetInputRegisters(in int deviceId, in ushort registers, in ushort length);

        bool GetCoil(in int deviceId, in ushort register);
        Span<bool> GetCoils(in int deviceId, in ushort register, in ushort length);

        bool GetDiscreteInput(in int deviceId, in ushort register);
        Span<bool> GetDiscreteInput(in int deviceId, in ushort register, in ushort length);
    }
}
