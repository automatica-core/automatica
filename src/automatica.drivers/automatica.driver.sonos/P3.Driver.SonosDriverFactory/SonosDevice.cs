using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Automatica.Core.Driver;
using Microsoft.Extensions.Logging;
using P3.Driver.Sonos;
using P3.Driver.Sonos.Discovery;
using P3.Driver.Sonos.Models;
using P3.Driver.SonosDriverFactory.Attributes;
using Timer = System.Timers.Timer;

namespace P3.Driver.SonosDriverFactory
{
    public class SonosDevice : DriverNotWriteableBase
    {
        private string _id;
        private bool? _useFixedIp;
        private string _ip;

        private readonly SonosControllerFactory _sonosControllerFactory;
        private SonosController _controller;

        public SonosController Controller => _controller;
        private readonly Timer _readTimer = new Timer();
        private int _errorReadCount = 0;

        private readonly SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);


        public SonosDevice(IDriverContext driverContext) : base(driverContext)
        {
            _sonosControllerFactory = new SonosControllerFactory();

            _readTimer.Elapsed += ReadTimerOnElapsed;
            _readTimer.Interval = TimeSpan.FromSeconds(15).TotalMilliseconds;
        }

        private async void ReadTimerOnElapsed(object sender, ElapsedEventArgs e)
        {
            await Read();
        }

        protected override async Task<bool> Read(IReadContext readContext, CancellationToken token = new CancellationToken())
        {
            try
            {
                await _semaphoreSlim.WaitAsync(token);
                var isPlaying = await _controller.GetIsPlayingAsync();

                _readTimer.Interval = isPlaying
                    ? TimeSpan.FromSeconds(1).TotalMilliseconds
                    : TimeSpan.FromSeconds(15).TotalMilliseconds;

                foreach (var child in Children)
                {
                    await child.Read(token);
                }

                _errorReadCount = 0;
            }
            catch (Exception ex)
            {
                _errorReadCount++;
                _readTimer.Interval = TimeSpan.FromSeconds(15).TotalMilliseconds;
                DriverContext.Logger.LogError(ex, "Error reading...");
                return false;
            }
            finally
            {
                if (_errorReadCount >= 10)
                {
                    //if we do use a variable ip....
                    if (_useFixedIp.HasValue && !_useFixedIp.Value)
                    {
                        var newDeviceIp = await GetDeviceIp();

                        if (!string.IsNullOrEmpty(newDeviceIp))
                        {
                            DriverContext.Logger.LogInformation($"Created a new controller index with ip {newDeviceIp} for device with id {_id}");
                            _controller = _sonosControllerFactory.Create(newDeviceIp, DriverContext.Logger);
                        }
                    }
                    _errorReadCount = 0;
                }

                _semaphoreSlim.Release();

            }

            return true;
        }

        public override Task<bool> Init(CancellationToken token = default)
        {
            _id = DriverContext.NodeInstance.GetPropertyValueString(SonosDriverFactory.IdAddressPropertyKey);
            
            _ip = DriverContext.NodeInstance.GetPropertyValueString(SonosDriverFactory.IpAddressPropertyKey);
            _useFixedIp = DriverContext.NodeInstance.GetProperty(SonosDriverFactory.UseFixedIpAddressPropertyKey).ValueBool;
            return base.Init(token);
        }

        public override async Task<bool> Start(CancellationToken token = default)
        {
            string ip;
            if (_useFixedIp.HasValue && _useFixedIp.Value)
            {
                ip = _ip;
            }
            else
            {
                var dynamicIp = await GetDeviceIp();

                if (string.IsNullOrEmpty(dynamicIp))
                {
                    return false;
                }

                ip = dynamicIp;
            }

            _controller = _sonosControllerFactory.Create(ip, DriverContext.Logger);
            _readTimer.Start();
            return await base.Start(token);
        }

