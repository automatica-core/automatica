using Automatica.Core.Base.Common;
using Automatica.Core.Runtime.Abstraction;
using System;
using System.Threading.Tasks;
using Automatica.Core.Internals.Cloud;
using Automatica.Core.Internals.Cloud.Model;

namespace Automatica.Core.Runtime.Core.Update
{
    internal class DockerUpdateHandler : IUpdateHandler
    {
        private readonly ICloudApi _cloudApi;

        public DockerUpdateHandler(ICloudApi cloudApi)
        {
            _cloudApi = cloudApi;
        }

        public Task ReInitialize()
        {
           return Task.CompletedTask;
        }

        public Task Update()
        {
            Environment.Exit(ServerInfo.ExitCodeUpdateInstallDocker);
            return Task.CompletedTask;
        }

        public async Task<IServerVersion> CheckForUpdates()
        {
            return await _cloudApi.CheckForDockerUpdates();
        }

        public Task<bool> UpdateAlreadyDownloaded()
        {
            return Task.FromResult(true);
        }

        public Task<bool> DownloadUpdate(IServerVersion version)
        {
            return Task.FromResult(true);
        }

        public Task Init()
        {

            return Task.CompletedTask;
        }
    }
}
