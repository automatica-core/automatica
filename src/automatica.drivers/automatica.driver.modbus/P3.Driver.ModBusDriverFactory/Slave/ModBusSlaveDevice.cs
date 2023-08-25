using System;
using Automatica.Core.Driver;
using P3.Driver.ModBusDriver;
using P3.Driver.ModBusDriver.Slave;
using P3.Driver.ModBusDriverFactory.Attributes;

namespace P3.Driver.ModBusDriverFactory.Slave
{
    public class ModBusSlaveDevice : DriverNoneAttributeBase
    {
        private readonly IModBusSlaveDriver _driver;
        public byte DeviceId { get; set; }

        public ModBusSlaveDevice(IDriverContext driverContext, IModBusSlaveDriver driver) : base(driverContext)
        {
            _driver = driver;

            DeviceId = (byte)GetProperty("modbus-device-id").ValueInt;
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            Type t = null;
            switch (ctx.NodeInstance.This2NodeTemplateNavigation.Key)
            {
                case "modbus-+2byte":
                case "modbus-2byte":
                    t = typeof(ModBus2ByteInteger);
                    break;
                case "modbus-+4byte":
                case "modbus-4byte":
                    t = typeof(ModBus4ByteInteger);
                    break;
                case "modbus-+8byte":
                case "modbus-8byte":
                    t = typeof(ModBus8ByteInteger);
                    break;
                case "modbus-4float":
                    t = typeof(ModBus4ByteFloat);
                    break;
                case "modbus-8float":
                    t = typeof(ModBus8ByteFloat);
                    break;
                case "modbus-binary":
                    t = typeof(ModBusBinaryAttribute);
                    break;

            }

            if (t != null)
            {
                var attribute = Activator.CreateInstance(t, ctx) as ModBusAttribute;
                var driverNode = new ModBusSlaveAttribute(ctx, this, _driver, attribute);

                switch (attribute.Table)
                {
                    case ModBusTable.HoldingRegister:
                       InitRegister((x) =>
                       {
                           _driver.InitHoldingRegister(DeviceId, x, 0);
                       }, driverNode);
                        break;
                    case ModBusTable.InputRegister:
                        InitRegister((x) =>
                        {
                            _driver.InitInputRegister(DeviceId, x, 0);
                        }, driverNode);
                        break;
                    case ModBusTable.Coil:
                        InitRegister((x) =>
                        {
                            _driver.InitCoil(DeviceId, x, false);
                        }, driverNode);
                        break;
                    case ModBusTable.DiscreteInput:
                        InitRegister((x) =>
                        {
                            _driver.InitDiscreteInput(DeviceId, x, false);
                        }, driverNode);
                        break;
                }
                return driverNode;
            } 
            throw new NotImplementedException();
        }

        private void InitRegister(Action<ushort> callback, ModBusSlaveAttribute att)
        {
            for (int i = 0; i < att.Attribute.RegisterLength; i++)
            {
                var registerAddres = (ushort)(att.Attribute.Register + i);
                callback(registerAddres);
            }
        }
    }
}
