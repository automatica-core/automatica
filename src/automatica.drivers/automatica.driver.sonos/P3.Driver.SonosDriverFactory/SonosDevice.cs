using System;
using System.Linq;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using Microsoft.Extensions.Logging;
using P3.Driver.Sonos;
using P3.Driver.Sonos.Discovery;
using P3.Driver.Sonos.Models;

namespace P3.Driver.SonosDriverFactory
{
    public class SonosDevice : DriverBase
    {
        private string _id;
        private bool? _useFixedIp;
        private string _ip;

        private readonly SonosControllerFactory _sonosControllerFactory;
        private SonosController _controller;
        

        public SonosDevice(IDriverContext driverContext) : base(driverContext)
        {
            _sonosControllerFactory = new SonosControllerFactory();
        }

        public override bool Init()
        {
            _id = DriverContext.NodeInstance.GetPropertyValueString(SonosDriverFactory.IdAddressPropertyKey);
            
            _ip = DriverContext.NodeInstance.GetPropertyValueString(SonosDriverFactory.IpAddressPropertyKey);
            _useFixedIp = DriverContext.NodeInstance.GetProperty(SonosDriverFactory.UseFixedIpAddressPropertyKey).ValueBool;
            return base.Init();
        }

        public override async Task<bool> Start()
        {
            var ip = String.Empty;
            if (_useFixedIp.HasValue && _useFixedIp.Value)
            {
                ip = _ip;
            }
            else
            {
                var scan = await SonosDiscovery.DiscoverSonos();

                if (scan.Count == 0)
                {
                    DriverContext.Logger.LogError($"Could not find any sonos device..");
                    return false;
                }

                var device = scan.FirstOrDefault(a => a.Uuid == _id);

                if (device == null)
                {
                    DriverContext.Logger.LogError($"Could not find sonos device with id {_id}..");
                    return false;
                }

                ip = device.Location.Host;
            }

            _controller = _sonosControllerFactory.Create(ip);
            return await base.Start();
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            var nodeId = ctx.NodeInstance.This2NodeTemplateNavigation.ObjId;

            if (nodeId == SonosDriverFactory.PlayGuid)
            {
                return new Sonos(ctx, this, null, async o =>
                {
                    DriverContext.Logger.LogDebug($"Sonos play...");
                    await _controller.PlayAsync();
                });
            }
            if (nodeId == SonosDriverFactory.PauseGuid)
            {
                return new Sonos(ctx, this, null, async o =>
                {
                    DriverContext.Logger.LogDebug($"Sonos pause...");
                    await _controller.PauseAsync();
                });
            }
            if (nodeId == SonosDriverFactory.NextTrack)
            {
                return new Sonos(ctx, this, null, async o =>
                {

                    DriverContext.Logger.LogDebug($"Sonos next track...");
                    await _controller.NextTrackAsync();
                });
            }
            if (nodeId == SonosDriverFactory.SetVolumeGuid)
            {
                return new Sonos(ctx, this, null, async o =>
                {

                    DriverContext.Logger.LogDebug($"Sonos set volume to {o}...");
                    var volume = Convert.ToInt32(o);
                    await _controller.SetVolumeAsync(new SonosVolume(volume));
                });
            }
            if (nodeId == SonosDriverFactory.SetTuneInRadio)
            {
                return new Sonos(ctx, this, null, async o =>
                {
                    DriverContext.Logger.LogDebug($"Sonos set radio to {o}...");
                    await _controller.SetRadio((int)o);
                });
            }
            if (nodeId == SonosDriverFactory.SetTuneInRadioAndPlay)
            {
                return new Sonos(ctx, this, null, async o =>
                {

                    DriverContext.Logger.LogDebug($"Sonos set radio and play to {o}...");
                    var value = Convert.ToInt32(o);

                    await _controller.SetRadio(value);
                    await _controller.PlayAsync();
                });
            }

            return null;
        }
    }
}
