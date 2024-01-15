using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Control.Base;
using Automatica.Core.Driver;
using Automatica.Core.EF.Models;
using Automatica.Core.Model;
using Microsoft.Extensions.Logging;
using P3.Driver.HomeKit;
using P3.Driver.HomeKit.Hap;
using P3.Driver.HomeKit.Hap.EventArgs;
using P3.Driver.HomeKit.Hap.Model;
using P3.Driver.HomeKitFactory.NodeInstances;
using P3.Driver.HomeKitFactory.NodeInstances.Nodes;

namespace P3.Driver.HomeKitFactory
{
    public class HomeKitDriver : DriverNoneAttributeBase
    {
        private HomeKitServer _server;
        private PropertyInstance _ltskProperty;
        private PropertyInstance _ltpkProperty;
        private PropertyInstance _pairCodeProperty;

        private PairingKeyNode _pairingNode;

        private readonly List<Accessory> _accessories = new List<Accessory>();
        private readonly Dictionary<Characteristic, List<BaseNode>> _characteristicNodeMap = new();
        private readonly Dictionary<Accessory, IControl> _characteristicControlMap = new();

        private readonly AccessoryInstanceIdGenerator _aidGenerator;
        
        private readonly List<IControl> _controls = new();

        public HomeKitDriver(IDriverContext driverContext) : base(driverContext)
        {
            var highestAid = 1;

            foreach (var child in driverContext.NodeInstance.InverseThis2ParentNodeInstanceNavigation)
            {
                if (child.This2NodeTemplateNavigation.Key == "pairing-key")
                {
                    continue;
                }

                var aidProperty = child.GetProperty(HomeKitFactory.AidPropertyKey);

                if (!aidProperty.ValueDouble.HasValue)
                {
                    continue;
                }

                highestAid = Math.Max(highestAid, Convert.ToInt32(aidProperty.ValueDouble.Value));
            }

            _aidGenerator = new AccessoryInstanceIdGenerator(highestAid);

            driverContext.Logger.LogDebug($"Highest generated AID is {highestAid}");

            InitializeAidProperties(driverContext);
        }

        private void InitializeAidProperties(IDriverContext driverContext)
        {
            foreach (var child in driverContext.NodeInstance.InverseThis2ParentNodeInstanceNavigation)
            {
                if (child.This2NodeTemplateNavigation.Key == "pairing-key")
                {
                    continue;
                }

                var aidProperty = child.GetProperty(HomeKitFactory.AidPropertyKey);

                if (!aidProperty.ValueDouble.HasValue)
                {
                    aidProperty.ValueDouble = _aidGenerator.GetNextAidInstance();
                    driverContext.NodeTemplateFactory.SetPropertyValue(aidProperty.ObjId, aidProperty.ValueDouble);

                    driverContext.Logger.LogDebug($"Set aid {aidProperty.ValueDouble} for {Name} {driverContext.NodeInstance.ObjId}");
                }
            }

        }

        public override async Task<bool> Init(CancellationToken token = new CancellationToken())
        {
           
            return await base.Init(token);
        }

