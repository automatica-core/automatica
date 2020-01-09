using Automatica.Core.EF.Models;
using Automatica.Core.Runtime.Core;
using Microsoft.AspNetCore.Mvc;

namespace Automatica.Core.WebApi.Controllers
{
    public class ServerStateObject
    {
        public RunState Status { get; set; }
    }

    [Route("webapi/server")]
    public class ServerController : BaseController
    {
        private readonly CoreServer _coreServer;

        public ServerController(AutomaticaContext dbContext, CoreServer coreServer) : base(dbContext)
        {
            _coreServer = coreServer;
        }

        [HttpGet]
        [Route("state")]
        public ServerStateObject GetServerStatus()
        {
            var statusObj = new ServerStateObject {Status = _coreServer.RunState};

            return statusObj;
        }
    }
}
