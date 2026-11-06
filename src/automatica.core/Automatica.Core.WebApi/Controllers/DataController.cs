using System;
using System.Collections.Generic;
using System.Linq;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.EF.Models.Trendings;
using Automatica.Core.Internals.Cache.Driver;
using Automatica.Core.Internals.Cache.Logic;
using Automatica.Core.Model.Models.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Automatica.Core.WebApi.Controllers
{
    [Route("webapi/data")]
    public class DataController : BaseController
    {
        /// <summary>
        /// Upper limit of the trend values of one request.
        /// </summary>
        public const int MaxTrendValues = 100000;

        private readonly IDispatcher _dispatcher;
        private readonly INodeInstanceCache _nodeInstanceCache;
        private readonly ILogicInterfaceInstanceCache _logicInterfaceInstanceCache;
        private readonly ILogicInstanceCache _logicInstanceCache;

        public DataController(AutomaticaContext db, IDispatcher dispatcher, INodeInstanceCache nodeInstanceCache,
            ILogicInterfaceInstanceCache logicInterfaceInstanceCache, ILogicInstanceCache logicInstanceCache)
            : base(db)
        {
            _dispatcher = dispatcher;
            _nodeInstanceCache = nodeInstanceCache;
            _logicInterfaceInstanceCache = logicInterfaceInstanceCache;
            _logicInstanceCache = logicInstanceCache;
        }

        /// <summary>
        /// The user group of a node or of the logic of a logic interface decides who may see the value.
        /// </summary>
        private bool CanSeeValue(Guid id)
        {
            if (IsAdmin)
            {
                return true;
            }

            var node = _nodeInstanceCache.Get(id);
            if (node != null)
            {
                return CanAccess(node.This2UserGroup);
            }

            var logicInterface = _logicInterfaceInstanceCache.Get(id);
            if (logicInterface != null)
            {
                return CanAccess(_logicInstanceCache.Get(logicInterface.This2RuleInstance)?.This2UserGroup);
            }

            return true;
        }

        private IDictionary<Guid, DispatchValue> Filter(IDictionary<Guid, DispatchValue> values)
        {
            if (values == null || IsAdmin)
            {
                return values;
            }

            return values.Where(a => CanSeeValue(a.Key)).ToDictionary(a => a.Key, a => a.Value);
        }

        [HttpGet]
        [Route("node/current")]
        [Authorize(Policy = Role.ViewerRole)]
        public IDictionary<Guid, DispatchValue> GetCurrentNodeValues()
        {
            return Filter(_dispatcher.GetValues(DispatchableType.NodeInstance));
        }
        [HttpGet]
        [Route("node/{id:guid}")]
        [Authorize(Policy = Role.ViewerRole)]
        public DispatchValue GetNodeValue(Guid id)
        {
            // a value the user may not see is treated like an unknown id
            return CanSeeValue(id) ? _dispatcher.GetValue(DispatchableType.NodeInstance, id) : null;
        }

        [HttpGet]
        [Route("all/current")]
        [Authorize(Policy = Role.ViewerRole)]
        public IDictionary<Guid, DispatchValue> GetCurrentValues()
        {
            return Filter(_dispatcher.GetValues());
        }
        [HttpGet]
        [Route("all/{id:guid}")]
        [Authorize(Policy = Role.ViewerRole)]
        public object GetValue(Guid id)
        {
            // a value the user may not see is treated like an unknown id
            return CanSeeValue(id) ? _dispatcher.GetValue(id) : null;
        }

        [HttpGet]
        [Route("trend/{id}/{startVisible}/{endVisible}/{startBound}/{endBound}")]
        [Authorize(Policy = Role.ViewerRole)]
        public ActionResult<IList<Trending>> GetTrendingValues(Guid id, DateTime startVisible, DateTime endVisible, DateTime startBound, DateTime endBound)
        {
            if (endBound < startBound)
            {
                return BadRequest("The end must not be before the start");
            }

            if (!CanSeeValue(id))
            {
                return NotFound();
            }

            // read only and bounded: a large range must not load the whole trend table into memory
            return DbContext.Trendings.AsNoTracking()
                .Where(a => a.This2NodeInstance == id && a.Timestamp >= startBound && a.Timestamp <= endBound)
                .OrderBy(a => a.Timestamp)
                .Take(MaxTrendValues)
                .ToList();
        }
    }
}