        public override async Task<bool> Start(CancellationToken token = default)
        {
            _ltpkProperty = GetProperty("ltpk-private");
            _ltskProperty = GetProperty("ltsk-private");
            _pairCodeProperty = GetProperty("pair-code");

            var objId = DriverContext.NodeInstance.ObjId.ToString().Replace("-", "");

            var homekitId =
                $"{objId[0]}{objId[1]}:{objId[2]}{objId[3]}:{objId[4]}{objId[5]}:{objId[6]}{objId[7]}:{objId[8]}{objId[9]}:{objId[10]}{objId[11]}";

            var pairCode = "";

            if (String.IsNullOrEmpty(_pairCodeProperty.ValueString))
            {
                string code = $"{CreateRandom(100, 999)}-{CreateRandom(10, 99)}-{CreateRandom(100, 999)}";
                DriverContext.NodeTemplateFactory.SetPropertyValue(_pairCodeProperty.ObjId, code); //save to database
                pairCode = code;
            }
            else
            {
                pairCode = _pairCodeProperty.ValueString;
            }



            var configProperty = GetPropertyValueInt("config-version");

            if (configProperty <= 0)
            {
                configProperty = 1;
            }

            configProperty += 1;

            DriverContext.NodeTemplateFactory.SetPropertyValue(GetProperty("config-version").ObjId, configProperty);

            DriverContext.Logger.LogDebug(
                $"Start homekit server with LTSK {_ltskProperty.Value} and LTPK {_ltpkProperty.Value}");
            _server = new HomeKitServer(DriverContext.Logger, GetPropertyValueInt("port"),
                DriverContext.NodeInstance.Name, _ltskProperty.ValueString, _ltpkProperty.ValueString, homekitId,
                pairCode, "AutomaticaCore", "AutomaticaCore" + homekitId, configProperty, "0.0.1");

            _pairingNode.Code = pairCode;
            _pairingNode.DispatchRead(pairCode);

            foreach (var accessory in _accessories)
            {
                accessory.Id = _server.AddAccessory(accessory);
            }

            _server.PairingCompleted += ServerOnPairingCompleted;
            _server.ValueChanged += ServerOnValueChanged;

            await _server.Start();

            var controlsProperty = GetProperty("controls");
            if (controlsProperty.Value is ControlConfiguration controlConfig)
            {
                foreach (var control in controlConfig.Controls)
                {
                    _controls.Add(await DriverContext.ControlContext.GetAsync(control, token));
                }
            }

            foreach (var control in _controls)
            {
                if (control == null)
                {
                    continue;
                }

                var aid = GuidToUint64(control.Id);
                if (control is IDimmer dimmer)
                {
                    var accessory = AccessoryFactory.CreateLightBulbAccessory(aid, control.Name, "AutomaticaCore",
                        control.Id.ToString(), false, 0);

                    accessory.Id = _server.AddAccessory(accessory);
                    _characteristicControlMap.Add(accessory, control);
                    var characteristic = accessory.Specific.Characteristics.First();
                    var dimValueChar = accessory.Specific.Characteristics.Last();

                    dimmer.RegisterValueCallback(() =>
                    {
                        characteristic.Value = dimmer.DimmerState;
                        dimValueChar.Value = dimmer.DimmerValue;

                        WriteCharacteristic(dimValueChar);
                        WriteCharacteristic(characteristic);
                    });

                }
                else if (control is ISwitch iSwitch)
                {
                    var accessory = AccessoryFactory.CreateSwitchAccessory(aid, control.Name, "AutomaticaCore",
                        control.Id.ToString(), false);

                    accessory.Id = _server.AddAccessory(accessory);
                    var characteristic = accessory.Specific.Characteristics.First();

                    _characteristicControlMap.Add(accessory, control);

                    iSwitch.RegisterValueCallback(() =>
                    {
                        characteristic.Value = iSwitch.State == SwitchState.On;
                        WriteCharacteristic(characteristic);
                    });
                }
                else if (control is IBlind iBlind)
                {
                    var accessory = AccessoryFactory.CreateWindowCovering(aid, control.Name, "AutomaticaCore",
                        control.Id.ToString(), 0);
                    
                    accessory.Id = _server.AddAccessory(accessory);
                    var characteristic = accessory.Specific.Characteristics.First();

                    _characteristicControlMap.Add(accessory, control);
                    iBlind.RegisterValueCallback(() =>
                    {
                        accessory.CurrentPosition.Value = iBlind.Position;
                        WriteCharacteristic(characteristic);
                    });
                }
            }

            return await base.Start(token);
        }

        public UInt64 GuidToUint64(Guid guid)
        {
            var val= BitConverter.ToUInt64(guid.ToByteArray(), 0);
            return val/2;
        }

        public override Task<IList<NodeInstance>> CustomAction(string actionName, CancellationToken token = default)
        {
            if (actionName == HomeKitFactory.ClearPairingsKey)
            {
                DriverContext.Logger.LogInformation($"Clear pairings...");
                DriverContext.NodeTemplateFactory.SetPropertyValue(_ltskProperty.ObjId, String.Empty);  //save to database
                DriverContext.NodeTemplateFactory.SetPropertyValue(_ltpkProperty.ObjId, String.Empty); //save to database

                _ltskProperty.Value = String.Empty;
                _ltpkProperty.Value = String.Empty;
                DriverContext.Logger.LogInformation($"Clear pairings...done");
            }
            return base.CustomAction(actionName, token);
        }

        private async void ServerOnValueChanged(object sender, CharactersiticValueChangedEventArgs e)
        {
            DriverContext.Logger.LogDebug($"Value changed for {e.Characteristic.Id} to {e.Value}");
            if (_characteristicNodeMap.TryGetValue(e.Characteristic, out var value))
            {
                value.ForEach(a => a.SetValue(e.Value));
            }
            else if (_characteristicControlMap.TryGetValue(e.Characteristic.Accessory, out var control))
            {
                DriverContext.Logger.LogDebug($"Target is a control {control.Name}");
                if (control is IDimmer iDimmer)
                {
                    if (e.Value is bool)
                    {
                        await Switch(e.Value, iDimmer);
                    }
                    else if(e.Value is long or int)
                    {
                        DriverContext.Logger.LogDebug($"Dim value to {e.Value}");
                        await iDimmer.DimAsync(Convert.ToInt32(e.Value));
                    }
                }
                else if (control is ISwitch iSwitch)
                {
                    await Switch(e.Value, iSwitch);
                }
                else if (control is IBlind iBlind)
                {
                    var targetBlind = Convert.ToInt32(e.Value); //homekit percentage is reversed, this means 100% = fully open and 0% = fully closed
                    var newTarget = Math.Abs(targetBlind - 100);
                    await iBlind.MoveAbsoluteAsync(newTarget);
                }
            }
        }

