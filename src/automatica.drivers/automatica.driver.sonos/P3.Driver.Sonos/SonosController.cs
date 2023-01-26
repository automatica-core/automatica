using System.Threading;
using System.Threading.Tasks;
using P3.Driver.Sonos.Models;
using P3.Driver.Sonos.Upnp.Services;
using P3.Driver.Sonos.Upnp.Services.Models;

namespace P3.Driver.Sonos
{
    public class SonosController
    {
        private readonly IAvTransportService _avTransportService;
        private readonly IRenderingControlService _renderingControlService;
        private readonly IContentDirectoryService _contentDirectoryService;
        public const string TuneInMediaUrl = "x-sonosapi-stream:{0}?sid=254&amp;flags=32";

        public SonosController(IAvTransportService avTransportService,
            IRenderingControlService renderingControlService,
            IContentDirectoryService contentDirectoryService)
        {
            _avTransportService = avTransportService;
            _renderingControlService = renderingControlService;
            _contentDirectoryService = contentDirectoryService;
        }

        #region Speaker

        public async Task<bool> GetIsPlayingAsync()
        {
            var response = await _avTransportService.GetTransportInfoAsync();
            return response.CurrentTransportState.IsPlaying;
        }

        public async Task<SonosVolume> GetVolumeAsync()
        {
            var value = await _renderingControlService.GetVolumeAsync();
            return new SonosVolume(value);
        }

        public async Task SetVolumeAsync(SonosVolume volume)
        {
            await _renderingControlService.SetVolumeAsync(volume.Value);
        }

        public async Task EnableLoudnessAsync()
        {
            await _renderingControlService.SetLoudnessAsync(true);
        }

        public async Task DisableLoudnessAsync()
        {
            await _renderingControlService.SetLoudnessAsync(false);
        }

        public async Task<SonosBass> GetBassAsync()
        {
            var value = await _renderingControlService.GetBassAsync();
            return new SonosBass(value);
        }

        public async Task SetBassAsync(SonosBass bass)
        {
            await _renderingControlService.SetBassAsync(bass.Value);
        }

        public async Task<SonosTreble> GetTrebleAsync()
        {
            var value = await _renderingControlService.GetTrebleAsync();
            return new SonosTreble(value);
        }

        public async Task SetTrebleAsync(SonosTreble treble)
        {
            await _renderingControlService.SetTrebleAsync(treble.Value);
        }

        public async Task ResetEqAsync()
        {
            await SetBassAsync(new SonosBass());
            await SetTrebleAsync(new SonosTreble());
            await EnableLoudnessAsync();
        }

        public async Task FadeAsync(SonosVolume targetVolume = null)
        {
            if(targetVolume == null)
                targetVolume = new SonosVolume();

            var volume = await GetVolumeAsync();

            while (volume.Value > targetVolume.Value)
            {
                volume.Decrease(1);
                await SetVolumeAsync(volume);

                if (volume.Value > targetVolume.Value)
                    Thread.Sleep(500);
            }
        }
        
        #endregion

        #region Queue

        public async Task ClearQueueAsync()
        {
            await _avTransportService.ClearQueueAsync();
        }

        public async Task<BrowseResponse> GetQueueAsync()
        {
            var response = await _contentDirectoryService.BrowseAsync(0, 1000);

            return response;        // TODO: map what you need from BrowseResponse
        }

        public async Task RemoveQueueTrackAsync(int trackNumber)
        {
            await _avTransportService.RemoveTrackFromQueueAsync(new QueueItemId(trackNumber));
        }

        public async Task AddQueueTrackAsync(string trackUri, int desiredFirstTrackNumberEnqueued = 0, bool enqueueAsNext = false)
        {
            await _avTransportService.AddTrackToQueueAsync(trackUri, desiredFirstTrackNumberEnqueued, enqueueAsNext);
        }

        #endregion

        #region Current Track

        public async Task PlayAsync()
        {
            await _avTransportService.PlayAsync();
        }

        public async Task StopAsync()
        {
            await _avTransportService.StopAsync();
        }

        public async Task PauseAsync()
        {
            await _avTransportService.PauseAsync();
        }

        public async Task NextTrackAsync()
        {
            await _avTransportService.NextTrackAsync();
        }

        public async Task PreviousTrackAsync()
        {
            await _avTransportService.PreviousTrackAsync();
        }

        public async Task RestartTrackAsync()
        {
            await SeekTimeAsync(new SonosTimeSpan());
        }

        public async Task RestartQueueAsync()
        {
            await SeekTrackAsync(new SonosTrackNumber());
        }

        public async Task SeekTimeAsync(SonosTimeSpan time)
        {
            await _avTransportService.SeekAsync(new SeekUnit(SeekUnitType.Time), time.ToString());
        }

        public async Task SeekTrackAsync(SonosTrackNumber trackNumber)
        {
            await _avTransportService.SeekAsync(new SeekUnit(SeekUnitType.TrackNumber), trackNumber.ToString());
        }


        public async Task SetRadio(int radioId)
        {
            await _avTransportService.SetTuneInRadio(radioId);
        }
        public async Task SetMediaUrl(string url)
        {
            await _avTransportService.SetMediaUrl(url);
        }


        public Task<GetMediaInfoResponse> GetMediaInfoAsync()
        {
            return _avTransportService.GetMediaInfoAsync();
        }
        #endregion
    }
}