﻿using Automatica.Core.Driver;
using P3.Driver.HueBridge.Data;
using P3.Driver.HueBridge.EventArgs;
using P3.Driver.HueBridgeSimulatorDriverFactory;

namespace P3.Driver.HueBridgeSimulator.DriverFactory.OnOff
{
    public class HueOnOffNode : HueBridgeNode
    {
        private readonly int _lightNumber;
        private HueOnOffSwitchNode _switchNode;
        private HueOnOffStateNode _stateNode;

        public HueOnOffNode(IDriverContext driverContext, HueBridgeDriver driver) : base(driverContext, driver)
        {
            _lightNumber = driver.AddHueLight(new HueLight(driverContext.NodeInstance.Name, driverContext.NodeInstance.ObjId.ToString()), this);
        }

        internal int LightNumber => _lightNumber;

        public override void SwitchLight(HueSwitchLightEventArgs eventArgs)
        {
            _switchNode?.DispatchValue(eventArgs.State);
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            if(ctx.NodeInstance.This2NodeTemplateNavigation.Key == "hue-onoff-state")
            {
                _stateNode = new HueOnOffStateNode(ctx, Driver);
                return _stateNode;
            }
            else if (ctx.NodeInstance.This2NodeTemplateNavigation.Key == "hue-onoff-switch")
            {
                _switchNode = new HueOnOffSwitchNode(ctx, Driver);
                return _switchNode;
            }
            return null;
        }
    }
}
