using System.Collections.Generic;
using Automatica.Core.EF.Models;
using Automatica.Core.Model.Models.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Automatica.Core.WebApi.Controllers
{
    [Route("nodeTemplates")]
    [Authorize(Roles = Role.AdminRole)]
    public class NodeTemplatesController : BaseController
    {
        public NodeTemplatesController(AutomaticaContext db)
            : base(db)
        {
            
        }

        [HttpGet]
        [Authorize(Roles = Role.AdminRole)]
        public IEnumerable<NodeTemplate> Get()
        {
            var x = DbContext.NodeTemplates
                .Include(a => a.This2NodeDataTypeNavigation)
                .Include(a => a.NeedsInterface2InterfacesTypeNavigation)
                .Include(a => a.ProvidesInterface2InterfaceTypeNavigation)
                .Include(a => a.PropertyTemplate).ThenInclude(b => b.This2PropertyTypeNavigation)
                .Include(a => a.PropertyTemplate).ThenInclude(b => b.Constraints).ThenInclude(c => c.ConstraintData);
            
            return x;
        }
    }
}
