using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using Microsoft.Extensions.Logging;

namespace P3.Driver.SonosDriverFactory.Attributes
{
    internal class SonosStatusAttribute : DriverNotWriteableBase
    {
        private readonly SonosDevice _device;

        private readonly List<SonosStatusValueAttribute> _attributes = new();

        public SonosStatusAttribute(IDriverContext driverContext, SonosDevice device) : base(driverContext)
        {
            _device = device;
        }

        public override Task<bool> Start(CancellationToken token = new CancellationToken())
        {
            ReadValue().ConfigureAwait(false);
            return base.Start(token);
        }


        protected override async Task<bool> Read(IReadContext readContext, CancellationToken token = new CancellationToken())
        {
            try
            {
                await ReadValue();
                return true;
            }
            catch (Exception ex)
            {
                DriverContext.Logger.LogError(ex, "Error reading...");
                return false;
            }
        }

        private async Task ReadValue()
        {
            try
            {
                var val = await _device.Controller.GetPositionInfoAsync();
                foreach (var attribute in _attributes)
                {
                    attribute.GetValueAndDispatch(val);
                }
            }
            catch (Exception e)
            {
                DriverContext.Logger.LogError(e, "Could not read positionInfo...");
            }
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            var key = ctx.NodeInstance.This2NodeTemplateNavigation.Key;

            SonosStatusValueAttribute attribute;

            switch (key)
            {
                case "sonos-status-title":
                    attribute = new SonosStatusValueAttribute(ctx, (a) => a.TrackMetaData.Title);
                    break;
                case "sonos-status-creator":
                    attribute = new SonosStatusValueAttribute(ctx, (a) => a.TrackMetaData.Creator);
                    break;
                case "sonos-status-album":
                    attribute = new SonosStatusValueAttribute(ctx, (a) => a.TrackMetaData.Album);
                    break;
                case "sonos-status-album-art-uri":
                    attribute = new SonosStatusValueAttribute(ctx, (a) => a.TrackMetaData.AlbumArtUri);
                    break;
                case "sonos-status-class":
                    attribute = new SonosStatusValueAttribute(ctx, (a) => a.TrackMetaData.Class);
                    break;
                case "sonos-status-duration":
                    attribute = new SonosStatusValueAttribute(ctx, (a) => a.TrackDuration);
                    break;
                case "sonos-status-relative-time":
                    attribute = new SonosStatusValueAttribute(ctx, (a) => a.RelativeTime);
                    break;
                default:
                    return null;
            }

            _attributes.Add(attribute);
            return attribute;
        }
    }
}
