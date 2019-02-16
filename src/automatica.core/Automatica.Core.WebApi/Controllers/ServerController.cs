using Automatica.Core.EF.Models;
using Automatica.Core.Runtime.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Automatica.Core.Model.Models.User;

namespace Automatica.Core.WebApi.Controllers
{
    public class ServerStateObject
    {
        public RunState Status { get; set; }
    }

    [Route("server")]
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

        [HttpPost]
        [Authorize(Policy = Role.AdminRole)]
        public async Task<bool> Reload()
        {
            await _coreServer.Reinit();

            return true;
        }
    }
}
