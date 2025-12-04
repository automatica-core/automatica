using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using Automatica.Core.EF.Models;

namespace P3.Driver.Pixoo64.Screens
{
    internal class InfoScreenDriverNode : Pixoo64Screen<InfoScreen>
    {
        public InfoScreenDriverNode(IDriverContext driverContext, IList<PixooSharp.Pixoo64> pixoo) : base(driverContext, pixoo)
        {
        }

        protected override InfoScreen CreateScreen()
        {
            return new InfoScreen(Pixoo, DriverContext.Logger);
        }


        protected override async Task SetScreenValue(object value, NodeInstance node, CancellationToken token = default)
        {
            await Task.CompletedTask;
            switch (node.This2NodeTemplateNavigation.Key)
            {
                case "info-outside":
                    Screen.Outside = Convert.ToDouble(value);
                    break;
                case "info-inside":
                    Screen.Inside = Convert.ToDouble(value);
                    break;
                case "door-lock-state":
                    Screen.DoorLockState = value?.ToString();
                    break;
            }
        }
    }
}