        private async Task<string?> GetDeviceIp()
        {
            var scan = await SonosDiscovery.DiscoverSonos();

            if (scan.Count == 0)
            {
                DriverContext.Logger.LogError($"{Name}: Could not find any sonos device..");
                return null;
            }

            var device = scan.FirstOrDefault(a => a.Uuid == _id);

            if (device == null)
            {
                DriverContext.Logger.LogError($"{Name}: Could not find sonos device with id {_id}..");
                return null;
            }

            return device.Location.Host;
        }

        public override async Task<bool> Stop(CancellationToken token = default)
        {
            _readTimer.Stop();
            await _controller?.StopAsync();
            return await base.Stop(token);
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            var nodeId = ctx.NodeInstance.This2NodeTemplateNavigation.ObjId;
            DriverBase sonosAttribute = null;

            if (nodeId == SonosDriverFactory.PlayGuid)
            {
                sonosAttribute = new SonosAttribute(ctx, async () =>
                {
                    var value = await _controller.GetIsPlayingAsync();
                    return value;
                }, async o =>
                {
                    if (o is true)
                    {
                        DriverContext.Logger.LogDebug($"{Name}: Sonos play...");
                        await _controller.PlayAsync();
                        return true;
                    }

                    return false;
                });
            }
            if (nodeId == SonosDriverFactory.PauseGuid)
            {
                sonosAttribute = new SonosAttribute(ctx, async () =>
                {
                    var value = await _controller.GetIsPlayingAsync();
                    return !value;
                }, async o =>
                {
                    if (o is true)
                    {
                        DriverContext.Logger.LogDebug($"{Name}:Sonos pause...");
                        await _controller.PauseAsync();
                        return true;
                    }
                    return null;
                });
            }
            if (nodeId == SonosDriverFactory.NextTrack)
            {
                sonosAttribute = new SonosAttribute(ctx, null, async o =>
                {
                    DriverContext.Logger.LogDebug($"{Name}: Sonos next track...");
                    if (o is true)
                    {
                        await _controller.NextTrackAsync();
                        return true;
                    }

                    return null;
                });
            }
            if (nodeId == SonosDriverFactory.PreviousTrack)
            {
                sonosAttribute = new SonosAttribute(ctx, null, async o =>
                {
                    DriverContext.Logger.LogDebug($"{Name}: Sonos previous track...");
                    if (o is true)
                    {
                        await _controller.PreviousTrackAsync();
                        return true;
                    }

                    return null;
                });
            }
            if (nodeId == SonosDriverFactory.VolumeGuid)
            {
                return new SonosAttribute(ctx,  async () =>
                {
                    var value = await _controller.GetVolumeAsync();
                    return value.Value;
                }, async o =>
                {
                    try
                    {
                        DriverContext.Logger.LogDebug($"{Name}: Sonos set volume to {o}...");
                        var volume = Convert.ToInt32(o);
                        await _controller.SetVolumeAsync(new SonosVolume(volume));
                        return volume;
                    }
                    catch (Exception e)
                    {
                        DriverContext.Logger.LogError(e, $"{Name}: Could not set volume...");
                    }

                    return null;
                }, true);
            }
            if (nodeId == SonosDriverFactory.SetTuneInRadio)
            {
                sonosAttribute = new SonosSetRadioAttribute(ctx, this);
            }
            if (nodeId == SonosDriverFactory.SetTuneInRadioAndPlay)
            {
                sonosAttribute = new SonosSetRadioAndPlayAttribute(ctx, this);
            }

            if (nodeId == SonosDriverFactory.StatusGuid)
            {
                sonosAttribute = new SonosStatusAttribute(ctx, this);
            }

            if (nodeId == SonosDriverFactory.SetMediaUrl)
            {
                sonosAttribute = new SonosSetMediaUrlAttribute(ctx, this);
            }
            if (nodeId == SonosDriverFactory.SetMediaUrlAndPlay)
            {
                sonosAttribute = new SonosSetMediaUrlAndPlayAttribute(ctx, this);
            }

            return sonosAttribute;
        }
    }
}
