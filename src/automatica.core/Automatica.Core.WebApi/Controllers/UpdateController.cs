using Automatica.Core.EF.Models;
using Automatica.Core.Internals.Cloud;
using Automatica.Core.Internals.Cloud.Model;
using Automatica.Core.Internals.Core;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Automatica.Core.Internals.License;
using Microsoft.Extensions.Logging;

namespace Automatica.Core.WebApi.Controllers
{
    public class ResultDto
    {
        public bool Result { get; set; }
    }
    [Route("webapi/update")]
    public class UpdateController : BaseController
    {
        private readonly ILogger<UpdateController> _logger;
        private readonly ICloudApi _api;
        private readonly IAutoUpdateHandler _updateHandler;
        private readonly ILicenseContext _licenseContext;

        public ICoreServer CoreServer { get; }

        public UpdateController(ILogger<UpdateController> logger, AutomaticaContext dbContext, ICloudApi api, ICoreServer coreServer, IAutoUpdateHandler updateHandler, ILicenseContext licenseContext) : base(dbContext)
        {
            _logger = logger;
            _api = api;
            _updateHandler = updateHandler;
            _licenseContext = licenseContext;
            CoreServer = coreServer;
        }

        [HttpGet, Route("checkForUpdate")]
        public async Task<IServerVersion> CheckForUpdate()
        {
            var update = await _updateHandler.CheckForUpdates();
            return update;
        }

        [HttpGet, Route("license")]
        public async Task<string> GetLicense()
        {
            var license = await _licenseContext.GetLicense();
            return license;
        }

        [HttpGet, Route("alreadyDownloaded")]
        public async Task<ResultDto> AlreadyDownloaded()
        {
            var downloaded = await _updateHandler.UpdateAlreadyDownloaded();
            
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
                var check = await _updateHandler.DownloadUpdate(version);
                
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
