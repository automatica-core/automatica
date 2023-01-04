using System;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using Automatica.Core.EF.Models;

namespace P3.Driver.Pixoo64.Screens
{
    internal class BatteryScreenDriverNode : Pixoo64Screen<BatteryScreen>
    {
       
        public BatteryScreenDriverNode(IDriverContext driverContext, PixooSharp.Pixoo64 pixoo) : base(driverContext, pixoo)
        {
        }

        protected override BatteryScreen CreateScreen()
        {
            return new BatteryScreen(Pixoo);
        }

        public override async Task SetScreenValue(object value, NodeInstance node)
        {
            await Task.CompletedTask;
            
            switch (node.This2NodeTemplateNavigation.Key)
            {
                case "battery-v1":
                    Screen.V1 = Convert.ToDouble(value);
                    break;
                case "battery-v2":
                    Screen.V2 = Convert.ToDouble(value);
                    break;
                case "battery-v3":
                    Screen.V3 = Convert.ToDouble(value);
                    break;
                case "battery-v4":
                    Screen.V4 = Convert.ToDouble(value);
                    break;
                case "battery-a1":
                    Screen.A1 = Convert.ToDouble(value);
                    break;
                case "battery-a2":
                    Screen.A2 = Convert.ToDouble(value);
                    break;
                case "battery-a3":
                    Screen.A3 = Convert.ToDouble(value);
                    break;
                case "battery-a4":
                    Screen.A4 = Convert.ToDouble(value);
                    break;
                case "battery-soc1":
                    Screen.Soc1 = Convert.ToInt32(value);
                    break;
                case "battery-soc2":
                    Screen.Soc2 = Convert.ToInt32(value);
                    break;
                case "battery-soc3":
                    Screen.Soc3 = Convert.ToInt32(value);
                    break;
                case "battery-soc4":
                    Screen.Soc4 = Convert.ToInt32(value);
                    break;
            }
        }
    }
}
