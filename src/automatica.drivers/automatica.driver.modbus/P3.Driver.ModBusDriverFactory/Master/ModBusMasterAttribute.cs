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

        protected override async Task Write(object value, IWriteContext writeContext, CancellationToken token = new CancellationToken())
        {
            try
            {
                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

                var shortValue = _attribute.ConvertValueToBus( value, out var convertedValue);
                DriverContext.Logger.LogInformation(
                    $"WRITE value ({convertedValue}) to {_parent.Name + $"({_parent.DeviceId})" + Name} (Register: {_attribute.Register}, Length: {_attribute.RegisterLength}, Table: {_attribute.Table})");
                switch (_attribute.Table)
                {
                    case ModBusTable.Coil:
                        await Driver.WriteCoil(_parent.DeviceId, _attribute.Register, shortValue[0] == 1, cts.Token);
                        break;
                    case ModBusTable.HoldingRegister:
                    {
                        for (int i = 0; i < _attribute.RegisterLength; i++)
                        {
                            var registerAddress = (ushort)(_attribute.Register + i);
                            switch (_attribute.Table)
                            {
                                case ModBusTable.HoldingRegister:
                                    await Driver.WriteHoldingRegister(_parent.DeviceId, registerAddress, shortValue[i],
                                        cts.Token);
                                    break;
                            }
                        }

                        break;
                    }
                    case ModBusTable.DiscreteInput:
                    case ModBusTable.InputRegister:
                        //not write able
                        return;
                }

                await writeContext.DispatchValue(value, cts.Token);
            }
            catch (Exception e)
            {
                DriverContext.Logger.LogError(e, $"Could not write value ({_parent.DeviceId}, {_attribute.Register} on {_attribute.Table})...{e}");
            }
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
