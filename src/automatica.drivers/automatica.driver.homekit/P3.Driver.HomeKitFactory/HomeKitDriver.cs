using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using Automatica.Core.EF.Models;
using Microsoft.Extensions.Logging;
using P3.Driver.HomeKit;
using P3.Driver.HomeKit.Hap;
using P3.Driver.HomeKit.Hap.EventArgs;
using P3.Driver.HomeKit.Hap.Model;
using P3.Driver.HomeKitFactory.NodeInstances;
using P3.Driver.HomeKitFactory.NodeInstances.Nodes;

namespace P3.Driver.HomeKitFactory
{
    public class HomeKitDriver : DriverBase
    {
        private HomeKitServer _server;
        private PropertyInstance _ltskProperty;
        private PropertyInstance _ltpkProperty;

        private PairingKeyNode _pairingNode;

        private readonly List<Accessory> _accessories = new List<Accessory>();
        private readonly Dictionary<Characteristic, List<BaseNode>> _characteristicNodeMap = new Dictionary<Characteristic, List<BaseNode>>();

        private readonly AccessoryInstanceIdGenerator _aidGenerator;

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

        public override async Task<bool> Start()
        {
            _ltpkProperty = GetProperty("ltpk-private");
            _ltskProperty = GetProperty("ltsk-private");

            var objId = DriverContext.NodeInstance.ObjId.ToString().Replace("-", "");

            var homekitId =
                $"{objId[0]}{objId[1]}:{objId[2]}{objId[3]}:{objId[4]}{objId[5]}:{objId[6]}{objId[7]}:{objId[8]}{objId[9]}:{objId[10]}{objId[11]}";

            string code = $"{CreateRandom(100, 999)}-{CreateRandom(10, 99)}-{CreateRandom(100, 999)}";
          
            var configProperty = GetPropertyValueInt("config-version");

            if(configProperty <= 0)
            {
                configProperty = 1;
            }

            DriverContext.NodeTemplateFactory.SetPropertyValue(GetProperty("config-version").ObjId, configProperty + 1);

            DriverContext.Logger.LogDebug($"Start homekit server with LTSK {_ltskProperty.ValueSlave} and LTPK {_ltpkProperty.ValueSlave}");
            _server = new HomeKitServer(DriverContext.Logger, GetPropertyValueInt("port"), DriverContext.NodeInstance.Name, _ltskProperty.ValueString, _ltpkProperty.ValueString, homekitId,
                code, "AutomaticaCore", "AutomaticaCore" + homekitId, configProperty);

            _pairingNode.DispatchValue(code);

            foreach (var accessory in _accessories)
            {
               accessory.Id = _server.AddAccessory(accessory);
            }

            await _server.Start();

            _server.PairingCompleted += ServerOnPairingCompleted;
            _server.ValueChanged += ServerOnValueChanged;

            return await base.Start();
        }

        private void ServerOnValueChanged(object sender, CharactersiticValueChangedEventArgs e)
        {
            if (_characteristicNodeMap.ContainsKey(e.Characteristic))
            {
                _characteristicNodeMap[e.Characteristic].ForEach(a => a.SetValue(e.Value));
            }
        }

        private int CreateRandom(int from, int to)
        {
            Random randomNumber = new Random();
            return randomNumber.Next(from, to);
        }

        private void ServerOnPairingCompleted(object sender, PairSetupCompleteEventArgs e)
        {
            _ltskProperty.Value = e.Ltsk;
            _ltpkProperty.Value = e.Ltpk;

            DriverContext.NodeTemplateFactory.SetPropertyValue(_ltskProperty.ObjId, e.Ltsk);  //save to database
            DriverContext.NodeTemplateFactory.SetPropertyValue(_ltpkProperty.ObjId, e.Ltpk); //save to database
        }

        public override async Task<bool> Stop()
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
            var aid = Convert.ToInt32(ctx.NodeInstance.GetPropertyValueDouble(HomeKitFactory.AidPropertyKey));

            switch (ctx.NodeInstance.This2NodeTemplateNavigation.Key)
            {
                case "light-bulb-folder":
                    accessory = AccessoryFactory.CreateLightBulbAccessory(aid, ctx.NodeInstance.Name, "AutomaticaCore",
                        ctx.NodeInstance.ObjId.ToString(), false);
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
