using Automatica.Core.EF.Models;
using Automatica.Core.Internals.Cloud.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;

namespace Automatica.Core.Internals.Cloud
{
    public interface ICloudApi : IServerCloudApi
    {
        event EventHandler<DownloadProgressChangedEventArgs> DownloadUpdateProgressChanged;
        event EventHandler<EventArgs> DownloadUpdateFinished;
        event EventHandler<AsyncCompletedEventArgs> DownloadUpdateFailed;

        Task<bool> Ping();
        Task<bool> SayHelloToCloud(SayHelloData sayHi);

        Task<bool> SendNgrokTunnelUrl(string url);

        Task<ServerVersion> CheckForUpdates();
        Task<IList<Plugin>> GetLatestPlugins();

        Task<bool> UpdateAlreadyDownloaded();

        Task<FileInfo> DownloadUpdate(ServerVersion update);
        Task<byte[]> DownloadFile(string url);
        
        Task<bool> InstallPlugin(Plugin plugin, string fileName);

        void DeleteUpdate();
    }
}
