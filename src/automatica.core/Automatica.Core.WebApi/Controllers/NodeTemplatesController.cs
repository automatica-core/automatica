using System;
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
        private readonly INodeInstanceCache _nodeInstanceCache;

        public NodeTemplatesController(
            AutomaticaContext db, 
            INodeTemplateCache nodeTemplateCache, 
            INodeInstanceCache nodeInstanceCache)
            : base(db)
        {
            _nodeTemplateCache = nodeTemplateCache;
            _nodeInstanceCache = nodeInstanceCache;
        }

        [HttpGet]
        [Authorize(Roles = Role.AdminRole)]
        public ICollection<NodeTemplate> Get()
        {
            return _nodeTemplateCache.All();
        }

        [HttpGet]
        [Authorize(Roles = Role.AdminRole)]
        [Route("supported/{targetNodeTemplate}/{neededInterface}")]
        public ICollection<NodeTemplate> GetSupportedTemplates(Guid targetNodeTemplate, Guid neededInterface)
        {
            return _nodeTemplateCache.GetSupportedTemplates(_nodeInstanceCache.Get(targetNodeTemplate), neededInterface);
        }


    }
}
