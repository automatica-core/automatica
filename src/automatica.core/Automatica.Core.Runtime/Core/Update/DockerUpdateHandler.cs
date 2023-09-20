using Automatica.Core.Base.Common;
using Automatica.Core.Runtime.Abstraction;
using System;
using System.Threading.Tasks;
using Automatica.Core.Internals.Cloud;
using Automatica.Core.Internals.Cloud.Model;
using Microsoft.Extensions.Configuration;

namespace Automatica.Core.Runtime.Core.Update
{
    internal class DockerUpdateHandler : IUpdateHandler
    {
        private readonly ICloudApi _cloudApi;
        private readonly bool _isSupervisorHosted;

        public DockerUpdateHandler(ICloudApi cloudApi, IConfiguration config)
        {
            _cloudApi = cloudApi;

            _isSupervisorHosted = config["AUTOMATICA_SUPERVISOR_HOSTED"] == "1";
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
            if (!_isSupervisorHosted)
            {
                throw new ArgumentException(
                    "Docker image cannot be automatically updated, if not hosted by our supervisor!");
            }
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
