using System.Threading.Tasks;
using P3.Driver.Sonos.Upnp.Services.Models;

namespace P3.Driver.Sonos.Upnp.Services
{
    public interface IContentDirectoryService
    {
        Task RefreshShareIndexAsync();
        Task<string> GetLastIndexChangeAsync();
        Task<bool> GetShareIndexInProgressAsync();
        Task<BrowseResponse> BrowseAsync(int startIndex = 0, int requestedCount = 100);
    }
}