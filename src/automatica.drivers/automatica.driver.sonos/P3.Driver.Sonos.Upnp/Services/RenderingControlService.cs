using System.Collections.Generic;
using System.Threading.Tasks;
using P3.Driver.Sonos.Upnp.Proxy;

namespace P3.Driver.Sonos.Upnp.Services
{
    public enum EqType
    {
        DialogLevel,        // Speech enhancement
        NightMode           // Night sound mode
    }

    public class RenderingControlService : IRenderingControlService
    {
        private readonly IUpnpClient _upnpClient;

        private const string ControlUrl = "/MediaRenderer/RenderingControl/Control";
        private const string ActionNamespace = "urn:schemas-upnp-org:service:RenderingControl:1";

        public RenderingControlService(string ipAddress)
        {
            var upnpUri = new SonosUri(ipAddress, ControlUrl);
            _upnpClient = new UpnpClient(upnpUri.ToUri(), ActionNamespace);
        }
        
        public async Task<bool> GetIsHeadphoneConnectedAsync()
        {
            var result = await _upnpClient.InvokeFuncAsync<int>("GetHeadphoneConnected");
            return result != 0;
        }

        public async Task<int> GetVolumeAsync(string channel = "Master")
        {
            return await _upnpClient.InvokeFuncAsync<int>("GetVolume", new List<UpnpArgument>
            {
                new UpnpArgument("Channel", channel)
            });
        }

        public async Task<bool> GetIsMuteEnabledAsync(string channel = "Master")
        {
            var result = await _upnpClient.InvokeFuncAsync<int>("GetMute", new List<UpnpArgument>
            {
                new UpnpArgument("Channel", channel)
            });
            return result != 0;
        }

        public async Task<bool> GetIsLoudnessEnabledAsync(string channel = "Master")
        {
            var result = await _upnpClient.InvokeFuncAsync<int>("GetLoudness", new List<UpnpArgument>
            {
                new UpnpArgument("Channel", channel)
            });
            return result != 0;
        }

        public async Task<int> GetBassAsync()
        {
            return await _upnpClient.InvokeFuncAsync<int>("GetBass");
        }

        public async Task<int> GetTrebleAsync()
        {
            return await _upnpClient.InvokeFuncAsync<int>("GetTreble");
        }

        public async Task SetVolumeAsync(int volume, string channel = "Master")
        {
            await _upnpClient.InvokeActionAsync("SetVolume", new List<UpnpArgument>
            {
                new UpnpArgument("Channel", channel),
                new UpnpArgument("DesiredVolume", volume)
            });
        }

        public async Task SetMuteAsync(bool mute, string channel = "Master")
        {
            await _upnpClient.InvokeActionAsync("SetMute", new List<UpnpArgument>
            {
                new UpnpArgument("Channel", channel),
                new UpnpArgument("DesiredMute", mute)
            });
        }

        public async Task SetLoudnessAsync(bool loudness, string channel = "Master")
        {
            await _upnpClient.InvokeActionAsync("SetLoudness", new List<UpnpArgument>
            {
                new UpnpArgument("Channel", channel),
                new UpnpArgument("DesiredLoudness", loudness)
            });
        }

        public async Task SetBassAsync(int bass)
        {
            await _upnpClient.InvokeActionAsync("SetBass", new List<UpnpArgument>
            {
                new UpnpArgument("DesiredBass", bass)
            });
        }

        public async Task SetTrebleAsync(int treble)
        {
            await _upnpClient.InvokeActionAsync("SetTreble", new List<UpnpArgument>
            {
                new UpnpArgument("DesiredTreble", treble)
            });
        }

        public async Task<bool> GetEqAsync(EqType eqType)
        {
            // Only relevant for Soundbar

            var value = await _upnpClient.InvokeFuncAsync<int>("GetEQ", new List<UpnpArgument>
            {
                new UpnpArgument("EQType", eqType.ToString())
            });

            return value != 0;
        }

        public async Task SetEqAsync(EqType eqType, bool value)
        {
            // Only relevant for Soundbar

            await _upnpClient.InvokeActionAsync("SetEQ", new List<UpnpArgument>
            {
                new UpnpArgument("EQType", eqType.ToString()),
                new UpnpArgument("DesiredValue", value.ToInt())
            });
        }
    }
}