using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace P3.Driver.ModBusDriver.Master
{
    public interface IModBusMasterDriver
    {
        bool Connected { get; }

        bool Open();
        Task<ModBusReturn> ReadInputRegisters(byte slaveId, ushort addr, int numberOfRegisters);
        Task<ModBusReturn> ReadRegisters(byte slaveId, ushort addr, int numberOfRegisters);
        Task<ModBusReturn> ReadCoils(byte slaveId, ushort addr, int numberOfRegisters);
        Task<ModBusReturn> ReadDiscreteInputs(byte slaveId, ushort addr, int numberOfRegisters);

        Task<ModBusReturn> WriteHoldingRegister(int deviceId, ushort register, ushort value);
        Task<ModBusReturn> WriteCoil(int deviceId, ushort register, bool value);

        Task<ModBusReturn> WriteMultipleHoldingRegisters(int deviceId, ushort startRegister, ushort[] values);
        Task<ModBusReturn> WriteMultipleCoils(int deviceId, ushort startRegister, bool[] values);

    }
}
