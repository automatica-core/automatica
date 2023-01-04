using System.Collections.Generic;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using P3.Driver.Pixoo64.Screens;

namespace P3.Driver.Pixoo64
{
    public class Pixoo64Driver : DriverBase
    {
        private PixooSharp.Pixoo64 _pixoo64;
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
            _pixoo64 = new PixooSharp.Pixoo64(ip, screenSize);
            

            return base.Init();
        }

        public override async Task<bool> Start()
        {
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
