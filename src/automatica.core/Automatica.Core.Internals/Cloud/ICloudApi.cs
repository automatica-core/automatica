using Automatica.Core.EF.Models;
using Automatica.Core.Internals.Cloud.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.Base.Tunneling;

namespace Automatica.Core.Internals.Cloud
{
    public interface ICloudApi : IServerCloudApi
    {
        event EventHandler<DownloadProgressChangedEventArgs> DownloadUpdateProgressChanged;
        event EventHandler<EventArgs> DownloadUpdateFinished;
        event EventHandler<AsyncCompletedEventArgs> DownloadUpdateFailed;

        Task<bool> Ping();
        Task<string> GetLicense();
        Task<bool> SayHelloToCloud(SayHelloData sayHi);

        Task<RemoteConnectObject> CreateRemoteConnectUrl(string url);

        Task<RemoteConnectObject> CreateRemoteConnectUrl(Guid pluginGuid, string url);
        Task<RemoteConnectPortResponse> GetRemoteConnectPort(Guid pluginGuid, string serviceName, TunnelingProtocol tunnelingProtocol);
        Task<bool> SendRemoteConnectUrl(string url);


        Task<ServerVersion> CheckForUpdates();
        Task<ServerDockerVersion> CheckForDockerUpdates();
        Task<IList<Plugin>> GetLatestPlugins();

        Task<bool> UpdateAlreadyDownloaded();

        Task<FileInfo> DownloadUpdate(IServerVersion update);
        Task<byte[]> DownloadFile(string url);
        
        Task<bool> InstallPlugin(Plugin plugin, string fileName);

        void DeleteUpdate();
    }
}
