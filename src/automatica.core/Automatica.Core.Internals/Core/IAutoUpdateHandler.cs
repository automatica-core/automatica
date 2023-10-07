using Automatica.Core.Internals.Cloud.Model;
using System.IO;
using System.Threading.Tasks;

namespace Automatica.Core.Internals.Core
{
    public interface IAutoUpdateHandler
    {
        Task ReInitialize();
        Task Update();

        Task<IServerVersion> CheckForUpdates();
        Task<bool> UpdateAlreadyDownloaded();
        Task<bool> DownloadUpdate(IServerVersion version);
    }
}
