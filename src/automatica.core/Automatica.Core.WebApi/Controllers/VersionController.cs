using Automatica.Core.Base.Common;
using Automatica.Core.EF.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Automatica.Core.WebApi.Controllers
{
    public class VersionObject
    {
        public string Version { get; set; }
    }

    [Route("webapi/version")]
    [AllowAnonymous]
    public class VersionController : BaseController
    {
        public VersionController(AutomaticaContext dbContext) : base(dbContext)
        {
        }

        [HttpGet]
        public VersionObject Get()
        {
            var json = new VersionObject();
            json.Version = ServerInfo.GetServerVersion();
            return json;
        }
    }
}
