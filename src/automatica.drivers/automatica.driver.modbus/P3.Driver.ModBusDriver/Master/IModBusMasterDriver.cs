using System.Threading;
using System.Threading.Tasks;

namespace P3.Driver.ModBusDriver.Master
{
    public interface IModBusMasterDriver
    {
        bool Connected { get; }

        bool Open();
        Task<bool> Stop();
        Task<ModBusReturn> ReadInputRegisters(byte slaveId, ushort address, int numberOfRegisters, CancellationToken cts = default);
        Task<ModBusReturn> ReadRegisters(byte slaveId, ushort address, int numberOfRegisters, CancellationToken cts = default);
        Task<ModBusReturn> ReadCoils(byte slaveId, ushort address, int numberOfRegisters, CancellationToken cts = default);
        Task<ModBusReturn> ReadDiscreteInputs(byte slaveId, ushort address, int numberOfRegisters, CancellationToken cts = default);

        Task<ModBusReturn> WriteHoldingRegister(int deviceId, ushort register, ushort value, CancellationToken cts = default);
        Task<ModBusReturn> WriteCoil(int deviceId, ushort register, bool value, CancellationToken cts = default);

        Task<ModBusReturn> WriteMultipleHoldingRegisters(int deviceId, ushort startRegister, ushort[] values, CancellationToken cts = default);
        Task<ModBusReturn> WriteMultipleCoils(int deviceId, ushort startRegister, bool[] values, CancellationToken cts = default);

    }
}
