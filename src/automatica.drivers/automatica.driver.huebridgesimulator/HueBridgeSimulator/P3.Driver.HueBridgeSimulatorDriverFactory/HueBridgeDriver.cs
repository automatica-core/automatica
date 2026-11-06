using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using Automatica.Core.EF.Models;
using Microsoft.Extensions.Logging;
using P3.Driver.HueBridge;
using P3.Driver.HueBridge.Data;
using P3.Driver.HueBridge.EventArgs;
using P3.Driver.HueBridgeSimulator.DriverFactory;
using P3.Driver.HueBridgeSimulator.DriverFactory.OnOff;

namespace P3.Driver.HueBridgeSimulatorDriverFactory
{
    public class HueBridgeDriver : DriverNoneAttributeBase
    {
        private readonly IDictionary<int, HueBridgeNode> _hueBridgeMapping = new Dictionary<int, HueBridgeNode>();
        private readonly HueDriver _driver;

        public HueBridgeDriver(IDriverContext driverContext) : base(driverContext)
        {
            var bindAddressValue = driverContext.NodeInstance.GetPropertyValueString(HueBridgeSimulator.DriverFactory.HueBridgeSimulatorDriverFactory.BindAddressKey);
            if (string.IsNullOrWhiteSpace(bindAddressValue) || !IPAddress.TryParse(bindAddressValue, out var bindAddress))
            {
                bindAddress = IPAddress.Any;
            }

            var port = HueDriver.DefaultPort;
            var portValue = driverContext.NodeInstance.GetPropertyValueInt(HueBridgeSimulator.DriverFactory.HueBridgeSimulatorDriverFactory.PortKey);
            if (portValue > 0 && portValue <= ushort.MaxValue)
            {
                port = portValue;
            }

            _driver = new HueDriver(DriverContext.Logger, bindAddress, port);

            var users = driverContext.NodeInstance.GetPropertyValueString(HueBridgeSimulator.DriverFactory.HueBridgeSimulatorDriverFactory.UsersKey);
            if (!string.IsNullOrEmpty(users))
            {
                _driver.LoadUsers(users.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
            }
        }

        private void HueDriverSwitchLight(object sender, HueSwitchLightEventArgs e)
        {
            if(_hueBridgeMapping.TryGetValue(e.Light.Id, out var value))
            {
                Task.Factory.StartNew(() => value.SwitchLight(e));
            }
        }

        private void DriverWhiteListChanged(object sender, EventArgs e)
        {
            try
            {
                var property = GetProperty(HueBridgeSimulator.DriverFactory.HueBridgeSimulatorDriverFactory.UsersKey);
                if (property == null)
                {
                    return;
                }

                var value = string.Join(",", _driver.GetUserNames());
                property.Value = value;
                DriverContext.NodeTemplateFactory.SetPropertyValue(property.ObjId, value);
            }
            catch (Exception ex)
            {
                DriverContext.Logger.LogError(ex, "Could not save the hue users");
            }
        }

        internal void SetLightState(int light, HueSwitchLightData state)
        {
            _driver.SetLightState(light, state, false);
        }

        internal int AddHueLight(HueLight light, HueBridgeNode node)
        {
            var lightNr = _driver.AddLight(light);
            _hueBridgeMapping.Add(light.Id, node);
            return lightNr;
        }

        public override Task<IList<NodeInstance>> CustomAction(string actionName, CancellationToken token = default)
        {
            if (actionName == HueBridgeSimulator.DriverFactory.HueBridgeSimulatorDriverFactory.LinkButtonKey)
            {
                _driver.PressLinkButton();
            }
            else if (actionName == HueBridgeSimulator.DriverFactory.HueBridgeSimulatorDriverFactory.RemoveUsersKey)
            {
                DriverContext.Logger.LogInformation("Removing all hue users");
                _driver.RemoveAllUsers();
            }

            return base.CustomAction(actionName, token);
        }

        public override async Task<bool> Start(CancellationToken token = default)
        {
            _driver.SwitchLight += HueDriverSwitchLight;
            _driver.WhiteListChanged += DriverWhiteListChanged;

            await Task.Factory.StartNew(async () =>
            {
                await _driver.Start();
            }, token);

            DriverContext.Logger.LogInformation($"Starting hue bridge web services on {_driver.BindAddress}:{_driver.Port}");

            return await base.Start(token);
        }

        public override async Task<bool> Stop(CancellationToken token = default)
        {
            _driver.SwitchLight -= HueDriverSwitchLight;
            _driver.WhiteListChanged -= DriverWhiteListChanged;

            await _driver.Stop();
            return await base.Stop(token);
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            if(ctx.NodeInstance.This2NodeTemplate == HueBridgeSimulator.DriverFactory.HueBridgeSimulatorDriverFactory.OnOffLight)
            {
                return new HueOnOffNode(ctx, this);
            }
            return null;
        }
    }
}
