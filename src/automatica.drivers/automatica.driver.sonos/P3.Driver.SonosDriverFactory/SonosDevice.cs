using System;
using System.Linq;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using Microsoft.Extensions.Logging;
using P3.Driver.Sonos;
using P3.Driver.Sonos.Discovery;
using P3.Driver.Sonos.Models;
using P3.Driver.SonosDriverFactory.Attributes;

namespace P3.Driver.SonosDriverFactory
{
    public class SonosDevice : DriverBase
    {
        private string _id;
        private bool? _useFixedIp;
        private string _ip;

        private readonly SonosControllerFactory _sonosControllerFactory;
        private SonosController _controller;

        public SonosController Controller => _controller;

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

            _controller = _sonosControllerFactory.Create(ip, DriverContext.Logger);
            return await base.Start();
        }

        public override async Task<bool> Stop()
        {
            await _controller.StopAsync();
            return await base.Stop();
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            var nodeId = ctx.NodeInstance.This2NodeTemplateNavigation.ObjId;

            if (nodeId == SonosDriverFactory.PlayGuid)
            {
                return new SonosAttribute(ctx, this, async () =>
                {
                    var value = await _controller.GetIsPlayingAsync();
                    return value;
                }, async o =>
                {
                    if (o is true)
                    {
                        DriverContext.Logger.LogDebug($"Sonos play...");
                        await _controller.PlayAsync();
                        return true;
                    }

                    return null;
                });
            }
            if (nodeId == SonosDriverFactory.PauseGuid)
            {
                return new SonosAttribute(ctx, this, async () =>
                {
                    var value = await _controller.GetIsPlayingAsync();
                    return !value;
                }, async o =>
                {
                    if (o is true)
                    {
                        DriverContext.Logger.LogDebug($"Sonos pause...");
                        await _controller.PauseAsync();
                        return true;
                    }
                    return null;
                });
            }
            if (nodeId == SonosDriverFactory.NextTrack)
            {
                return new SonosAttribute(ctx, this, null, async o =>
                {
                    DriverContext.Logger.LogDebug($"Sonos next track...");
                    if (o is true)
                    {
                        await _controller.NextTrackAsync();
                        return true;
                    }

                    return null;
                });
            }
            if (nodeId == SonosDriverFactory.VolumeGuid)
            {
                return new SonosAttribute(ctx, this, async () =>
                {
                    var value = await _controller.GetVolumeAsync();
                    return value.Value;
                }, async o =>
                {
                    try
                    {
                        DriverContext.Logger.LogDebug($"Sonos set volume to {o}...");
                        var volume = Convert.ToInt32(o);
                        await _controller.SetVolumeAsync(new SonosVolume(volume));
                        return volume;
                    }
                    catch (Exception e)
                    {
                        DriverContext.Logger.LogError(e, "Could not set volume...");
                    }

                    return null;
                });
            }
            if (nodeId == SonosDriverFactory.SetTuneInRadio)
            {
                return new SonosSetRadioAttribute(ctx, this);
            }
            if (nodeId == SonosDriverFactory.SetTuneInRadioAndPlay)
            {
                return new SonosSetRadioAndPlayAttribute(ctx, this);
            }

            return null;
        }
    }
}
