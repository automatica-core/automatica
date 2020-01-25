using System.Linq;
using Automatica.Core.Driver;
using Microsoft.Extensions.Logging;
using P3.Driver.HomeKit.Hap.Model;
using P3.Driver.HomeKitFactory.NodeInstances.Nodes;

namespace P3.Driver.HomeKitFactory.NodeInstances
{
    public class FolderNodeInstance : DriverBase
    {
        public HomeKitDriver Driver { get; }
        public Accessory Accessory { get; }

        public FolderNodeInstance(IDriverContext driverContext, HomeKitDriver driver, Accessory accessory) : base(
            driverContext)
        {
            Driver = driver;
            Accessory = accessory;

            driverContext.Logger.LogDebug($"Create accessory with aid {accessory.Id}");

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
