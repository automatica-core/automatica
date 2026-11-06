using System;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using Microsoft.Extensions.Logging;
using P3.Driver.ModBusDriver;
using P3.Driver.ModBusDriver.Slave;
using P3.Driver.ModBusDriverFactory.Attributes;

namespace P3.Driver.ModBusDriverFactory.Slave
{

    public class ModBusSlaveAttribute : DriverBase
    {
        public IModBusSlaveDriver Driver { get; }
        private readonly ModBusSlaveDevice _parent;
        private readonly ModBusAttribute _attribute;

        public ModBusAttribute Attribute => _attribute;

        public ModBusSlaveAttribute(IDriverContext driverContext, ModBusSlaveDevice parent, IModBusSlaveDriver driver, ModBusAttribute attribute) : base(driverContext)
        {
            Driver = driver;
            _parent = parent;
            _attribute = attribute;
        }

        public override Task<bool> Start(CancellationToken token = default)
        {
            Driver.HoldingRegisterChanged -= OnHoldingRegisterChanged;
            Driver.HoldingRegisterChanged += OnHoldingRegisterChanged;
            Driver.CoilRegisterChanged -= OnCoilRegisterChanged;
            Driver.CoilRegisterChanged += OnCoilRegisterChanged;
            return base.Start(token);
        }

        public override Task<bool> Stop(CancellationToken token = default)
        {
            Driver.HoldingRegisterChanged -= OnHoldingRegisterChanged;
            Driver.CoilRegisterChanged -= OnCoilRegisterChanged;
            return base.Stop(token);
        }

        private void OnHoldingRegisterChanged(object sender, RegisterChangedEventArgs e)
        {
            if (_attribute.Table == ModBusTable.HoldingRegister && IsAffected(e))
            {
                DispatchBusValue(() => Driver.GetHoldingRegisters(_parent.DeviceId, _attribute.Register, (ushort)_attribute.RegisterLength).ToArray());
            }
        }

        private void OnCoilRegisterChanged(object sender, RegisterChangedEventArgs e)
        {
            if (_attribute.Table == ModBusTable.Coil && IsAffected(e))
            {
                DispatchBusValue(() => new[] { Driver.GetCoil(_parent.DeviceId, _attribute.Register) ? (ushort)1 : (ushort)0 });
            }
        }

        /// <summary>
        /// true if a master wrote to (a part of) the registers of this attribute
        /// </summary>
        private bool IsAffected(RegisterChangedEventArgs e)
        {
            if (e.DeviceId != _parent.DeviceId)
            {
                return false;
            }

            var start = _attribute.Register;
            var end = start + _attribute.RegisterLength; // exclusive
            var changedEnd = e.StartIndex + Math.Max((int)e.Length, 1);

            return e.StartIndex < end && changedEnd > start;
        }

        private void DispatchBusValue(Func<ushort[]> readRegisters)
        {
            try
            {
                var value = _attribute.ConvertValueFromBus(readRegisters());
                DriverContext.Logger.LogInformation(
                    $"Master wrote {value} to {_parent.Name + $"(-{_parent.DeviceId}-)" + Name} (Register: {_attribute.Register}, Length: {_attribute.RegisterLength}, Table: {_attribute.Table})");
                DispatchRead(value);
            }
            catch (Exception ex)
            {
                DriverContext.Logger.LogError(ex, $"Could not dispatch value written by the master to {_attribute.Register} on {_attribute.Table} {ex}");
            }
        }

        protected override async Task Write(object value, IWriteContext writeContext, CancellationToken token = new CancellationToken())
        {
            await Task.CompletedTask;
            var shortValue = _attribute.ConvertValueToBus( value, out var convertedValue);

            DriverContext.Logger.LogInformation(
                $"Get value ({value} - {String.Join("-", shortValue)}) {_parent.Name + $"(-{_parent.DeviceId}-)" + Name} (Register: {_attribute.Register}, Length: {_attribute.RegisterLength}, Table: {_attribute.Table})");
            switch (_attribute.Table)
            {
                case ModBusTable.Coil:
                    Driver.SetCoil(_parent.DeviceId, _attribute.Register, shortValue[0] == 1);
                    return;
                case ModBusTable.DiscreteInput:
                    Driver.SetDiscreteInput(_parent.DeviceId, _attribute.Register, shortValue[0] == 1);
                    return;
            }

            for (int i = 0; i < _attribute.RegisterLength; i++)
            {
                var registerAddress = (ushort)(_attribute.Register + i);
                switch (_attribute.Table)
                {
                    case ModBusTable.HoldingRegister:
                        Driver.SetHoldingRegister(_parent.DeviceId, registerAddress, shortValue[i]);
                        break;
                    case ModBusTable.InputRegister:
                        Driver.SetInputRegister(_parent.DeviceId, registerAddress, shortValue[i]);
                        break;
                }
            }

            // dispatch the value the bus now holds (rounded/scaled back with the read formula), not the raw register value
            await writeContext.DispatchValue(_attribute.ConvertValueFromBus(shortValue), token);
        }

        protected override Task<bool> Read(IReadContext readContext, CancellationToken token = new CancellationToken())
        {
            return Task.FromResult(false);
        }


        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            return null;
        }
    }
}
