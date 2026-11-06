using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.Driver;
using Microsoft.Extensions.Logging;
using P3.Driver.ModBusDriver;
using P3.Driver.ModBusDriver.Master;
using P3.Driver.ModBusDriverFactory.Attributes;
using P3.Driver.ModBusDriverFactory.Common;

namespace P3.Driver.ModBusDriverFactory.Master
{

    public class ModBusMasterAttribute : DriverBase
    {
        public IModBusMasterDriver Driver { get; }
        private readonly ModBusMasterDevice _parent;
        private readonly ModBusAttribute _attribute;

        public ModBusAttribute Attribute => _attribute;

        public ModBusMasterAttribute(IDriverContext driverContext, ModBusMasterDevice parent, IModBusMasterDriver driver, ModBusAttribute attribute) : base(driverContext)
        {
            Driver = driver;
            _parent = parent;
            _attribute = attribute;
        }

        /// <summary>
        /// Writes the value to the device. A failed bus write throws, so the write retry of <see cref="DriverBase"/> sees the failure;
        /// the value is only dispatched once the device acknowledged the write.
        /// </summary>
        protected override async Task Write(object value, IWriteContext writeContext, CancellationToken token = new CancellationToken())
        {
            if (_attribute.Table == ModBusTable.DiscreteInput || _attribute.Table == ModBusTable.InputRegister)
            {
                // not writeable
                DriverContext.Logger.LogWarning($"Could not write value ({value}) to {_parent.DeviceId}, {_attribute.Register}: {_attribute.Table} is read only");
                return;
            }

            ushort[] busValue;
            object convertedValue;
            try
            {
                busValue = _attribute.ConvertValueToBus(value, out convertedValue);
            }
            catch (Exception e)
            {
                // an invalid value stays invalid, a retry cannot help
                DriverContext.Logger.LogError(e, $"Could not convert value ({value}) for {_parent.DeviceId}, {_attribute.Register} on {_attribute.Table}...{e}");
                return;
            }

            DriverContext.Logger.LogInformation(
                $"WRITE value ({convertedValue}) to {_parent.Name + $"({_parent.DeviceId})" + Name} (Register: {_attribute.Register}, Length: {_attribute.RegisterLength}, Table: {_attribute.Table})");

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
            cts.CancelAfter(TimeSpan.FromSeconds(5));

            ModBusReturn result;
            if (_attribute.Table == ModBusTable.Coil)
            {
                result = await Driver.WriteCoil(_parent.DeviceId, _attribute.Register, busValue[0] == 1, cts.Token);
            }
            else if (busValue.Length == 1)
            {
                result = await Driver.WriteHoldingRegister(_parent.DeviceId, _attribute.Register, busValue[0], cts.Token);
            }
            else
            {
                // 32/64 bit values in one request (FC16), never as separate partial writes
                result = await Driver.WriteMultipleHoldingRegisters(_parent.DeviceId, _attribute.Register, busValue, cts.Token);
            }

            if (result == null || result.ModBusRequestStatus != ModBusRequestStatus.Success)
            {
                var reason = result switch
                {
                    null => "no response",
                    ModBusExceptionReturn exceptionReturn => $"exception {exceptionReturn}",
                    _ => result.ModBusRequestStatus.ToString()
                };
                DriverContext.Logger.LogError($"Could not write value ({value}) to {_parent.DeviceId}, {_attribute.Register} on {_attribute.Table}: {reason}");

                throw result is ModBusExceptionReturn exReturn
                    ? new ModBusException(exReturn.Exception)
                    : new IOException($"ModBus write to {_parent.DeviceId}, {_attribute.Register} on {_attribute.Table} failed: {reason}");
            }

            // dispatch what the device holds now (rounded/scaled back with the read formula)
            await writeContext.DispatchValue(_attribute.ConvertValueFromBus(busValue), token);
        }

        protected override async Task<bool> Read(IReadContext readContext, CancellationToken token = new CancellationToken())
        {
            return await ReadValue(token) != null;
        }

        public async Task<object> ReadValue(CancellationToken token = default)
        {
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            try
            {
                ModBusReturn modbusReturn = null;
                switch (_attribute.Table)
                {
                    case ModBusTable.Coil:
                        modbusReturn = await Driver.ReadCoils(_parent.DeviceId, _attribute.Register,
                            _attribute.RegisterLength, cts.Token);
                        break;
                    case ModBusTable.HoldingRegister:
                        modbusReturn = await Driver.ReadRegisters(_parent.DeviceId, _attribute.Register,
                            _attribute.RegisterLength, cts.Token);
                        break;
                    case ModBusTable.InputRegister:
                        modbusReturn = await Driver.ReadInputRegisters(_parent.DeviceId, _attribute.Register,
                            _attribute.RegisterLength, cts.Token);
                        break;
                    case ModBusTable.DiscreteInput:
                        modbusReturn = await Driver.ReadDiscreteInputs(_parent.DeviceId, _attribute.Register,
                            _attribute.RegisterLength, cts.Token);
                        break;
                }

                if (modbusReturn != null)
                {
                    if (modbusReturn is ModBusExceptionReturn excReturn)
                    {
                        DriverContext.Logger.LogError(
                            $"{Parent.Parent.Name} - {Parent.Name} - {Name}: Could not read register {Attribute.Register} - Error {excReturn}");
                        throw new ModBusException(excReturn.Exception);
                    }

                    switch (_attribute.Table)
                    {
                        case ModBusTable.Coil:
                        case ModBusTable.DiscreteInput:
                            if (modbusReturn is ModBusIoStateValueReturn ioStateReturn)
                            {
                                return _attribute.ConvertValueFromBus(ioStateReturn.DataShort.ToArray());
                            }

                            break;
                        case ModBusTable.HoldingRegister:
                        case ModBusTable.InputRegister:
                            if (modbusReturn is ModBusRegisterValueReturn registerStateReturn)
                            {
                                return _attribute.ConvertValueFromBus(registerStateReturn.DataShort.ToArray());
                            }

                            break;
                    }

                }
            }
            catch (IOException)
            {
                throw;
            }
            catch (Exception e)
            {
                DriverContext.Logger.LogError(e, $"Could not read value ({_parent.DeviceId}, {_attribute.Register} on {_attribute.Table})...{e}");
            }

            return null;
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            return null;
        }
    }
}
