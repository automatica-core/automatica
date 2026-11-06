using System.Linq;
using Automatica.Core.Driver;
using Microsoft.Extensions.Logging;
using P3.Driver.HomeKit.Hap.Model;
using P3.Driver.HomeKitFactory.Accessories;
using P3.Driver.HomeKitFactory.NodeInstances.Nodes;

namespace P3.Driver.HomeKitFactory.NodeInstances
{
    public class FolderNodeInstance : DriverNoneAttributeBase
    {
        public HomeKitDriver Driver { get; }
        public Accessory Accessory { get; }
        internal HomeKitAccessoryType AccessoryType { get; }

        public FolderNodeInstance(IDriverContext driverContext, HomeKitDriver driver, Accessory accessory) : this(driverContext, driver, accessory, null)
        {
        }

        internal FolderNodeInstance(IDriverContext driverContext, HomeKitDriver driver, Accessory accessory, HomeKitAccessoryType accessoryType) : base(
            driverContext)
        {
            Driver = driver;
            Accessory = accessory;
            AccessoryType = accessoryType;

            driverContext.Logger.LogDebug($"Create accessory {Name} with aid {accessory.Id}");

            foreach (var service in accessory.Services)
            {
                driverContext.Logger.LogDebug($"Create service with iid {service.Id} and type '{service.Type}'");

                foreach (var characteristic in service.Characteristics)
                {
                    driverContext.Logger.LogDebug($"Create characteristic with type '{characteristic.Type}'");
                }
            }
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            IDriverNode driverNode = null;

            var dataPoint = AccessoryType?.GetDataPoint(ctx.NodeInstance.This2NodeTemplateNavigation.Key);
            if (dataPoint != null)
            {
                var characteristic = HomeKitAccessoryType.GetCharacteristic(Accessory, dataPoint);
                var definition = dataPoint.Characteristic;

                if (definition.Type == CharacteristicBase.ProgrammableSwitchEventType)
                {
                    return new ProgrammableSwitchEventNode(ctx, characteristic, Driver, definition, dataPoint.DataType);
                }

                return new CharacteristicNode(ctx, characteristic, Driver, definition, dataPoint.DataType);
            }

            switch (ctx.NodeInstance.This2NodeTemplateNavigation.Key)
            {
                case "light-bulb-switch":
                    driverNode = new SwitchNode(ctx,
                        Accessory.Services[1].Characteristics.Single(a => a.Type == CharacteristicBase.OnType), Driver);
                    break;
                case "light-bulb-brightness":
                    var brigthness = Accessory.CreateBrightness();
                    driverNode = new LightSwitch(ctx, Driver, brigthness);
                    break;
                case "light-bulb-hue":
                    var hue = Accessory.CreateHue();
                    driverNode = new LightSwitch(ctx, Driver, hue);
                    break;
                case "power-outlet-switch":
                    driverNode = new SwitchNode(ctx,
                        Accessory.Services[1].Characteristics.Single(a => a.Type == CharacteristicBase.OnType), Driver);
                    break;
                case "contact-sensor":
                    driverNode = new BaseNode(ctx,
                        Accessory.Services[1].Characteristics
                            .Single(a => a.Type == CharacteristicBase.ContactSensorStateType), Driver);
                    break;
                case "switch":
                    driverNode = new SwitchNode(ctx,
                        Accessory.Services[1].Characteristics.Single(a => a.Type == CharacteristicBase.OnType), Driver);
                    break;
                case "temperature-sensor":
                    driverNode = new BaseNode(ctx,
                        Accessory.Services[1].Characteristics
                            .Single(a => a.Type == CharacteristicBase.CurrentTemperatureType), Driver);
                    break;
                case "switch-status":
                case "power-outlet-status":
                case "light-bulb-status":
                    driverNode = new StateNode(ctx,
                        Accessory.Services[1].Characteristics.Single(a => a.Type == CharacteristicBase.OnType), Driver);
                    break;
            }

            return driverNode;
        }
    }
}
