using Automatica.Core.EF.Models;
using Automatica.Core.Model.Models.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Automatica.Core.WebApi.Controllers
{
    [Route("Ping")]
    public class PingController : BaseController
    {
        public PingController(AutomaticaContext dbContext) : base(dbContext)
        {
        }

        [HttpGet]
        [Authorize(Policy = Role.ViewerRole)]
        public string Ping() => "{ \"Ping\": \"Pong\"} ";
    }
}
