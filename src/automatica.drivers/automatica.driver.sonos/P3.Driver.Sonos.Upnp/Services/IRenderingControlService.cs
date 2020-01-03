using System.Threading.Tasks;

namespace P3.Driver.Sonos.Upnp.Services
{
    public interface IRenderingControlService
    {
        Task<bool> GetIsHeadphoneConnectedAsync();
        Task<int> GetVolumeAsync(string channel = "Master");
        Task<bool> GetIsMuteEnabledAsync(string channel = "Master");
        Task<bool> GetIsLoudnessEnabledAsync(string channel = "Master");
        Task<int> GetBassAsync();
        Task<int> GetTrebleAsync();
        Task SetVolumeAsync(int volume, string channel = "Master");
        Task SetMuteAsync(bool mute, string channel = "Master");
        Task SetLoudnessAsync(bool loudness, string channel = "Master");
        Task SetBassAsync(int bass);
        Task SetTrebleAsync(int treble);
    }
}