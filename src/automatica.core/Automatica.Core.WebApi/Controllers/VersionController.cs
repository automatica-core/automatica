using Automatica.Core.Base.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace Automatica.Core.WebApi.Controllers
{
    public class VersionObject
    {
        public string Version { get; set; }
    }

    [Route("webapi/version")]
    [AllowAnonymous]
    public class VersionController
    {
        [HttpGet]
        public VersionObject Get()
        {
            var json = new VersionObject();
            json.Version = ServerInfo.GetServerVersion();
            return json;
        }
    }
}
