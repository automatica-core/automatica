using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace P3.Driver.ModBusDriver
{
    public enum ModBusTable
    {
        HoldingRegister,
        InputRegister,
        Coil,
        DiscreteInput
    }

    public enum ModBusFunction
    {
        ReadCoils = 1,
        ReadDiscreteInputs = 2,
        ReadHoldingRegisters = 3,
        ReadInputRegisters = 4,

        WriteSingleCoil = 5,
        WriteSingleRegister = 6,

        ReadExceptionStatus = 7,
        Diagnostics = 8,

        WriteMultipleCoils = 0x0F,
        WriteMultipleRegisters = 0x10,

        Error = 0x83
    }

    public enum ModBusExceptionCode
    {
        NoException = 0,
        IllegalFunction = 1,
        IllegalDataAddress = 2,
        IllegalDataValue = 3,
        ServerDeviceFailure = 4,
        Ack = 5,
        ServerDeviceBusy = 6,
        MemoryParityError = 8,
        GatewayPathUnavailable = 0x0A,
        GatewayTargetDeviceFailedToRespond = 0x0B
    }

    public static class ModBus
    {
        public static ILogger Logger { get; set; } = NullLogger.Instance;
    }
}
