using System.Collections.Generic;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using Microsoft.Extensions.Logging;
using P3.Driver.HueBridge;
using P3.Driver.HueBridge.Data;
using P3.Driver.HueBridge.EventArgs;
using P3.Driver.HueBridgeSimulator.DriverFactory.OnOff;

namespace P3.Driver.HueBridgeSimulatorDriverFactory
{
    public class HueBridgeDriver : DriverBase
    {
        private IList<HueLight> _hueLights = new List<HueLight>();
        private IDictionary<int, HueBridgeNode> _hueBridgeMapping = new Dictionary<int, HueBridgeNode>();
        private readonly HueDriver _driver;

        public HueBridgeDriver(IDriverContext driverContext) : base(driverContext)
        {
            _driver = new HueDriver(DriverContext.Logger);
        }

        private void HueDriverSwitchLight(object sender, HueSwitchLightEventArgs e)
        {
            if(_hueBridgeMapping.ContainsKey(e.Light.Id))
            {
                Task.Factory.StartNew(() => _hueBridgeMapping[e.Light.Id].SwitchLight(e));
            }
        }

        internal void SetLightState(int light, HueSwitchLightData state)
        {
            _driver.SetLightState(light, state, false);
        }

        public override bool Init()
        {
            return base.Init();
        }

        internal int AddHueLight(HueLight light, HueBridgeNode node)
        {
            var lightNr = _driver.AddLight(light);
            _hueBridgeMapping.Add(light.Id, node);
            return lightNr;
        }

        public override async Task<bool> Start()
        {
            _driver.SwitchLight += HueDriverSwitchLight;

            await Task.Factory.StartNew(async () =>
            {
                await _driver.Start();
            });

            DriverContext.Logger.LogInformation("Starting hue bridge web services");
            
            return await base.Start();
        }

        public override async Task<bool> Stop()
        {
            _driver.SwitchLight -= HueDriverSwitchLight;


            await _driver.Stop();
            return true;
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            if(ctx.NodeInstance.This2NodeTemplate == HueBridgeSimulatorDriverFactory.OnOffLight)
            {
                return new HueOnOffNode(ctx, this);
            }
            return null;
        }
    }
}