        private async Task Switch(object value, ISwitch control)
        {
            DriverContext.Logger.LogDebug($"Switch {control.Name} to {value}");
            if (value is bool bValue)
            {
                await control.SwitchAsync(bValue);
            }
            else if (value is int intValue)
            {
                await control.SwitchAsync(intValue == 1);
            }
            else if (value is double dValue)
            {
                await control.SwitchAsync(dValue == 1);
            }
            else if (value is long lValue)
            {
                await control.SwitchAsync(lValue == 1);
            }
        }

        private int CreateRandom(int from, int to)
        {
            Random randomNumber = new Random();
            return randomNumber.Next(from, to);
        }

        private void ServerOnPairingCompleted(object sender, PairSetupCompleteEventArgs e)
        {
            DriverContext.Logger.LogInformation($"Saving LTSK {e.Ltsk} and LTPK {e.Ltpk} to database...");

            _ltskProperty.Value = e.Ltsk;
            _ltpkProperty.Value = e.Ltpk;

            DriverContext.NodeTemplateFactory.SetPropertyValue(_ltskProperty.ObjId, e.Ltsk);  //save to database
            DriverContext.NodeTemplateFactory.SetPropertyValue(_ltpkProperty.ObjId, e.Ltpk); //save to database

            DriverContext.Logger.LogInformation($"Saving LTSK {e.Ltsk} and LTPK {e.Ltpk} to database...done");
        }

        public override async Task<bool> Stop(CancellationToken token = default)
        {
            if (_server != null)
            {
                _server.PairingCompleted -= ServerOnPairingCompleted;
                await _server.Stop();
            }


            return true;
        }

        internal void RegisterCharacteristic(Accessory accessory, Characteristic c, BaseNode node)
        {
            if (!_characteristicNodeMap.ContainsKey(c))
            {
                _characteristicNodeMap.Add(c, new List<BaseNode>());
            }
            _characteristicNodeMap[c].Add(node);
        }

        internal void WriteCharacteristic(Characteristic c)
        {
            _server.SetCharacteristicValue(c, c.Value);
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {

            if (ctx.NodeInstance.This2NodeTemplateNavigation.Key == "pairing-key")
            {
                _pairingNode = new PairingKeyNode(ctx);

                return _pairingNode;
            }

            Accessory accessory = null;
            var aid = Convert.ToUInt64(ctx.NodeInstance.GetPropertyValueDouble(HomeKitFactory.AidPropertyKey));

            switch (ctx.NodeInstance.This2NodeTemplateNavigation.Key)
            {
                case "light-bulb-folder":
                    accessory = AccessoryFactory.CreateLightBulbAccessory(aid, ctx.NodeInstance.Name, "AutomaticaCore",
                        ctx.NodeInstance.ObjId.ToString(), false, 0);
                    break;
                case "power-outlet-folder":
                    accessory = AccessoryFactory.CreateOutletAccessory(aid, ctx.NodeInstance.Name, "AutomaticaCore",
                        ctx.NodeInstance.ObjId.ToString(), false);
                    break;
                case "contact-sensor-folder":
                    accessory = AccessoryFactory.CreateContactSensorAccessory(aid, ctx.NodeInstance.Name, "AutomaticaCore",
                        ctx.NodeInstance.ObjId.ToString(), 1);
                    break;
                case "switch-folder":
                    accessory = AccessoryFactory.CreateSwitchAccessory(aid, ctx.NodeInstance.Name, "AutomaticaCore",
                        ctx.NodeInstance.ObjId.ToString(), false);
                    break;
                case "temperature-sensor-folder":
                    accessory = AccessoryFactory.CreateTemperatureSensorAccessory(aid, ctx.NodeInstance.Name, "AutomaticaCore",
                        ctx.NodeInstance.ObjId.ToString(), 0);
                    break;
            }

            if (accessory == null)
            {
                return null;
            }

            _accessories.Add(accessory);

            return new FolderNodeInstance(ctx, this, accessory);
        }
    }
}
