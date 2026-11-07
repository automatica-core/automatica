using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
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
using P3.Driver.HomeKitFactory.Accessories;
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
                var code = HomeKitServer.GenerateSetupCode();
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
                $"Start homekit server (LTSK {(String.IsNullOrEmpty(_ltskProperty.ValueString) ? "not set" : "loaded")}, pairings {(String.IsNullOrEmpty(_ltpkProperty.ValueString) ? "not set" : "loaded")})");
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

                    characteristic.Value = dimmer.DimmerState;
                    dimValueChar.Value = dimmer.DimmerValue;
                    
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
                    var accessory = CreateSwitchControlAccessory(aid, control);

                    accessory.Id = _server.AddAccessory(accessory);
                    var characteristic = accessory.Specific.Characteristics.First();

                    _characteristicControlMap.Add(accessory, control);
                    characteristic.Value = iSwitch.State == SwitchState.On;

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

                    _characteristicControlMap.Add(accessory, control);
                    BindBlind(accessory, iBlind, WriteCharacteristic, DriverContext.Logger);
                }
            }

            foreach (var accessory in _server.AccessoryContainer.Accessories)
            {
                DriverContext.Logger.LogInformation($"Accessory: {accessory.Id} is {accessory.AccessoryInfo.Name.Value}");

                foreach (var service in accessory.Services)
                {
                    DriverContext.Logger.LogInformation($"Service: {service.Id} has type {service.Type}");
                }
            }

            return await base.Start(token);
        }

        private Accessory CreateSwitchControlAccessory(ulong aid, IControl control)
        {
            var switchType = (HomeKitSwitchControlType)(GetProperty(HomeKitFactory.SwitchControlTypePropertyKey)?.ValueInt ?? 0);

            switch (switchType)
            {
                case HomeKitSwitchControlType.Outlet:
                    return AccessoryFactory.CreateServiceAccessory(aid, control.Name, "AutomaticaCore", control.Id.ToString(), AccessoryFactory.OutletType,
                        HapCharacteristics.On, HapCharacteristics.OutletInUse);
                case HomeKitSwitchControlType.Light:
                    return AccessoryFactory.CreateServiceAccessory(aid, control.Name, "AutomaticaCore", control.Id.ToString(), AccessoryFactory.LightBulbType,
                        HapCharacteristics.On);
                case HomeKitSwitchControlType.Fan:
                    return AccessoryFactory.CreateServiceAccessory(aid, control.Name, "AutomaticaCore", control.Id.ToString(), AccessoryFactory.FanType,
                        HapCharacteristics.On);
                default:
                    return AccessoryFactory.CreateSwitchAccessory(aid, control.Name, "AutomaticaCore", control.Id.ToString(), false);
            }
        }

        /// <summary>
        /// Initializes the window covering from the blind and notifies HomeKit about position and movement changes of the blind.
        /// </summary>
        internal static void BindBlind(WindowCovering accessory, IBlind blind, Action<Characteristic> writeCharacteristic, ILogger logger)
        {
            accessory.PositionType.Value = ToHapPositionState(blind);
            accessory.TargetPosition.Value = ToHapPosition(blind.Position);
            accessory.CurrentPosition.Value = ToHapPosition(blind.Position);

            var wasMoving = blind.IsMoving;
            blind.RegisterValueCallback(() =>
            {
                accessory.CurrentPosition.Value = ToHapPosition(blind.Position);
                accessory.PositionType.Value = ToHapPositionState(blind);

                // IBlind has no target: when a movement ends (also one started outside of HomeKit, e.g. by a wall switch) the
                // target is where the blind stopped, otherwise HomeKit keeps showing the blind as opening/closing.
                // Only on the moving => stopped transition, a HomeKit target must not be overwritten before the blind starts moving.
                var stopped = wasMoving && !blind.IsMoving;
                wasMoving = blind.IsMoving;

                logger.LogInformation($"{blind.Name} Blind...moving {blind.IsMoving} direction {blind.Direction} position {blind.Position} ({accessory.CurrentPosition.Value})");
                logger.LogInformation($"{blind.Name} Updating blind...current pos {accessory.CurrentPosition.Value} and position state {accessory.PositionType.Value}");

                writeCharacteristic(accessory.CurrentPosition);
                writeCharacteristic(accessory.PositionType);

                if (stopped)
                {
                    accessory.TargetPosition.Value = accessory.CurrentPosition.Value;
                    writeCharacteristic(accessory.TargetPosition);
                }
            });
        }

        private static int ToHapPosition(int input)
        {
            var currentBlind = Convert.ToInt32(input); //homekit percentage is reversed, this means 100% = fully open and 0% = fully closed
            var hapCurrentBlind = Math.Abs(currentBlind - 100);
            return hapCurrentBlind;
        }

        private static int ToHapPositionState(IBlind iBlind)
        {
            int value = 0;
            if (iBlind.IsMoving)
            {
                value = iBlind.Direction == 0 ? 1 : 0;
            }
            else
            {
                value = 2; //stopped
            }

            return value;
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
                // the running server keeps the pairings in memory, it would still verify the old controllers and reject a new pair-setup;
                // this also closes all controller connections and resets the failed pair-setup counter (lockout)
                _server?.ClearPairings();
                ClearPersistedPairing();
                DriverContext.Logger.LogInformation($"Clear pairings...done");
            }
            return base.CustomAction(actionName, token);
        }

        private void ClearPersistedPairing()
        {
            DriverContext.NodeTemplateFactory.SetPropertyValue(_ltskProperty.ObjId, String.Empty);  //save to database
            DriverContext.NodeTemplateFactory.SetPropertyValue(_ltpkProperty.ObjId, String.Empty); //save to database

            _ltskProperty.Value = String.Empty;
            _ltpkProperty.Value = String.Empty;
        }

        private async void ServerOnValueChanged(object sender, CharactersiticValueChangedEventArgs e)
        {
            try
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
                        // decide by the characteristic, HomeKit may send "On" as 0/1 and brightness as any number type
                        if (e.Characteristic.Type == CharacteristicBase.OnType)
                        {
                            await Switch(e.Value, iDimmer);
                        }
                        else if (e.Characteristic.Type == CharacteristicBase.BrightnessType)
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
            catch (Exception ex)
            {
                DriverContext.Logger.LogError(ex, $"Error handling homekit value change {ex}");
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

        /// <summary>
        /// Raised by the pairing store on every change: pair-setup, add/remove pairing by a controller (removing the last admin
        /// clears all pairings and the accessory LTSK, e.g. when the bridge is removed in the Home app), failed pair-setup attempts
        /// and clear. <see cref="PairSetupCompleteEventArgs.Ltpk"/> is the serialized controller list.
        /// </summary>
        private void ServerOnPairingCompleted(object sender, PairSetupCompleteEventArgs e)
        {
            try
            {
                DriverContext.Logger.LogInformation($"Saving HomeKit pairings to database...");

                _ltskProperty.Value = e.Ltsk;
                _ltpkProperty.Value = e.Ltpk;

                DriverContext.NodeTemplateFactory.SetPropertyValue(_ltskProperty.ObjId, e.Ltsk);  //save to database
                DriverContext.NodeTemplateFactory.SetPropertyValue(_ltpkProperty.ObjId, e.Ltpk); //save to database

                DriverContext.Logger.LogInformation($"Saving HomeKit pairings to database...done");
            }
            catch (Exception ex)
            {
                // raised while a HAP request is handled, a failure must not break the request
                DriverContext.Logger.LogError(ex, "Could not save HomeKit pairings");
            }
        }

        public override async Task<bool> Stop(CancellationToken token = default)
        {
            if (_server != null)
            {
                _server.PairingCompleted -= ServerOnPairingCompleted;
                _server.ValueChanged -= ServerOnValueChanged;
                await _server.Stop();
            }


            return await base.Stop(token);
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
            HomeKitAccessoryType accessoryType = null;
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
                default:
                    accessoryType = HomeKitAccessoryCatalog.GetByKey(ctx.NodeInstance.This2NodeTemplateNavigation.Key);
                    accessory = accessoryType?.CreateAccessory(aid, ctx.NodeInstance.Name, "AutomaticaCore", ctx.NodeInstance.ObjId.ToString(), ctx.NodeInstance);
                    break;
            }

            if (accessory == null)
            {
                return null;
            }

            _accessories.Add(accessory);

            return new FolderNodeInstance(ctx, this, accessory, accessoryType);
        }
    }
}
