using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using Microsoft.Extensions.Logging;
using P3.Driver.Pixoo64.Screens;

namespace P3.Driver.Pixoo64
{
    public class Pixoo64Driver : DriverBase
    {
        private readonly List<PixooSharp.Pixoo64> _pixoo64 = new List<PixooSharp.Pixoo64>();
        private PixooMainLoop _mainLoop;

        private readonly List<Pixoo64Screen> _screens = new List<Pixoo64Screen>();
        public Pixoo64Driver(IDriverContext ctx) : base(ctx)
        {
        }

        public override bool Init()
        {
            var ip = GetPropertyValueString("pixoo64-ip");
            int screenSize = 64;
            var size = GetProperty("pixoo64-size");

            if (size != null && size.ValueInt.HasValue)
            {
                screenSize = size.ValueInt.Value;
            }

            var ipSplit = ip.Split(";", StringSplitOptions.RemoveEmptyEntries);

            foreach (var ipAddress in ipSplit)
            {
                try
                {
                    IPAddress.Parse(ipAddress);
                    _pixoo64.Add(new PixooSharp.Pixoo64(ipAddress, screenSize));
                }
                catch (Exception e)
                {
                    DriverContext.Logger.LogError($"Could not parse IPAddress ({ipAddress})");
                }
            }

            


            return base.Init();
        }

        public override async Task<bool> Start()
        {
            DriverContext.Logger.LogInformation($"Starting pixoo64 main loop");
            _mainLoop = new PixooMainLoop(_pixoo64, DriverContext.Logger);
            foreach (var screen in _screens)
            {
                _mainLoop.AddScreen(screen.BaseScreen);
            }

            await _mainLoop.Start();
            return await base.Start();
        }

        public override async Task<bool> Stop()
        {
            DriverContext.Logger.LogInformation($"Stopping pixoo64 main loop");
            await _mainLoop.Stop();
            return await base.Stop();
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            var key = ctx.NodeInstance.This2NodeTemplateNavigation.Key;


            Pixoo64Screen? ret = null;
            switch (key)
            {
                case "pixoo64-infoscreen":
                    ret = new InfoScreenDriverNode(ctx, _pixoo64);
                    break;
                case "pixoo64-batteryscreen":
                    ret = new BatteryScreenDriverNode(ctx, _pixoo64);
                    break;
                case "pixoo64-crytoscreen":
                    ret = new CryptoScreenDriverNode(ctx, _pixoo64);
                    break;
                case "pixoo64-meterscreen":
                    ret = new MeterScreenDriverNode(ctx, _pixoo64);
                    break;
            }

            if (ret != null)
            {
                _screens.Add(ret);
            }

            return ret;
        }
    }
}
