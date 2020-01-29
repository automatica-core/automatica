using Automatica.Core.Base.Common;
using Automatica.Core.Common.Update;
using Automatica.Core.EF.Models;
using Automatica.Core.Internals;
using Automatica.Core.Internals.Cloud;
using Automatica.Core.Internals.Cloud.Model;
using Automatica.Core.Internals.Core;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace Automatica.Core.WebApi.Controllers
{
    public class ResultDto
    {
        public bool Result { get; set; }
    }
    [Route("webapi/update")]
    public class UpdateController : BaseController
    {
        private readonly ICloudApi api;
        private readonly IAutoUpdateHandler _updateHandler;

        public ICoreServer CoreServer { get; }

        public UpdateController(AutomaticaContext dbContext, ICloudApi api, ICoreServer coreServer, IAutoUpdateHandler updateHandler) : base(dbContext)
        {
            this.api = api;
            _updateHandler = updateHandler;
            CoreServer = coreServer;
        }

        [HttpGet, Route("checkForUpdate")]
        public async Task<ServerVersion> CheckForUpdate()
        {
            var update = await api.CheckForUpdates();
            return update;
        }

        [HttpGet, Route("alreadyDownloaded")]
        public async Task<ResultDto> AlreadyDownloaded()
        {
            var downloaded = await api.UpdateAlreadyDownloaded();
            
            return new ResultDto
            {
                Result = downloaded
            };
        }

        [HttpPost, Route("install")]
        public async Task<ResultDto> Install()
        {
            await _updateHandler.Update();
            return new ResultDto
            {
                Result = true
            };
        }

        [HttpPost, Route("download")]
        public async Task<ResultDto> Download([FromBody]ServerVersion version)
        {
            try
            {
                var fileInfo = await api.DownloadUpdate(version);
                var check = Update.CheckUpdateFile(SystemLogger.Instance, fileInfo.FullName, ServerInfo.Rid);

                if(!check)
                {
                    api.DeleteUpdate();
                }

                return new ResultDto
                {
                    Result = check
                };
            }
            catch (Exception)
            {
                return new ResultDto
                {
                    Result = false
                };
            }
        }
    }
}
