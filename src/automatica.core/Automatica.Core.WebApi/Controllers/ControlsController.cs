using Automatica.Core.Control.Cache;
using Automatica.Core.EF.Models;
using Automatica.Core.Model.Models.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using Automatica.Core.Control.Base;

namespace Automatica.Core.WebApi.Controllers
{
    [Route("webapi/controls")]
    public class ControlsController : BaseController
    {
        private readonly IControlCache _controlCache;

        public ControlsController(AutomaticaContext dbContext, IControlCache controlCache) : base(dbContext)
        {
            _controlCache = controlCache;
        }

        [HttpGet]
        [Route("all")]
        [Authorize(Policy = Role.AdminRole)]
        public IEnumerable<IControl> GetControls()
        {
            return _controlCache.All();
        }
    }
}
