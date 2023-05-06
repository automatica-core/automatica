using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using Automatica.Core.EF.Models;

namespace P3.Driver.Pixoo64.Screens
{
    internal class MeterScreenDriverNode : Pixoo64Screen<MeterScreen>
    {
        public MeterScreenDriverNode(IDriverContext driverContext, IList<PixooSharp.Pixoo64> pixoo) : base(driverContext, pixoo)
        {
        }


        protected override MeterScreen CreateScreen()
        {
            return new MeterScreen(Pixoo, DriverContext.Logger);
        }

        protected override async Task SetScreenValue(object value, NodeInstance node)
        {
            await Task.CompletedTask;
            var dValue = Convert.ToDouble(value);
            switch (node.This2NodeTemplateNavigation.Key)
            {
                case "meterscreen-meter1":
                    Screen.Meter1 = dValue;
                    Screen.Meter1Name = node.GetPropertyValueString("meter-name");
                    break;
                case "meterscreen-meter2":
                    Screen.Meter2 = dValue;
                    Screen.Meter2Name = node.GetPropertyValueString("meter-name");
                    break;
                case "meterscreen-meter3":
                    Screen.Meter3 = dValue;
                    Screen.Meter3Name = node.GetPropertyValueString("meter-name");
                    break;
                case "meterscreen-meter4":
                    Screen.Meter4 = dValue;
                    Screen.Meter4Name = node.GetPropertyValueString("meter-name");
                    break;
                case "meterscreen-meter5":
                    Screen.Meter5 = dValue;
                    Screen.Meter5Name = node.GetPropertyValueString("meter-name");
                    break;
                case "meterscreen-meter6":
                    Screen.Meter6 = dValue;
                    Screen.Meter6Name = node.GetPropertyValueString("meter-name");
                    break;
            }
        }
    }
}
