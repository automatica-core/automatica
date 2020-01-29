using System.Collections.Generic;
using Automatica.Core.EF.Models;
using Automatica.Core.Internals.Cache.Driver;
using Automatica.Core.Model.Models.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Automatica.Core.WebApi.Controllers
{
    [Route("webapi/nodeTemplates")]
    [Authorize(Roles = Role.AdminRole)]
    public class NodeTemplatesController : BaseController
    {
        private readonly INodeTemplateCache _nodeTemplateCache;

        public NodeTemplatesController(AutomaticaContext db, INodeTemplateCache nodeTemplateCache)
            : base(db)
        {
            _nodeTemplateCache = nodeTemplateCache;
        }

        [HttpGet]
        [Authorize(Roles = Role.AdminRole)]
        public ICollection<NodeTemplate> Get()
        {
            return _nodeTemplateCache.All();
        }
    }
}
