using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Driver;
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
        private IList<HueLight> _hueLights = new List<HueLight>();
        private readonly IDictionary<int, HueBridgeNode> _hueBridgeMapping = new Dictionary<int, HueBridgeNode>();
        private readonly HueDriver _driver;

        public HueBridgeDriver(IDriverContext driverContext) : base(driverContext)
        {
            _driver = new HueDriver(DriverContext.Logger);
        }

        private void HueDriverSwitchLight(object sender, HueSwitchLightEventArgs e)
        {
            if(_hueBridgeMapping.TryGetValue(e.Light.Id, out var value))
            {
                Task.Factory.StartNew(() => value.SwitchLight(e));
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

        public override async Task<bool> Start(CancellationToken token = default)
        {
            _driver.SwitchLight += HueDriverSwitchLight;

            await Task.Factory.StartNew(async () =>
            {
                await _driver.Start();
            }, token);

            DriverContext.Logger.LogInformation("Starting hue bridge web services");
            
            return await base.Start(token);
        }

        public override async Task<bool> Stop(CancellationToken token = default)
        {
            _driver.SwitchLight -= HueDriverSwitchLight;


            await _driver.Stop();
            return await base.Start(token);
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
